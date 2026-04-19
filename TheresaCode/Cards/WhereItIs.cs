using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// ■之所在
/// 1费攻击牌
/// 造成7（升级后+2）点伤害1次
/// 敌人每有1层SilkCocoon就增加一次伤害判定
/// </summary>

[Pool(typeof(TheresaCardPool))]
public sealed class WhereItIs() : TheresaCardModel(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<SilkCocoon>(),
        HoverTipFactory.FromPower<Broken>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var targetCreature = cardPlay.Target;
        if (targetCreature == null) return;

        // 获取目标敌人的SilkCocoon层数
        var silkCocoonAmount = targetCreature.GetPowerAmount<SilkCocoon>();
        
        // 基础1次 + 每有1层SilkCocoon增加1次
        var hitCount = 1 + silkCocoonAmount;

        // 造成伤害，次数由SilkCocoon层数决定
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(hitCount)
            .WithHitFx("vfx/vfx_attack_slash", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升级后伤害 +2（从7变为9）
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
