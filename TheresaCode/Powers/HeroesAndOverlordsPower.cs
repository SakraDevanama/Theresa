using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Godot;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 英雄与魔王
/// 持续整个战斗
/// 每当你获得希望或恨意时，立即额外获得1层相同的Power
/// 回合结束转化时，额外层数不参与转化计算
/// </summary>
public sealed class HeroesAndOverlordsPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => false;

    /// <summary>
    /// 显示层数（本回合额外获得的希望/恨意总数）
    /// </summary>
    public override int DisplayAmount => GetInternalData<Data>().ExtraGainedThisTurn;

    private class Data
    {
        public int ExtraHopeGained;      // 本回合额外获得的希望层数
        public int ExtraHateGained;      // 本回合额外获得的恨意层数
        public int ExtraGainedThisTurn => ExtraHopeGained + ExtraHateGained;
        public bool IsProcessing;        // 防止递归的标志
    }

    protected override object InitInternalData() => new Data { 
        ExtraHopeGained = 0, 
        ExtraHateGained = 0,
        IsProcessing = false
    };
    private Data GetData() => GetInternalData<Data>();

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    /// <summary>
    /// 监控希望/恨意Power的层数变化
    /// 当玩家获得希望或恨意时，立即额外给予1层相同的Power
    /// 同时通知目标Power记录额外层数，以便回合结束转化时排除
    /// </summary>
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 只处理增加层数的情况
        if (amount <= 0) return;
        
        // 确保是Power拥有者的Power发生变化
        if (power.Owner != Owner) return;
        
        // 确保是玩家自己施加的（避免无限循环）
        if (applier?.Player != Owner?.Player) return;

        var data = GetData();
        
        // 防止递归
        if (data.IsProcessing) return;

        // 检测是否是希望Power
        if (power is TheresiasHopePower hopePower)
        {
            data.IsProcessing = true;
            data.ExtraHopeGained++;
            
            // 通知TheresiasHopePower记录额外层数
            hopePower.RecordExtraFromHeroesAndOverlords(1);
            
            Flash();
            InvokeDisplayAmountChanged();
            
            // 立即额外给予1层希望
            await PowerCmd.Apply<TheresiasHopePower>(Owner, 1, Owner, null, true);
            data.IsProcessing = false;
        }
        // 检测是否是恨意Power
        else if (power is ZaakathHatePower hatePower)
        {
            data.IsProcessing = true;
            data.ExtraHateGained++;
            
            // 通知ZaakathHatePower记录额外层数
            hatePower.RecordExtraFromHeroesAndOverlords(1);
            
            Flash();
            InvokeDisplayAmountChanged();
            
            // 立即额外给予1层恨意
            await PowerCmd.Apply<ZaakathHatePower>(Owner, 1, Owner, null, true);
            data.IsProcessing = false;
        }
    }

    /// <summary>
    /// 回合结束时重置计数器
    /// </summary>
    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Side != side) return Task.CompletedTask;

        var data = GetData();

        // 重置计数器
        if (data.ExtraHopeGained > 0 || data.ExtraHateGained > 0)
        {
            data.ExtraHopeGained = 0;
            data.ExtraHateGained = 0;
            InvokeDisplayAmountChanged();
        }
        
        return Task.CompletedTask;
    }
}
