// 引入 Godot 引擎核心类型（如 Color）

using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.ValueProps;
// 引入生物实体（角色/敌人）

// 引入动态变量系统（用于本地化和数值配置）

// 引入核心模型（如 CardModel）

// 引入屏幕特效工具（闪光、震动等）

// 引入伤害属性标志（如 Unpowered）

// 声明命名空间：守望者的姿态模块
namespace Theresa.TheresaCode.Stances;

// 定义“愤怒姿态”类，继承自通用姿态基类 StancePower

public sealed class WrathStance : StancePower
{
    // 定义一个常量键名，用于在动态变量系统中标识“伤害倍率”
    private const string DamageMultiplier = "DamageMultiplier";

    // 重写光环特效场景路径：指向愤怒姿态的 VFX 场景文件
    //protected override string AuraScenePath => "res://Watcher/scenes/watcher_mod/vfx/wrath_aura.tscn";

    // 重写进入音效路径
    //protected override string EnterSfxPath => "res://Watcher/audio/wrath_enter.ogg";

    // 重写循环环境音效路径（持续播放的背景怒吼声）
    //protected override string AmbienceLoopPath => "res://Watcher/audio/wrath_loop.ogg";

    // 重写屏幕闪光颜色：使用鲜红色（R=1, G=0.15, B=0.1），象征愤怒
    protected override Color? ScreenFlashColor => new Color(1f, 0.15f, 0.1f);

    // 重写屏幕震动强度：中等（Medium），比神威弱，但仍有冲击感
    protected override ShakeStrength ScreenShakeStrength => ShakeStrength.Medium;

    // 定义该姿态的“标准动态变量”集合
    // 这些变量可用于：
    //   - 卡牌描述本地化（如 "{DamageMultiplier}x 伤害"）
    //   - 游戏内数值配置（未来可从配置文件读取）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new(DamageMultiplier, 2m)]; // 表示默认造成 2 倍伤害

    // 重写 ModifyDamageMultiplicative 方法：修改伤害倍率
    public override decimal ModifyDamageMultiplicative(
        Creature? target,           // 伤害目标
        decimal amount,             // 原始伤害值（此处未使用）
        ValueProp props,            // 伤害属性（如是否可被强化）
        Creature? dealer,           // 伤害来源（攻击者）
        CardModel? cardSource)      // 触发伤害的卡牌
    {
        // 判断条件：
        //   - 如果是 **自己造成的伤害**（dealer == Owner）
        //     **或**
        //   - 是 **对自己造成的伤害**（target == Owner，例如反伤、自伤效果）
        //   - 并且该伤害 **不是“不可强化”的**（Unpowered）
        // 则应用愤怒姿态的伤害倍率
        if ((dealer == Owner || target == Owner) && !props.HasFlag(ValueProp.Unpowered))
            // 从动态变量系统中读取 DamageMultiplier 的基础值（这里是 2）
            return DynamicVars[DamageMultiplier].BaseValue;

        // 否则，不修改伤害（返回 1 倍）
        return 1m;
    }
}