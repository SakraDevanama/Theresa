using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 千丝万缕（SilkPower）
/// 
/// 效果：你的丝线的数值提升 {Amount} 点。
/// 
/// 对应原版 Java 的 SilkPower。
/// 所有继承自 AbstractSilkEnchantment 的丝线在 ApplyPowers() 时会读取此 Power 的层数，
/// 并叠加到自身的 Amount 上。
/// </summary>
public class SilkPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public SilkPower()
    {
    }
}
