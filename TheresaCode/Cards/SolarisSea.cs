using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 索拉里斯之海 (SolarisSea)
/// 3费能力牌（升级后2费）
/// 罕见
/// 不可叠加。
/// 主动打出牌后，选择抽牌堆中1张费用更低的牌放入手中并免费打出。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class SolarisSea() : TheresaCardModel(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SolarisSeaPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        // 应用索拉里斯之海能力
        await PowerCmd.Apply<SolarisSeaPower>(Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级后费用-1
        EnergyCost.UpgradeBy(-1);
    }
}
