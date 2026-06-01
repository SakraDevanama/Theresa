using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 特蕾西娅的希望
/// 被动效果 (回合结束后触发):
/// 回合结束时，若[green]希望[/green]层数多于[red]恨意[/red]，将1层[red]恨意[/red]转化为[green]希望[/green]。
///
/// 主动效果 (获得层数时触发):
/// 每当你获得[green]希望[/green]时，获得{Amount}点格挡（至多6点）。
/// </summary>
public class TheresiasHopePower : TheresaPowerModel
{ 
    public override PowerType Type => PowerType.Buff; 
    public override PowerStackType StackType => PowerStackType.Counter; 

    // 格挡获取上限
    private const int MAX_BLOCK_PER_GAIN = 6;

    // 内部数据：记录本回合从HeroesAndOverlordsPower获得的额外层数
    private class Data
    {
        public int ExtraFromHeroesAndOverlords;  // 从英雄与魔王获得的额外层数
    }

    protected override object InitInternalData() => new Data { ExtraFromHeroesAndOverlords = 0 };
    private Data GetData() => GetInternalData<Data>();

    /// <summary>
    /// 获取用于转化逻辑的有效层数（排除从HeroesAndOverlordsPower获得的额外层数）
    /// </summary>
    public int GetEffectiveAmount()
    {
        return this.Amount - GetData().ExtraFromHeroesAndOverlords;
    }

    /// <summary>
    /// 记录从HeroesAndOverlordsPower获得的额外层数
    /// </summary>
    public void RecordExtraFromHeroesAndOverlords(int amount)
    {
        GetData().ExtraFromHeroesAndOverlords += amount;
    }

    // 不需要额外定义 CanonicalVars，直接使用内置的 Amount

    // --- 主动逻辑：在Power层数发生变化后触发格挡 ---
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 确保是当前实例的层数发生了变化
        if (power != this) return;

        // 检查是否是增加层数 (amount > 0)
        if (amount > 0)
        {
            // 检查拥有者是否存在
            if (Owner == null) return;

            // 关键：根据当前总层数获得格挡（上限6）
            // 当前总层数 = this.Amount
            int actualBlock = Math.Min(this.Amount, MAX_BLOCK_PER_GAIN);

            // 获得格挡
            await CreatureCmd.GainBlock(
                Owner,
                new BlockVar("THERESIAS_HOPE_GAINED_BLOCK", actualBlock, ValueProp.Unpowered),
                null
            );
        }
    }

    // --- 被动逻辑：在拥有者回合结束后执行转化效果 ---
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants) 
    { 
        // 确保是拥有此 Power 的生物回合结束了 
        if (Owner?.Side != side) return; 

        // 如果拥有者为空，则直接返回 
        if (Owner == null) return; 

        var data = GetData();

        // 获取用于转化的有效层数（排除额外层数）
        int effectiveHopeAmount = GetEffectiveAmount();

        // 获取众萨卡兹的恨意 Power 的有效层数
        var zaakathHatePower = Owner.Powers.FirstOrDefault(p => p is ZaakathHatePower) as ZaakathHatePower; 
        int effectiveHateAmount = zaakathHatePower?.GetEffectiveAmount() ?? 0; 

        // 回合结束时，若[green]希望[/green]层数多于[red]恨意[/red]，将1层[red]恨意[/red]转化为[green]希望[/green]
        if (effectiveHopeAmount > effectiveHateAmount && effectiveHateAmount > 0) 
        { 
            if (zaakathHatePower != null)
            {
                // 1. 移除1层[red]恨意[/red]
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), zaakathHatePower, -1, null, null); 
                // 2. 增加1层[green]希望[/green]
                await PowerCmd.Apply<TheresiasHopePower>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null); 
            }
        } 

        // 重置额外层数记录
        data.ExtraFromHeroesAndOverlords = 0;
    } 
}