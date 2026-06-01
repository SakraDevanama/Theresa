using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 往昔尘埃
/// 1费（升级后1费）能力牌
/// 获得2层（升级后3层）往昔尘埃：当卡牌成为微尘时获得格挡
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class PastDust() : TheresaCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    private const int BaseAmount = 2;
    private const bool shouldShowInCardLibrary = false;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<PastDustPower>(),
    ];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Amount", BaseAmount)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature != null)
        {
            await PowerCmd.Apply<PastDustPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, BaseAmount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1m);
    }
}
