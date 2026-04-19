using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 缝补生命 (FixLife)
/// 2费能力牌
/// 获得 2（+1）层 虚弱 和 脆弱 。
/// 你拥有的虚弱，脆弱和 易伤 效果反转。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class FixLife() : TheresaCardModel(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    // 基础层数
    private const int BaseAmount = 2;
    // 升级后额外层数
    private const int UpgradeAmountDelta = 1;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<FrailPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("WeakAmount", BaseAmount),
        new DynamicVar("VulnerableAmount", BaseAmount)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        int weakAmount = (int)DynamicVars["WeakAmount"].BaseValue;
        int vulnerableAmount = (int)DynamicVars["VulnerableAmount"].BaseValue;

        // 应用虚弱和脆弱
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, weakAmount, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, vulnerableAmount, Owner.Creature, this);

        // 应用效果反转能力
        await PowerCmd.Apply<FixLifeEffect>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级后层数+1
        DynamicVars["WeakAmount"].UpgradeValueBy(UpgradeAmountDelta);
        DynamicVars["VulnerableAmount"].UpgradeValueBy(UpgradeAmountDelta);
    }
}
