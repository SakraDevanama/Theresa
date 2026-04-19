using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Enchantments;

/// <summary>
/// 茧笼丝线（原 NormalSilk）
/// 
/// 回合结束时效果：
/// - 攻击牌：对随机敌人造成 {Amount} 点伤害
/// - 技能牌：获得 {Amount} 点格挡
/// 
/// 替换关系：可替换意志丝线
/// </summary>
public class CocoonSilkEnchantment : AbstractSilkEnchantment
{
    protected override string? CustomIconPath => "res://Theresa/images/icons/silk_thread.png";

    public CocoonSilkEnchantment()
    {
        BaseAmount = 3;
        Amount = 3;
    }

    /// <summary>
    /// 回合结束效果
    /// </summary>
    public override async Task AtTurnEnd(PlayerChoiceContext choiceContext, PileType pileType)
    {
        if (Card?.Owner?.Creature == null) return;
        if (pileType != PileType.Hand && pileType != PileType.None) return; // None 代表微尘

        var owner = Card.Owner.Creature;

        // 播放卡牌闪光
        PlayCardFlash();

        if (Card.Type == CardType.Attack)
        {
            // 攻击牌：对随机敌人造成伤害
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
        }
        else if (Card.Type == CardType.Skill)
        {
            // 技能牌：获得格挡
            await CreatureCmd.GainBlock(owner, Amount, ValueProp.Move, null);
        }

        TriggeredOnce();
    }

    /// <summary>
    /// 茧笼可以替换意志丝线
    /// </summary>
    public override bool CanReplace(AbstractSilkEnchantment silkToReplace)
    {
        return silkToReplace is MindSilkEnchantment;
    }

    /// <summary>
    /// 播放卡牌闪光动画
    /// </summary>
    private static void PlayCardFlash()
    {
        // 闪光动画由 SilkSpreadPower 统一处理
    }
}
