using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Monsters;

/// <summary>
/// 特雷西斯型怪物
/// 生命值180，使用蓄力机制
/// 循环：重新蓄力 -> 兽吼 -> 瘴气 -> 战歌 -> 强力护盾 -> 挥砍 -> 循环
/// 战斗开始时给予玩家 Hex 和 Dampen（仅第一回合生效）
/// </summary>
public sealed class TheresaSwordsmanMonster : CustomMonsterModel
{
    public override int MinInitialHp => 180;

    public override int MaxInitialHp => 180;

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public override bool HasDeathSfx => false;

    public override float DeathAnimLengthOverride => 1.5f;

    // 使用召唤物的攻击音效
    protected override string AttackSfx => "event:/sfx/enemy/enemy_attacks/swordsman_minion/swordsman_minion_attack";

    protected override string CastSfx => "event:/sfx/enemy/enemy_attacks/swordsman_minion/swordsman_minion_cast";

    // 挥砍伤害
    private int SlashDamage => 40;

    // 兽吼伤害
    private int BeastCryDamage => 15;

    // Power Shield 给予的格挡
    private int PowerShieldBlock => 15;

    // Power Shield 给予的力量
    private int PowerShieldStrength => 2;

    // War Chant 给予的力量
    private int WarChantStrength => 3;

    // Miasma 给予的格挡
    private int MiasmaBlock => 8;

    public override string? CustomVisualPath => "res://Theresa/animations/Monsters/TheresaSwordsman.tscn";

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // 初始给予1层蓄力
        await PowerCmd.Apply<ChargingPower>(base.Creature, 1m, base.Creature, null);

        // 第一回合生效：给予玩家 Hex 和 Dampen
        var players = base.Creature.CombatState?.Players;
        if (players != null)
        {
            foreach (var player in players)
            {
                await PowerCmd.Apply<HexPower>(player.Creature, 1m, base.Creature, null);
                var dampen = await PowerCmd.Apply<DampenPower>(player.Creature, 1m, base.Creature, null);
                if (dampen != null)
                {
                    dampen.AddCaster(base.Creature);
                }
            }
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = [];

        // 重新蓄力
        MoveState rechargeMove = new("RECHARGE", RechargeMove, new DefendIntent());

        // 兽吼：造成伤害并施加易伤
        MoveState beastCryMove = new("BEAST_CRY", BeastCryMove, new SingleAttackIntent(BeastCryDamage), new DebuffIntent());

        // 瘴气 - 给玩家-2敏捷，自己+2敏捷和格挡
        MoveState miasmaMove = new("MIASMA", MiasmaMove, new DebuffIntent(), new DefendIntent(), new BuffIntent());

        // 战歌 - 给自己力量
        MoveState warChantMove = new("WAR_CHANT", WarChantMove, new BuffIntent());

        // 强力护盾 - 给自己力量和格挡
        MoveState powerShieldMove = new("POWER_SHIELD", PowerShieldMove, new BuffIntent(), new DefendIntent());

        // 挥砍 - 造成40点伤害
        MoveState slashMove = new("SLASH", SlashMove, new SingleAttackIntent(SlashDamage));

        // 设置循环
        rechargeMove.FollowUpState = beastCryMove;
        beastCryMove.FollowUpState = miasmaMove;
        miasmaMove.FollowUpState = warChantMove;
        warChantMove.FollowUpState = powerShieldMove;
        powerShieldMove.FollowUpState = slashMove;
        slashMove.FollowUpState = rechargeMove;

        list.Add(rechargeMove);
        list.Add(beastCryMove);
        list.Add(miasmaMove);
        list.Add(warChantMove);
        list.Add(powerShieldMove);
        list.Add(slashMove);

        return new MonsterMoveStateMachine(list, rechargeMove);
    }

    /// <summary>
    /// 获取当前蓄力层数并增加
    /// </summary>
    private int GetAndIncrementChargeLevel()
    {
        var chargePower = base.Creature.Powers.OfType<ChargingPower>().FirstOrDefault();
        int level = chargePower != null ? (int)chargePower.Amount : 1;
        // 蓄力层数在1-6之间循环
        int nextLevel = level >= 6 ? 1 : level + 1;
        PowerCmd.Apply<ChargingPower>(base.Creature, nextLevel - level, base.Creature, null);
        return level;
    }

    private async Task RechargeMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play(CastSfx);
        await CreatureCmd.TriggerAnim(base.Creature, "Attack", 0.5f);
        GetAndIncrementChargeLevel();
    }

    private async Task BeastCryMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play(CastSfx);
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0f);
        await Cmd.Wait(0.3f);
        await Cmd.Wait(0.75f);
        await PowerCmd.Apply<RingingPower>(targets, 1m, base.Creature, null);
        GetAndIncrementChargeLevel();
    }

    private async Task PowerShieldMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/magi_knight/magi_knight_cast_shield");
        await CreatureCmd.TriggerAnim(base.Creature, "Attack", 0.6f);
        await CreatureCmd.GainBlock(base.Creature, PowerShieldBlock, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(base.Creature, PowerShieldStrength, base.Creature, null);
        GetAndIncrementChargeLevel();
    }

    private async Task WarChantMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play("event:/sfx/enemy/enemy_attacks/flail_knight/flail_knight_war_chant");
        await CreatureCmd.TriggerAnim(base.Creature, "Attack", 0.5f);
        await PowerCmd.Apply<StrengthPower>(base.Creature, WarChantStrength, base.Creature, null);
        GetAndIncrementChargeLevel();
    }

    private async Task MiasmaMove(IReadOnlyList<Creature> targets)
    {
        SfxCmd.Play(CastSfx);
        await CreatureCmd.TriggerAnim(base.Creature, "Attack", 0.5f);
        var players = base.Creature.CombatState?.Players;
        if (players != null)
        {
            foreach (var player in players)
            {
                await PowerCmd.Apply<DexterityPower>(player.Creature, -2m, base.Creature, null);
            }
        }
        await CreatureCmd.GainBlock(base.Creature, MiasmaBlock, ValueProp.Move, null);
        await PowerCmd.Apply<DexterityPower>(base.Creature, 2m, base.Creature, null);
        GetAndIncrementChargeLevel();
    }

    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SlashDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.5f)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        GetAndIncrementChargeLevel();
    }

    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
    {
        // 待机循环
        AnimState idle = new("C1_Idle", true);
        
        // 攻击
        AnimState attack = new("C1_Attack") { NextState = idle };
        
        // 受伤：C1_Revive_Begin -> C1_Revive_End -> C1_Idle
        AnimState hurtBegin = new("C1_Revive_Begin");
        AnimState hurtEnd = new("C1_Revive_End") { NextState = idle };
        hurtBegin.NextState = hurtEnd;
        
        // 死亡 - 使用 C2_Skill_Die 作为死亡动画，播放完后停在最后一帧，不要回到 idle
        AnimState die = new("C2_Skill_Die");

        // 创建动画器，初始状态为 idle
        CreatureAnimator animator = new(idle, controller);
        
        // 添加任意状态转换
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", attack);
        animator.AddAnyState("ShieldAttack", attack);
        animator.AddAnyState("BreakerAttack", attack);
        animator.AddAnyState("Dead", die);
        animator.AddAnyState("Hit", hurtBegin);

        return animator;
    }
}
