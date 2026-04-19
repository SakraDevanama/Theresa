using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Dust;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 万千愿景Power
/// 每抽13张牌，额外萦绕指定次数。
/// 
/// 注意：基础萦绕（每回合1次）由 DustManager.AtTurnStartPostDraw 处理
/// 此Power只负责额外的萦绕触发
/// </summary>
public sealed class ThousandsWishPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    private const int DrawThreshold = 13;

    // 内部数据：记录剩余抽牌数
    private class Data
    {
        public int DrawnCount;      // 已抽牌计数
    }

    public override int DisplayAmount => GetInternalData<Data>().DrawnCount;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Amount", 1)];

    protected override object InitInternalData()
    {
        return new Data { DrawnCount = 0 };
    }

    /// <summary>
    /// 每次抽牌后触发 - 计数并检查是否达到阈值
    /// 达到阈值时延迟执行额外萦绕（等待当前抽牌动画完成）
    /// </summary>
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        // 只统计Power拥有者的抽牌
        if (card.Owner?.Creature != Owner) return;

        var data = GetInternalData<Data>();
        
        // 累加抽牌计数
        data.DrawnCount++;
        
        MainFile.Logger?.Info($"[ThousandsWishPower] AfterCardDrawn: count={data.DrawnCount}/{DrawThreshold}");
        
        // 通知UI更新显示数字
        InvokeDisplayAmountChanged();

        // 如果达到阈值，重置计数并延迟执行额外萦绕
        if (data.DrawnCount >= DrawThreshold)
        {
            data.DrawnCount = 0;
            InvokeDisplayAmountChanged();
            Flash();
            
            int lingerCount = (int)Amount;
            MainFile.Logger?.Info($"[ThousandsWishPower] Threshold reached! Triggering {lingerCount} extra linger(s)");
            
            // 延迟执行额外萦绕，等待当前抽牌动画完成
            _ = TriggerExtraLingerDelayed(lingerCount);
        }
    }

    /// <summary>
    /// 延迟触发额外萦绕效果（在当前抽牌动画完成后执行）
    /// 调用 DustManager.DustIt 从微尘中随机打出一张牌，与基础萦绕一致
    /// </summary>
    private async Task TriggerExtraLingerDelayed(int lingerCount)
    {
        // 等待抽牌动画完成
        await Cmd.Wait(0.8f);
        
        for (int i = 0; i < lingerCount; i++)
        {
            MainFile.Logger?.Info($"[ThousandsWishPower] Extra linger {i+1}/{lingerCount}");
            // 调用 DustManager.DustIt 从微尘中随机选牌打出（与基础萦绕相同）
            await DustManager.DustIt(false, false);
            if (i < lingerCount - 1)
                await Cmd.Wait(0.3f);
        }
    }
}
