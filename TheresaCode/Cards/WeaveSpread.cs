using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

[Pool(typeof(TheresaCardPool))]
public sealed class WeaveSpread() : TheresaCardModel(3, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<Broken>(),
        HoverTipFactory.FromPower<SilkCocoon>(),
    ];

    // 修改 CanonicalVars
    // 1. DamageVar 作为单次伤害
    // 2. 添加一个 RepeatVar 来控制攻击次数 (3次)
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(6m, ValueProp.Move), // 单次伤害
        new RepeatVar(3)                   // 攻击次数
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var targetCreature = cardPlay.Target as Creature;
        if (targetCreature == null) return;

        // 1. 对目标敌人造成伤害，次数由 RepeatVar 控制
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue) // 使用 DamageVar 的基础值作为单次伤害
            .FromCard(this)
            .Targeting(cardPlay.Target) // 目标是特定敌人
            .WithHitCount(DynamicVars.Repeat.IntValue) // 攻击次数由 RepeatVar 控制
            .WithHitFx("vfx/vfx_attack_slash", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        var creatureCombatState = Owner.Creature.CombatState;
        if (creatureCombatState == null) return;

        // 获取所有敌人 (这个列表包含了刚被攻击的目标)
        var allEnemies = creatureCombatState.GetOpponentsOf(Owner.Creature).ToList();

        // 获取除了目标之外的所有敌人 (这个列表不包含刚被攻击的目标)
        var otherEnemies = allEnemies.Where(enemy => enemy != targetCreature).ToList();

        // 2. 如果目标没有 SilkCocoon，先赋予目标一层
        var silkCocoonAmount = targetCreature.GetPowerAmount<SilkCocoon>();
        if (silkCocoonAmount <= 0)
        {
            await PowerCmd.Apply<SilkCocoon>(new ThrowingPlayerChoiceContext(), targetCreature, 1m, Owner.Creature, this);
            silkCocoonAmount = 1;
        }

        // 3. 将该层数传播给除目标外的所有其他敌人
        if (otherEnemies.Any())
        {
            await PowerCmd.Apply<SilkCocoon>(new ThrowingPlayerChoiceContext(), otherEnemies, silkCocoonAmount, Owner.Creature, this);
        }

    }

    // 添加 OnUpgrade 方法来处理卡牌升级
    protected override void OnUpgrade()
    {
        // 升级时，将单次伤害值（DamageVar）增加 2
        // 这样，未升级时总伤害是 (6 * 3) = 18，升级后总伤害是 (8 * 3) = 24
        DynamicVars.Damage.UpgradeValueBy(1m); // 根据您的要求，每次升级增加1点基础伤害
    }
}