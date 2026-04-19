using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 蓄力 - 记录怪物当前的蓄力层数
/// </summary>
public sealed class ChargingPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}
