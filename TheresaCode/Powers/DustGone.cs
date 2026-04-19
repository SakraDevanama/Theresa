using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 逝尘
/// 当受到攻击伤害时，将自身受到的伤害量变为0并获得等量伤害数的凋亡层数。
/// </summary>
public class DustGone : TheresaPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 防递归标志 - 避免凋亡触发其他效果导致死循环
    private bool _isProcessing = false;

    /// <summary>
    /// 在失去生命值前拦截 - 立即给予凋亡
    /// </summary>
    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 只处理自己受到的伤害
        if (target != Owner) return amount;
        
        // 只处理来自敌人的伤害
        if (dealer == null || dealer == Owner) return amount;
        
        // 防递归
        if (_isProcessing) return amount;

        int damage = (int)amount;
        if (damage <= 0) return amount;

        // 设置标志
        _isProcessing = true;

        // ✅ FireAndForget 立即给予凋亡（不等待）
        _ = Task.Run(async () =>
        {
            try
            {
                if (Owner != null)
                {
                    await PowerCmd.Apply<ApoptosisPower>(new ThrowingPlayerChoiceContext(), 
                        Owner,
                        damage,
                        Owner,  // 伤害来源是自己（自伤转化）
                        null
                    );
                }
            }
            finally
            {
                _isProcessing = false;
            }
        });

        // 将伤害变为0
        return 0m;
    }
}