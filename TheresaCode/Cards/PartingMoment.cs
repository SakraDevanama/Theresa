using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 道别之时
/// 1费技能牌
/// 失去1个真言。
/// 提升所有敌人的茧缚3（+2）层。如果没有茧缚则先赋予1层茧缚。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class PartingMoment() : TheresaCardModel(baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [LingerKeyword.Linger, DimKeyword.Dim];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromPower<DivinityStance>(),
        HoverTipFactory.FromPower<SilkCocoon>(),
        HoverTipFactory.FromPower<Broken>()
    ];
    
    
    private const int BaseCocoonIncrease = 3;
    private const int UpgradeCocoonBonus = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("CocoonAmount", BaseCocoonIncrease)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        // 1. 失去1个真言
        var mantraPower = Owner.Creature.Powers.OfType<MantraPower>().FirstOrDefault();
        if (mantraPower != null)
        {
            await PowerCmd.Apply<MantraPower>(Owner.Creature, -1m, Owner.Creature, this);
        }

        // 2. 对所有敌人处理茧缚
        var enemies = combatState.GetOpponentsOf(Owner.Creature)
            .Where(c => c.IsAlive)
            .ToList();

        var cocoonAmount = (int)DynamicVars["CocoonAmount"].BaseValue;

        foreach (var enemy in enemies)
        {
            var hasCocoon = enemy.Powers.OfType<SilkCocoon>().Any();
            if (hasCocoon)
            {
                await PowerCmd.Apply<SilkCocoon>(enemy, cocoonAmount, Owner.Creature, this);
            }
            else
            {
                await PowerCmd.Apply<SilkCocoon>(enemy, 1, Owner.Creature, this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["CocoonAmount"].UpgradeValueBy(UpgradeCocoonBonus);
    }
}
