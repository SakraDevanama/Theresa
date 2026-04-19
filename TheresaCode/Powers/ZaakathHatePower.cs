using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 众萨卡兹的恨意
/// 被动效果 (回合结束后触发):
/// 如果自身的层数比特蕾西娅的希望多，且希望层数大于0，则移除1层希望，并将1层转化为恨意。
///
/// 主动效果 (获得层数时触发):
/// 每当你获得[red]恨意[/red]时，对所有敌方单位造成{Amount}点伤害（至多5点）。
/// </summary>
public class ZaakathHatePower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 伤害上限
    private const int MAX_DAMAGE_PER_GAIN = 5;

    // 内部数据：记录本回合从HeroesAndOverlordsPower获得的额外层数
    private class Data
    {
        public int ExtraFromHeroesAndOverlords;
    }

    protected override object InitInternalData() => new Data { ExtraFromHeroesAndOverlords = 0 };
    private Data GetData() => GetInternalData<Data>();

    public int GetEffectiveAmount()
    {
        return this.Amount - GetData().ExtraFromHeroesAndOverlords;
    }

    public void RecordExtraFromHeroesAndOverlords(int amount)
    {
        GetData().ExtraFromHeroesAndOverlords += amount;
    }

    // --- 主动逻辑：在Power层数发生变化后触发伤害 ---
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this) return;
        if (amount <= 0) return;
        if (Owner == null) return;

        // 获取所有存活的敌方单位
        var enemies = CombatState.GetOpponentsOf(Owner).Where(c => c.IsAlive).ToList();

        // 关键：根据当前总层数造成伤害（上限5）
        int damageAmount = Math.Min(this.Amount, MAX_DAMAGE_PER_GAIN);

        // 对所有敌人造成一次等于当前层数（上限5）的伤害
        foreach (var enemy in enemies)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                enemy,
                damageAmount,
                ValueProp.Unpowered,
                Owner,
                null
            );
        }
    }

    // --- 核心逻辑：在拥有者回合结束后结算 ---
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Side != side) return;
        if (Owner == null) return;

        var data = GetData();
        int effectiveHateAmount = GetEffectiveAmount();

        var theresiasHopePower = Owner.Powers.FirstOrDefault(p => p is TheresiasHopePower) as TheresiasHopePower;
        int effectiveHopeAmount = theresiasHopePower?.GetEffectiveAmount() ?? 0;

        // 如果恨意有效层数大于希望有效层数，且希望有效层数大于0
        if (effectiveHateAmount > effectiveHopeAmount && effectiveHopeAmount > 0)
        {
            if (theresiasHopePower != null)
            {
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), theresiasHopePower, -1, null, null);
            }
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, 1, null, null);
        }

        data.ExtraFromHeroesAndOverlords = 0;
    }
}