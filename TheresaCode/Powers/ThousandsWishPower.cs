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
/// 此Power只负责额外的萦绕触发。
/// 
/// 网络同步：额外萦绕通过 <see cref="DustManager.DustIt"/> -> <see cref="Theresa.TheresaCode.Actions.DustItAction"/>
/// 走 GameAction 队列同步，避免之前 fire-and-forget 导致的 host/client 状态分歧。
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
    /// 每次抽牌后触发 - 计数并检查是否达到阈值。
    /// 达到阈值时通过 GameAction 同步执行额外萦绕。
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

        // 如果达到阈值，重置计数并通过 GameAction 同步执行额外萦绕
        if (data.DrawnCount >= DrawThreshold)
        {
            data.DrawnCount = 0;
            InvokeDisplayAmountChanged();
            Flash();

            int lingerCount = (int)Amount;
            MainFile.Logger?.Info($"[ThousandsWishPower] Threshold reached! Enqueuing {lingerCount} extra linger(s) via DustItAction");

            for (int i = 0; i < lingerCount; i++)
            {
                // AfterCardDrawn 处于 Draw 命令同步路径中，直接同步执行萦绕
                await DustManager.DustItSync(card.Owner, false, false);
            }
        }
    }
}
