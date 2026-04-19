using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 往昔尘埃
/// 当卡牌成为微尘时获得格挡
/// </summary>
public sealed class PastDustPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task TriggerOnBecomeDust(CardModel card)
    {
        if (Owner?.IsAlive != true) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
    }
}
