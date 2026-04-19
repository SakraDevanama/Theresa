using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Commands;
using Theresa.TheresaCode.Extensions;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Stances;

/// <summary>
/// 灾厄姿态 (Disaster Stance)
/// 
/// 效果：
/// 1. 受到的攻击伤害提升20%
/// 2. 受到伤害后自动退出姿态
/// 3. 退出姿态时获得灾厄之力（DisasterPower）：受到伤害降低20%，持续1回合
/// 
/// Java 原版：
/// - atDamageReceive: NORMAL 伤害 ×1.2
/// - DisasterPatch: 受到伤害且 lastDamageTaken>0 时退出姿态
/// - onExitStance: 获得 DisasterPower
/// </summary>
public class DisasterStance : StancePower
{
    protected override Color? BodyTint => new Color(1.3f, 0.5f, 0.3f);
    protected override Color? ScreenFlashColor => new Color(0.9f, 0.2f, 0.1f);
    protected override ShakeStrength ScreenShakeStrength => ShakeStrength.Weak;
    protected override string? EnterSfxPath => "event:/sfx/stance_enter_wrath";
    
// 自动生成小图标路径：使用类名（去掉前缀）+ .png + 扩展方法
    public override string CustomPackedIconPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();

    // 大图标复用小图标
    public override string CustomBigIconPath => CustomPackedIconPath;
    /// <summary>
    /// 受到的攻击伤害提升20%
    /// 在计算格挡后、实际扣血前修改失去的生命值
    /// </summary>
    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 只影响攻击伤害（带 Move 标记的伤害）
        if (target == Owner && props.HasFlag(ValueProp.Move) && amount > 0)
        {
            return amount * 1.2m;
        }
        return amount;
    }

    /// <summary>
    /// 受到伤害后：如果实际受到了伤害，退出灾厄姿态
    /// 对应原版 DisasterPatch：lastDamageTaken > 0 时退出
    /// </summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 只处理拥有者自己受到伤害
        if (target != Owner) return;

        // 如果实际受到了未格挡的伤害，退出姿态
        if (result.UnblockedDamage > 0)
        {
            MainFile.Logger?.Info($"[DisasterStance] Took {result.UnblockedDamage} damage, exiting stance");
            await StanceCmd.ExitStance(Owner, null);
        }
    }

    /// <summary>
    /// 退出姿态时获得灾厄之力
    /// </summary>
    public override async Task OnExitStance(Creature creature)
    {
        // 先执行基类的退出逻辑（移除VFX等）
        await base.OnExitStance(creature);

        // 获得灾厄之力：受到伤害降低20%，持续1回合
        if (creature.IsAlive)
        {
            await PowerCmd.Apply<DisasterPower>(new ThrowingPlayerChoiceContext(), new[] { creature }, 1, creature, null);
            MainFile.Logger?.Info($"[DisasterStance] Applied DisasterPower on exit");
        }
    }
}
