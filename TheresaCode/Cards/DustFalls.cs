using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 尘埃落定 (DustFalls)
/// 1费能力牌
/// 获得2层希望。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class DustFalls() : TheresaCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    private const int BaseAmount = 2;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<TheresiasHopePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HopeAmount", BaseAmount)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        int hopeAmount = (int)DynamicVars["HopeAmount"].BaseValue;

        // 获得希望
        await PowerCmd.Apply<TheresiasHopePower>(Owner.Creature, hopeAmount, Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        DynamicVars["HopeAmount"].UpgradeValueBy(1);
    }
}
