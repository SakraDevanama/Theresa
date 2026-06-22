using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Enchantments;

/// <summary>
/// 泪水丝线（原 TearSilk）
/// 
/// 回合结束时效果：
/// - 同时造成 {Amount} 点伤害 和 获得 {Amount} 点格挡
/// 
/// 替换关系：可替换茧笼丝线和意志丝线
/// </summary>
public class TearSilkEnchantment : AbstractSilkEnchantment
{
    protected override string? CustomIconPath => "res://Theresa/images/icons/silk_thread5.png";

    public TearSilkEnchantment()
    {
        BaseAmount = 2;
    }

    /// <summary>
    /// 回合结束效果：同时打伤+获格挡
    /// </summary>
    public override async Task AtTurnEnd(PlayerChoiceContext choiceContext, PileType pileType)
    {
        if (Card?.Owner?.Creature == null) return;
        if (pileType != PileType.Hand && pileType != PileType.None) return;

        var owner = Card.Owner.Creature;

        // 播放卡牌闪光
        PlayCardFlash();

        // 对随机敌人造成伤害
        var combatState = owner.CombatState;
        if (combatState != null)
        {
            var enemies = combatState.HittableEnemies.ToList();
            if (enemies.Any())
            {
                var target = Card.Owner.RunState.Rng.CombatTargets.NextItem(enemies);
                if (target != null)
                {
                    await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Unpowered | ValueProp.Move, owner, null);
                }
            }
        }

        // 获得格挡
        await CreatureCmd.GainBlock(owner, Amount, ValueProp.Move, null);

        TriggeredOnce();
    }

    /// <summary>
    /// 泪水可以替换茧笼和意志
    /// </summary>
    public override bool CanReplace(AbstractSilkEnchantment silkToReplace)
    {
        return silkToReplace is CocoonSilkEnchantment or MindSilkEnchantment;
    }

    private static void PlayCardFlash()
    {
        // 闪光动画由 SilkSpreadPower 统一处理
    }
}
