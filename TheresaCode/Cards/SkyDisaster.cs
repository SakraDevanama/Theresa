using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;


[Pool(typeof(TheresaCardPool))]
public sealed class SkyDisaster() : TheresaCardModel(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ApoptosisPower>()
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(7m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target!;
        
        // 造成7点伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 获取伤害值（用于给予等量ApoptosisPower）
        var damageAmount = DynamicVars.Damage.BaseValue;
        
        // 检查目标是否已有ApoptosisPower
        var existingApoptosis = target.GetPower<ApoptosisPower>();
        var hasApoptosis = existingApoptosis != null && existingApoptosis.Amount > 0;
        
        // 给予等量ApoptosisPower
        await PowerCmd.Apply<ApoptosisPower>(new ThrowingPlayerChoiceContext(), target, damageAmount, Owner.Creature, this);
        
        // 若目标无ApoptosisPower，则再给予1次
        if (!hasApoptosis)
        {
            await PowerCmd.Apply<ApoptosisPower>(new ThrowingPlayerChoiceContext(), target, damageAmount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
