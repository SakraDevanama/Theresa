using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 回响
/// 2费（升级后2费）能力牌
/// 获得1层（升级后2层）回响：回合开始时额外触发一次萦绕
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class Echoism() : TheresaCardModel(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    private const bool shouldShowInCardLibrary = true;
    private const int BaseAmount = 1;
    // public override string? CustomSpinePortraitScenePath => "res://Theresa/animations/cards/unique.tscn";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        LingerKeyword.Linger,
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Amount", BaseAmount)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(LingerKeyword.Linger),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature != null)
        {
            int amount = (int)DynamicVars["Amount"].BaseValue;
            await PowerCmd.Apply<EchoismPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, amount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1m);
    }
}
