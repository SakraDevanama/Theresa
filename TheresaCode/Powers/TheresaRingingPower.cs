using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Afflictions;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 特雷西斯版耳鸣（复刻原版 RingingPower）
/// 修复：目标卡牌已有其他 affliction 时也能正确附加 Ringing
/// </summary>
public sealed class TheresaRingingPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        IEnumerable<CardModel> allCards = base.Owner.Player.PlayerCombatState.AllCards;
        foreach (CardModel item in allCards)
        {
            // 如果卡牌已有其他 affliction，先清除再附加 Ringing
            if (item.Affliction != null && item.Affliction is not TheresaRinging)
            {
                CardCmd.ClearAffliction(item);
            }
            await CardCmd.Afflict<TheresaRinging>(item, 1m);
        }
    }

    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Owner == base.Owner.Player)
        {
            // 如果卡牌已有其他 affliction，先清除再附加 Ringing
            if (card.Affliction != null && card.Affliction is not TheresaRinging)
            {
                CardCmd.ClearAffliction(card);
            }
            await CardCmd.Afflict<TheresaRinging>(card, 1m);
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            Flash();
            await PowerCmd.Remove(this);
        }
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        IEnumerable<CardModel> enumerable = oldOwner.Player?.PlayerCombatState?.AllCards ?? Array.Empty<CardModel>();
        foreach (CardModel item in enumerable)
        {
            if (item.Affliction is TheresaRinging)
            {
                CardCmd.ClearAffliction(item);
            }
        }
        return Task.CompletedTask;
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType _)
    {
        if (card.Owner.Creature != base.Owner)
        {
            return true;
        }
        if (card.Affliction is not TheresaRinging)
        {
            return true;
        }
        return !CombatManager.Instance.History.CardPlaysStarted.Any((CardPlayStartedEntry e) => e.HappenedThisTurn(base.CombatState) && e.CardPlay.Card.Owner.Creature == base.Owner);
    }
}
