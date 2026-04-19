using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Runs;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Dust.Patches;

/// <summary>
/// 抽牌拦截补丁 - 将攻击/技能牌转化为微尘
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Draw))]
[HarmonyPatch([typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool)])]
public static class DustDrawPatch
{
    /// <summary>
    /// 临时标志：跳过 Dust 转化，用于需要直接抽牌的逻辑（如 Iterate）
    /// </summary>
    public static bool SkipDustConversion { get; set; }

    [HarmonyPrefix]
    public static bool Prefix(ref Task<IEnumerable<CardModel>> __result, PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw)
    {
        __result = AsyncWrapper(choiceContext, count, player, fromHandDraw);
        return false;
    }

    private static bool IsTheresa(Player player) => player?.Character?.Id?.Entry == Theresa.TheresaCode.Character.Theresa.CharacterId;

    /// <summary>
    /// 原版抽牌逻辑的内联实现，避免 ReversePatch 递归问题
    /// </summary>
    private static async Task<IEnumerable<CardModel>> DrawOriginal(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw)
    {
        var result = new List<CardModel>();
        var hand = PileType.Hand.GetPile(player);
        var drawPile = PileType.Draw.GetPile(player);
        int drawsRequested = count > 0m ? (int)Math.Ceiling(count) : 0;
        int num = Math.Max(0, 10 - hand.Cards.Count);

        for (int i = 0; i < drawsRequested && num > 0; i++)
        {
            // 安全检查：如果抽牌堆和弃牌堆都为空，直接退出
            var discardPile = PileType.Discard.GetPile(player);
            if (drawPile.Cards.Count == 0 && discardPile.Cards.Count == 0)
            {
                MainFile.Logger?.Info("[DustDrawPatch] Draw pile and discard pile are both empty, stopping draw.");
                break;
            }

            await CardPileCmd.ShuffleIfNecessary(choiceContext, player);
            var card = drawPile.Cards.FirstOrDefault();
            if (card == null || hand.Cards.Count >= 10) break;

            result.Add(card);
            await CardPileCmd.Add(card, hand);
            CombatManager.Instance.History.CardDrawn(player.Creature.CombatState, card, fromHandDraw);
            await Hook.AfterCardDrawn(player.Creature.CombatState, choiceContext, card, fromHandDraw);
            card.InvokeDrawn();
            num = Math.Max(0, 10 - hand.Cards.Count);
        }

        return result;
    }

    private static async Task<IEnumerable<CardModel>> AsyncWrapper(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw)
    {
        // 只处理 Theresa 玩家
        if (!IsTheresa(player))
        {
            return await DrawOriginal(choiceContext, count, player, fromHandDraw);
        }

        // 如果玩家不在战斗中，走原版逻辑
        if (player?.Creature?.CombatState == null)
        {
            return await DrawOriginal(choiceContext, count, player, fromHandDraw);
        }

        // 如果设置了跳过标志，走原版逻辑
        if (SkipDustConversion)
        {
            return await DrawOriginal(choiceContext, count, player, fromHandDraw);
        }

        // 如果微尘已满，走原版逻辑
        if (DustManager.IsFull)
        {
            return await DrawOriginal(choiceContext, count, player, fromHandDraw);
        }

        // 否则，走自定义逻辑：部分抽牌转化为微尘
        var result = new List<CardModel>();
        var drawPile = PileType.Draw.GetPile(player);
        var hand = PileType.Hand.GetPile(player);
        int drawsRequested = count > 0m ? (int)Math.Ceiling(count) : 0;
        int num = Math.Max(0, 10 - hand.Cards.Count);

        for (int i = 0; i < drawsRequested && num > 0; i++)
        {
            // 安全检查：如果抽牌堆和弃牌堆都为空，直接退出
            var discardPile = PileType.Discard.GetPile(player);
            if (drawPile.Cards.Count == 0 && discardPile.Cards.Count == 0)
            {
                MainFile.Logger?.Info("[DustDrawPatch] Draw pile and discard pile are both empty, stopping dust conversion draw.");
                break;
            }

            await CardPileCmd.ShuffleIfNecessary(choiceContext, player);
            var card = drawPile.Cards.FirstOrDefault();
            if (card == null || hand.Cards.Count >= 10) break;

            if (!card.Keywords.Contains(Keywords.DimKeyword.Dim) && DustManager.ShouldBecomeDust(card))
            {
                // 转化为微尘：先移除再添加，保持状态一致
                card.RemoveFromCurrentPile();
                await DustManager.AddCard(card);
                // 通知游戏原生代码抽牌事件（保持动画系统正常工作）
                CombatManager.Instance.History.CardDrawn(player.Creature.CombatState, card, fromHandDraw);
                await Hook.AfterCardDrawn(player.Creature.CombatState, choiceContext, card, fromHandDraw);
                card.InvokeDrawn();
                // 不加入 result（因为没抽入手牌）
            }
            else
            {
                // 正常抽牌
                result.Add(card);
                await CardPileCmd.Add(card, hand);
                CombatManager.Instance.History.CardDrawn(player.Creature.CombatState, card, fromHandDraw);
                await Hook.AfterCardDrawn(player.Creature.CombatState, choiceContext, card, fromHandDraw);
                card.InvokeDrawn();
            }

            num = Math.Max(0, 10 - hand.Cards.Count);
        }

        return result;
    }
}
