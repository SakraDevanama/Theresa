using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 何以为家
/// 2费攻击牌
/// 造成30点伤害（+18）
/// 若本回合结束没有打出，给予自身3层ZaakathHateBacklashPower（+1）
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class WhereIsHome() : TheresaCardModel(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ZaakathHateBacklashPower>(),
    ];
    
    public override bool HasTurnEndInHandEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(30m, ValueProp.Move),
        new DynamicVar("Backlash", 3m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(18m);
        DynamicVars["Backlash"].UpgradeValueBy(1m);
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        int backlashAmount = (int)DynamicVars["Backlash"].BaseValue;
        await PowerCmd.Apply<ZaakathHateBacklashPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, backlashAmount, Owner.Creature, this);
    }
}
