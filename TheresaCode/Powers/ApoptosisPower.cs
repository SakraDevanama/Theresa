using System.Collections.Generic;
using BaseLib.Hooks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 动态变量：显示凋亡的伤害降低百分比
/// 实时计算，不依赖外部更新BaseValue
/// </summary>
public class ApoptosisReductionVar : DynamicVar
{
    public ApoptosisReductionVar() : base("ReductionPercent", 0m)
    {
    }

    public override void SetOwner(AbstractModel owner)
    {
        base.SetOwner(owner);
    }

    /// <summary>
    /// 实时计算降低百分比
    /// </summary>
    protected override decimal GetBaseValueForIConvertible()
    {
        if (_owner is ApoptosisPower apoptosisPower && apoptosisPower.Owner != null)
        {
            return apoptosisPower.CalculateReduction(true);
        }
        return 0m;
    }

    /// <summary>
    /// ToString也返回计算值
    /// </summary>
    public override string ToString()
    {
        return ((int)GetBaseValueForIConvertible()).ToString();
    }
}

/// <summary>
/// 凋亡 - 根据层数与生命值的比例降低造成的伤害
/// 公式: 伤害降低比例 = min(凋亡层数 / 当前生命值, 25%)，有凋亡爆发时翻倍（最大50%）
/// </summary>
public class ApoptosisPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 凋亡血条显示的颜色 (亮粉色)
    private static readonly Color ApoptosisColor = new Color("FF69B4");

    // 预创建动态变量实例，确保只创建一个
    private static readonly ApoptosisReductionVar _reductionVarInstance = new();

    // 返回预创建的实例
    protected override IEnumerable<DynamicVar> CanonicalVars => new[] { _reductionVarInstance };

    /// <summary>
    /// 计算伤害降低比例
    /// </summary>
    /// <param name="asPercentage">是否以百分比形式返回（100表示100%）</param>
    /// <returns>伤害降低比例</returns>
    public decimal CalculateReduction(bool asPercentage = false)
    {
        if (Owner == null || Owner.CurrentHp <= 0) return 0m;

        // 计算比例: 凋亡层数 / 当前生命值
        decimal ratio = (decimal)Amount / Owner.CurrentHp;

        // 上限 25%
        ratio = Math.Min(ratio, 0.25m);

        // 有凋亡爆发时翻倍
        var burstPower = Owner.Powers.FirstOrDefault(p => p is ApoptosisBurstPower);
        if (burstPower != null && burstPower.Amount > 0)
        {
            ratio *= (1m + burstPower.Amount);
        }

        // 再次限制上限为50%（即使有爆发）
        ratio = Math.Min(ratio, 0.50m);

        return asPercentage ? ratio * 100 : ratio;
    }

    /// <summary>
    /// 是否已达到最大降低比例（25%，用于UI显示）
    /// </summary>
    public bool ReachMax()
    {
        if (Owner == null || Owner.CurrentHp <= 0) return false;
        decimal ratio = (decimal)Amount / Owner.CurrentHp;
        return ratio >= 0.25m;
    }

    /// <summary>
    /// 修改造成的伤害 - 在伤害计算时应用降低效果
    /// 返回乘数: 1 - 降低比例
    /// </summary>
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 只影响拥有此Power的生物造成的伤害
        if (dealer != Owner)
            return 1m;

        // 只对攻击伤害生效
        if (!props.IsPoweredAttack())
            return 1m;

        // 计算降低比例
        decimal reduction = CalculateReduction(false);

        // 返回乘数: (1 - 降低比例)
        // 例如: 降低25%则返回0.75
        return 1m - reduction;
    }

    /// <summary>
    /// 强制重新计算效果（用于凋亡爆发触发时）
    /// </summary>
    public async Task ForceRecalculate()
    {
        Flash();
        await Task.CompletedTask;
    }

    /// <summary>
    /// 在血条上显示凋亡层数（类似毒和灾厄）
    /// </summary>
    public override IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        // 只显示在拥有此Power的生物血条上
        if (context.Creature != Owner)
            yield break;

        // 如果凋亡层数大于0，显示在血条上
        if (Amount > 0)
        {
            // 凋亡显示为亮粉色条，从左侧开始
            yield return new HealthBarForecastSegment(
                amount: Amount,
                color: ApoptosisColor,
                direction: HealthBarForecastDirection.FromLeft,
                order: 100 // 显示顺序（数字越大越靠后）
            );
        }
    }

    /// <summary>
    /// 在回合结束前检查是否触发斩杀
    /// </summary>
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // 确保是拥有此 Power 的生物所在的阵营回合即将结束
        if (side != base.Owner.Side) return;

        // 检查拥有者是否还存在且存活
        if (!base.Owner.IsAlive) return;

        // 检查凋亡层数是否达到或超过当前生命值
        if (IsOwnerApoptotic())
        {
            // 获取当前回合所有处于凋亡状态的生物
            var apoptoticCreatures = GetApoptoticCreatures(Owner.CombatState?.GetCreaturesOnSide(side));

            // 只有当拥有者是第一个凋亡生物时，才执行斩杀，以防止多个凋亡生物同时触发
            if (apoptoticCreatures.FirstOrDefault() == Owner)
            {
                await ApoptosisKill(apoptoticCreatures);
            }
        }
    }

    /// <summary>
    /// 判断拥有者是否处于凋亡状态 (凋亡层数 >= 当前生命值)
    /// </summary>
    private bool IsOwnerApoptotic()
    {
        return base.Owner.CurrentHp <= base.Amount;
    }

    /// <summary>
    /// 获取指定生物列表中处于凋亡状态的生物
    /// </summary>
    private static IReadOnlyList<Creature> GetApoptoticCreatures(IReadOnlyList<Creature>? creatures)
    {
        if (creatures == null) return new List<Creature>();
        return creatures.Where(c => c.GetPower<ApoptosisPower>()?.IsOwnerApoptotic() ?? false).ToList();
    }

    /// <summary>
    /// 执行凋亡斩杀
    /// </summary>
    private static async Task ApoptosisKill(IReadOnlyList<Creature> creatures)
    {
        if (creatures.Count == 0) return;

        foreach (Creature creature in creatures)
        {
            await CreatureCmd.Kill(creature);
        }
    }
}
