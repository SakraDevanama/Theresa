// ==================== CardsDrawnCounterPower.cs ====================
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Commands;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 抽牌计数器 - 每抽12张牌获得1层微尘（跨回合累计）
/// </summary>
public sealed class CardsDrawnCounterPower : TheresaPowerModel
{
    // 内部数据：记录本场战斗累计抽牌数和剩余计数
    private class Data
    {
        public int CardsDrawnTotal;      // 本场战斗累计抽牌数（跨回合）
        public int CardsLeft;            // 距离下次触发还剩多少张牌
    }

    private const int CardsThreshold = 12;   // 每抽12张牌触发
    private const int MantraReward = 1;      // 触发获得1层微尘

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => true;  // 每个实例独立数据

    // UI显示：距离下次触发还差多少张牌
    public override int DisplayAmount => GetInternalData<Data>().CardsLeft;

    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [new DynamicVar("MantraAmount", MantraReward)];

    protected override object InitInternalData()
    {
        return new Data 
        { 
            CardsDrawnTotal = 0,
            CardsLeft = CardsThreshold  // 初始需要12张
        };
    }

    /// <summary>
    /// 每次抽牌后触发 - 立即计算并给予Mantra
    /// </summary>
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        // 只统计Power拥有者的抽牌
        if (card.Owner?.Creature != Owner) return;

        var data = GetInternalData<Data>();
        
        // 累加本场战斗总抽牌数
        data.CardsDrawnTotal++;
        
        // 减少剩余计数
        data.CardsLeft--;
        
        // 通知UI更新显示数字
        InvokeDisplayAmountChanged();

        // 如果达到阈值，立即给予Mantra并重置计数
        if (data.CardsLeft <= 0)
        {
            Flash();
            
            // 立即给予MantraPower
            await PowerCmd.Apply<MantraPower>(
                Owner,                          // 目标：Power拥有者
                MantraReward,                   // 层数：1
                Owner,                          // 施加者：自己
                null                            // 卡牌来源：无（由Power触发）
            );
            
            // 重置计数
            data.CardsLeft = CardsThreshold;
            InvokeDisplayAmountChanged();
        }
    }

    public int GetCardsDrawnTotal() => GetInternalData<Data>().CardsDrawnTotal;
}