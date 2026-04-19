using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 故事的终章
/// Boss 遗物
/// 
/// 回合开始时：如果微尘数量低于上限，抽等同于差值的牌。
/// 回合结束时：本回合被萦绕过的固有微尘获得 虚无 和 消耗，然后移入弃牌堆。
/// 
/// 获得时：如果已有巴别塔遗物，替换它并继承位置。
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class BabelWord : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    // 记录本回合被萦绕过的卡牌（用于回合结束时处理）
    private readonly List<CardModel> _lingeredThisTurn = [];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player == null || player.NetId != Owner.NetId)
            return;

        // 回合开始时：如果微尘数量低于上限，抽等同于差值的牌
        int dustCount = DustManager.Cards.Count;
        int maxDust = DustManager.MaxDust;
        int drawCount = maxDust - dustCount;

        if (drawCount > 0)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, drawCount, player);
        }

        // 清空上回合记录
        _lingeredThisTurn.Clear();

        await base.AfterPlayerTurnStart(choiceContext, player);
    }

    /// <summary>
    /// 记录被萦绕的卡牌（由 DustManager 调用）
    /// </summary>
    public void OnCardLingered(CardModel card)
    {
        if (card == null) return;
        if (!_lingeredThisTurn.Contains(card))
        {
            _lingeredThisTurn.Add(card);
        }
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != CombatSide.Player)
            return;

        // 回合结束时：处理本回合被萦绕过的固有微尘
        bool anyProcessed = false;
        foreach (var card in _lingeredThisTurn.ToList())
        {
            // 检查卡牌是否仍在微尘中
            if (DustManager.ContainsCard(card))
            {
                anyProcessed = true;

                // 添加 虚无（Ethereal）
                if (!card.Keywords.Contains(CardKeyword.Ethereal))
                {
                    card.AddKeyword(CardKeyword.Ethereal);
                }

                // 添加 消耗（Exhaust）
                if (!card.Keywords.Contains(CardKeyword.Exhaust))
                {
                    card.AddKeyword(CardKeyword.Exhaust);
                }

                // 移入弃牌堆
                await DustManager.RemoveCard(card);
                await CardPileCmd.Add(card, PileType.Discard);
            }
        }

        if (anyProcessed)
        {
            Flash();
        }

        _lingeredThisTurn.Clear();

        await base.BeforeTurnEnd(choiceContext, side);
    }
}
