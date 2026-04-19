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
/// 予你安息
/// 2费攻击牌
/// 对所有敌人：
/// 造成12（+4）点伤害
/// 给予5（+2）层凋亡
/// 给予凋亡爆发
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class GrantYouRest() : TheresaCardModel(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ApoptosisPower>(),
        HoverTipFactory.FromPower<ApoptosisBurstPower>()
    ];
    
    
    // 基础伤害
    private const int BaseDamage = 11;
    // 升级后伤害增加
    private const int UpgradeDamageBonus = 4;
    
    // 基础凋亡层数
    private const int BaseApoptosis = 3;
    // 升级后凋亡增加
    private const int UpgradeApoptosisBonus = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move),
        new DynamicVar("ApoptosisAmount", BaseApoptosis)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;

        // 对所有敌人造成伤害、凋亡和凋亡爆发
        // 使用 ToList() 创建快照，避免在迭代过程中集合被修改（敌人死亡时会被移除）
        foreach (Creature enemy in CombatState.Enemies.ToList())
        {
            // 检查敌人是否仍然可击中（可能已经被之前的伤害杀死）
            if (!enemy.IsHittable || !enemy.IsAlive) continue;

            // 1. 造成伤害
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(enemy)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            // 造成伤害后再次检查敌人是否仍然存活
            if (!enemy.IsAlive) continue;

            // 2. 给予凋亡
            int apoptosisAmount = (int)DynamicVars["ApoptosisAmount"].BaseValue;
            await PowerCmd.Apply<ApoptosisPower>(new ThrowingPlayerChoiceContext(), enemy, apoptosisAmount, Owner?.Creature, this);

            // 3. 给予凋亡爆发
            await PowerCmd.Apply<ApoptosisBurstPower>(new ThrowingPlayerChoiceContext(), enemy, 1, Owner?.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后伤害+4
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        // 升级后凋亡+2
        DynamicVars["ApoptosisAmount"].UpgradeValueBy(UpgradeApoptosisBonus);
    }
}