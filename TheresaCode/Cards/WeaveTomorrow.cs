using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 编织来日
/// 本回合内使所有未拥有SilkCocoon的敌人给予SilkCocoon
/// 本回合SilkCocoon会额外触发1（+1）次
/// （如果未拥有先触发给予，再触发额外）
/// </summary>

[Pool(typeof(TheresaCardPool))]
public sealed class WeaveTomorrow() : TheresaCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<SilkCocoon>(),
        HoverTipFactory.FromPower<Broken>()
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ExtraTrigger", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        // 1. 获取所有未拥有SilkCocoon的敌人
        var enemiesWithoutSilkCocoon = CombatState.Enemies
            .Where(e => e.IsAlive && e.GetPowerAmount<SilkCocoon>() <= 0)
            .ToList();

        // 2. 给予这些敌人SilkCocoon（先给予）
        if (enemiesWithoutSilkCocoon.Any())
        {
            await PowerCmd.Apply<SilkCocoon>(new ThrowingPlayerChoiceContext(), enemiesWithoutSilkCocoon, 2m, Owner.Creature, this);
        }

        // 3. 应用"编织来日"效果Power（再触发额外）
        var extraTriggerCount = DynamicVars["ExtraTrigger"].BaseValue;
        await PowerCmd.Apply<WeaveTomorrowEffect>(new ThrowingPlayerChoiceContext(), Owner.Creature, extraTriggerCount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级后额外触发次数 +1（从1变为2）
        DynamicVars["ExtraTrigger"].UpgradeValueBy(1m);
    }
}
