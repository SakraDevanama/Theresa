using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Dust.Patches;

/// <summary>
/// 抽牌拦截补丁 - 仅将 Theresa 的额外抽牌（fromHandDraw=false）中符合条件的攻击/技能牌转化为微尘。
///
/// 网络同步原则：
/// 1. 非 Theresa 玩家完全走原版 CardPileCmd.Draw，避免在联机两端复刻逻辑不一致。
/// 2. Theresa 的回合开始常规手牌抽取（fromHandDraw=true）也走原版，确保与原版的联网行为一致。
/// 3. 只有 Theresa 的额外抽牌（遗物/卡牌效果）进入微尘逻辑，且内部调用 Hook.ShouldDraw / CheckIfDrawIsPossible，
///    尽可能复刻原版抽牌判定，减少状态分歧。
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
        // 非 Theresa / 不在战斗 / 显式跳过 / 常规手牌抽取：全部交回原版逻辑
        if (!IsTheresa(player))
            return true;
        if (player?.Creature?.CombatState == null)
            return true;
        if (SkipDustConversion)
            return true;
        if (fromHandDraw)
            return true;

        __result = DustDrawAsync(choiceContext, count, player, fromHandDraw);
        return false;
    }

    private static bool IsTheresa(Player player) => player?.Character?.Id?.Entry == Theresa.TheresaCode.Character.Theresa.CharacterId;

    /// <summary>
    /// 微尘抽牌逻辑：逐张抽牌，符合微尘条件的牌直接移入 DustManager，否则正常加入手牌。
    /// </summary>
    private static async Task<IEnumerable<CardModel>> DustDrawAsync(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw)
    {
        var result = new List<CardModel>();
        var hand = PileType.Hand.GetPile(player);
        var drawPile = PileType.Draw.GetPile(player);
        int drawsRequested = count > 0m ? (int)Math.Ceiling(count) : 0;
        int remainingHandSpace = Math.Max(0, 10 - hand.Cards.Count);

        for (int i = 0; i < drawsRequested && remainingHandSpace > 0; i++)
        {
            var combatState = player.Creature.CombatState!;

            // 尊重原版“阻止抽牌”钩子（如 WordlessEffectPower）
            if (!Hook.ShouldDraw(combatState, player, fromHandDraw, out AbstractModel? modifier))
            {
                await Hook.AfterPreventingDraw(combatState, modifier!);
                break;
            }

            if (!CheckDrawPossible(player))
                break;

            await CardPileCmd.ShuffleIfNecessary(choiceContext, player);

            if (!CheckDrawPossible(player))
                break;

            var card = drawPile.Cards.FirstOrDefault();
            if (card == null || hand.Cards.Count >= 10)
                break;

            if (DustManager.ShouldBecomeDust(card))
            {
                // 转化为微尘：先从当前牌堆移除，再添加到 DustManager
                card.RemoveFromCurrentPile();
                await DustManager.AddCard(card);

                // 通知游戏原生代码抽牌事件（保持动画/历史记录一致）
                CombatManager.Instance.History.CardDrawn(combatState, card, fromHandDraw);
                await Hook.AfterCardDrawn(combatState, choiceContext, card, fromHandDraw);
                card.InvokeDrawn();
                // 不加入 result（因为没抽入手牌）
            }
            else
            {
                // 正常抽牌
                result.Add(card);
                await CardPileCmd.Add(card, hand);
                CombatManager.Instance.History.CardDrawn(combatState, card, fromHandDraw);
                await Hook.AfterCardDrawn(combatState, choiceContext, card, fromHandDraw);
                card.InvokeDrawn();
            }

            remainingHandSpace = Math.Max(0, 10 - hand.Cards.Count);
        }

        return result;
    }

    /// <summary>
    /// 复刻原版 CardPileCmd.CheckIfDrawIsPossibleAndShowThoughtBubbleIfNot 的判定。
    /// </summary>
    private static bool CheckDrawPossible(Player player)
    {
        if (PileType.Draw.GetPile(player).Cards.Count + PileType.Discard.GetPile(player).Cards.Count == 0)
        {
            ThinkCmd.Play(new LocString("combat_messages", "NO_DRAW"), player.Creature, 2.0);
            return false;
        }

        if (PileType.Hand.GetPile(player).Cards.Count >= 10)
        {
            ThinkCmd.Play(new LocString("combat_messages", "HAND_FULL"), player.Creature, 2.0);
            return false;
        }

        return true;
    }
}
