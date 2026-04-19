using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Commands;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Powers;

public sealed class MantraPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DivinityStance>()
    ];

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        var player = Owner.Player;

        if (power is not MantraPower || player == null)
            return;

        if (applier != null && !applier.IsPlayer)
            return;

        if (amount <= 0)
            return;

        // 如果已经处于魔王残响状态，不扣除微尘
        if (Owner.HasPower<DivinityStance>())
            return;

        var triggers = Amount / 14;
        if (triggers <= 0) return;

        var totalCost = triggers * 8m;
        totalCost = Math.Min(totalCost, Amount); // 防止扣除后变成负数
        if (totalCost <= 0) return;

        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -totalCost, Owner, cardSource);
        await StanceCmd.EnterDivinity(player.Creature, cardSource);

        // 兜底保护：防止其他代码直接把 Amount 减到负数
        if (Amount < 0)
        {
            SetAmount(0, silent: true);
        }
    }
}
