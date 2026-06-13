using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Minion;
using Theresa.TheresaCode.Minions.Cards;
using Theresa.TheresaCode.Minions.Powers;
using Theresa.TheresaCode.Minions.Interfaces;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Theresa.TheresaCode.Minions.Models;

/// <summary>
/// 特雷西斯随从
/// 约誓之剑 - 攻击型随从
/// </summary>
public sealed class SwordsmanMinion : MinionModel
{
    public override int MinInitialHp => 10;

    public override int MaxInitialHp => 99;

    // 使用特雷西斯的Spine动画资源
    protected override string VisualsPath => "res://Theresa/animations/Minion/Swordsman.tscn";

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        // 定义动画状态（根据实际的Spine动画名称）
        // 可用动画: C1_Idle, C1_Move, C1_Attack, C1_Default, C2_Skill_Die, C2_Idle
        
        AnimState idle = new("C1_Idle", true);                    // 待机循环
        AnimState attack = new("C1_Attack") { NextState = idle }; // 攻击
        AnimState hurt = new("C1_Default") { NextState = idle };  // 受伤（使用Default作为受伤恢复）
        AnimState die = new("C2_Skill_Die");                      // 死亡
        AnimState deadLoop = new("C2_Idle", true);                // 死亡循环（使用C2_Idle）
        AnimState summon = new("C1_Move") { NextState = idle };   // 召唤入场动画

        // 设置分支 - 当受到"Hit"事件时切换到受伤动画
        idle.AddBranch("Hit", hurt);
        attack.AddBranch("Hit", hurt);
        hurt.AddBranch("Hit", hurt);
        
        // 死亡后进入死亡循环
        die.NextState = deadLoop;

        // 创建动画器，初始状态为 idle
        CreatureAnimator animator = new(idle, controller);
        
        // 添加任意状态转换
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", attack);
        animator.AddAnyState("Dead", die);
        animator.AddAnyState("Summon", summon);
        
        return animator;
    }

    public override async Task OnSummon(PlayerChoiceContext choiceContext, Player owner, MinionSummonOptions options)
    {
        // 设置血量
        if (options.MaxHp is decimal maxHp)
            await CreatureCmd.SetMaxAndCurrentHp(this.Creature, maxHp);

        // 应用力量
        if (options.PrimaryStatAmount is decimal strength && strength > 0m)
            await PowerCmd.Apply<StrengthPower>(choiceContext, this.Creature, strength, owner.Creature, options.Source, false);

        // 应用挥砍行动（初始1层，每回合增加1层，4层后可手动触发造成40点伤害）
        await PowerCmd.Apply<SwordsmanSlashAction>(choiceContext, this.Creature, 1m, owner.Creature, options.Source, false);

        // 给予玩家绑定特雷西斯的"卫护"牌
        await GiveGuardianSlashCard(owner);
    }

    /// <summary>
    /// 给予玩家绑定此随从的"卫护"牌
    /// </summary>
    private async Task GiveGuardianSlashCard(Player owner)
    {
        try
        {
            var combatState = this.Creature.CombatState;
            if (combatState == null) return;

            // 创建绑定随从的"卫护"牌
            var card = combatState.CreateCard<GuardianSlashBound>(owner);

            // 绑定到特雷西斯
            card.BindMinion(this.Creature);

            MainFile.Logger?.Info($"[SwordsmanMinion] Created GuardianSlashBound card bound to {this.Creature.Name}");

            // 加入手牌
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, null);
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Info($"[SwordsmanMinion] Error giving GuardianSlashBound card: {ex.Message}");
        }
    }
}
