using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 落零花海
/// 1费（升级后0费）技能牌
/// 获得1点能量
/// 若你在下回合开始前有能量剩余，再获得1点能量
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class FallingPetalSea : TheresaCardModel
{
    public FallingPetalSea() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = Owner?.Creature;
        if (creature == null)
            return;

        // 立即获得1点能量
        if (Owner != null)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        }

        // 给予"落零花海效果"Power，用于追踪本回合结束时的能量剩余情况
        await PowerCmd.Apply<FallingPetalSeaEffectPower>(new ThrowingPlayerChoiceContext(), creature, 1m, creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级后费用变为0
        EnergyCost.UpgradeBy(-1);
    }
}
