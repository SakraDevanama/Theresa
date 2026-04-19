using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Powers; 



public class EnergyReboundPower : TheresaPowerModel
{
    // 用于存储此 Power 实例的私有数据
    private class Data
    {
        public int EnergySpentThisTurn; // 记录本回合总消耗的能量
    }

    private const int EnergyThreshold = 3; // 消耗能量的阈值
    private const int EnergyReward = 2;   // 触发后获得的能量

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 计数器类型，可以叠加层数来影响效果

    // 在界面上显示距离下次触发还需要多少能量
    public override int DisplayAmount => EnergyThreshold - (GetInternalData<Data>().EnergySpentThisTurn % EnergyThreshold);

    // Power 可以叠加，每一层都会独立计算和触发
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced; 

    // 定义动态变量，用于描述 Power 效果中的可变数值
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(EnergyReward)];

    // 初始化私有数据
    protected override object InitInternalData()
    {
        return new Data { EnergySpentThisTurn = 0 };
    }

    // 当拥有此 Power 的角色消耗能量后，会触发此方法
    public override async Task AfterEnergySpent(CardModel card, int amount)
    {
        // 确保是 Power 拥有者（也就是玩家）消耗了能量
        if (card.Owner.Creature == Owner)
        {
            var data = GetInternalData<Data>();
            data.EnergySpentThisTurn += amount; // 累加消耗的能量
            
            // 计算本次能量消耗后，总共触发了多少次效果
            // 例如，之前累计消耗了1点，这次又消耗了5点，总共6点。6/3=2次，而之前是0次，所以新增了2次触发。
            int totalPossibleTriggers = data.EnergySpentThisTurn / EnergyThreshold;
            int timesTriggeredThisTurn = totalPossibleTriggers; // 因为 IsInstanced 为 true，我们只关心当前实例的触发次数

            // 检查是否达到了新的触发点
            if (timesTriggeredThisTurn > (data.EnergySpentThisTurn - amount) / EnergyThreshold)
            {
                Flash(); // 触发时闪烁特效
                
                // 获得能量奖励 (Power的层数 * 每次奖励)
                if (Owner.Player != null) await PlayerCmd.GainEnergy(Amount * EnergyReward, Owner.Player);
            }
            
            // 通知UI更新显示的数字
            InvokeDisplayAmountChanged();
        }
    }
}