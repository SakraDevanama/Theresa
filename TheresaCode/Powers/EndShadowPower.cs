using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 末影
/// 每有一层MantraPower，降低3点受到的伤害
/// </summary>
public class EndShadowPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 修改受到的伤害 - 每有一层MantraPower降低1点伤害
    /// </summary>
    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 只影响自身受到的伤害
        if (Owner != target) return amount;

        // 获取MantraPower层数
        var mantraPower = Owner.Powers.FirstOrDefault(p => p is MantraPower);
        if (mantraPower == null) return amount;

        int mantraAmount = mantraPower.Amount;
        if (mantraAmount <= 0) return amount;

        // 每有一层MantraPower降低1点伤害
        decimal damageReduction = mantraAmount * 1m;
        
        // 确保伤害不会降到0以下
        return Math.Max(0m, amount - damageReduction);
    }
}
