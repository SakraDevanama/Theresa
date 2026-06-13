using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Cards;

namespace Theresa.TheresaCode.Powers;

// 茧缚
public sealed class SilkCocoon : TheresaPowerModel
{
    private const int DamagePerStack = 3;
    private const int MaxStacks = 10;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // --- 主动逻辑：在Power层数发生变化后，立即造成伤害并检查层数 ---
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 确保是自身层数发生了变化
        if (power != this) return;

        // 检查持有者是否仍然存活，如果已死亡则不执行任何逻辑
        if (Owner == null || !Owner.IsAlive) return;

        // 1. 检查层数是否达到或超过最大值，如果达到则移除该buff并对持有者施加Broken
        if (Amount >= MaxStacks)
        {
            // 为了防止无限递归，先移除掉层数，再施加Broken
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -MaxStacks, Owner, null);
            await PowerCmd.Apply<Broken>(new ThrowingPlayerChoiceContext(), Owner, 1m, Owner, null);
            // 层数变化后会再次触发 AfterPowerAmountChanged，此时 Amount < MaxStacks，会进入下面的伤害逻辑
        }

        // 2. 对持有该buff的人自己造成伤害 (基于当前层数)
        var currentDamage = DamagePerStack * Amount;
        if (currentDamage > 0)
        {
            // 再次检查持有者是否仍然存活（可能在上面逻辑中已死亡）
            if (Owner == null || !Owner.IsAlive) return;

            // 使用 ThrowingPlayerChoiceContext，参考 ZaakathHatePower 的写法
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(), // 使用临时上下文
                new List<Creature> { Owner },      // 伤害目标是持有者自己
                currentDamage,                     // 伤害值
                ValueProp.Unpowered,               // 伤害无特殊属性
                Owner,                             // 伤害来源是持有者自己
                null                               // 卡牌来源为空
            );
            Flash(); // 触发视觉反馈

            // 3. 检查是否有"编织来日"或"痛觉相连"效果，有则额外触发
            await TriggerExtraEffects();
        }
    }
    
    /// <summary>
    /// 检查并触发"编织来日"和"痛觉相连"的额外效果
    /// </summary>
    private async Task TriggerExtraEffects()
    {
        if (Owner == null) return;

        var combatState = Owner.CombatState;
        if (combatState == null) return;

        var totalExtraTriggers = 0;

        // 遍历所有玩家查找"编织来日"和"痛觉相连"效果
        foreach (var player in combatState.Players)
        {
            if (player?.Creature == null) continue;

            // 1. 在玩家身上查找"编织来日"效果
            var weaveTomorrowEffect = player.Creature.Powers.FirstOrDefault(p => p is WeaveTomorrowEffect) as WeaveTomorrowEffect;
            if (weaveTomorrowEffect != null)
            {
                totalExtraTriggers += (int)weaveTomorrowEffect.Amount;
            }

            // 2. 在玩家身上查找"痛觉相连"效果
            var painfulConnectionEffect = player.Creature.Powers.FirstOrDefault(p => p is PainfulConnectionEffect) as PainfulConnectionEffect;
            if (painfulConnectionEffect != null)
            {
                totalExtraTriggers += (int)painfulConnectionEffect.Amount;
            }
        }

        if (totalExtraTriggers <= 0) return;

        // 额外触发N次SilkCocoon伤害（不修改层数，只是额外造成伤害）
        var currentDamage = DamagePerStack * Amount;
        for (int i = 0; i < totalExtraTriggers; i++)
        {
            if (Owner == null || !Owner.IsAlive) return;

            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                new List<Creature> { Owner },
                currentDamage,
                ValueProp.Unpowered,
                Owner,
                null
            );
            Flash();
        }
    }
}