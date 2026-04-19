using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Utils;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 文明的存续 (CivilightEterna)
/// 1费技能牌
/// 虚无。重现1（+1）张牌，使其在整局游戏升级。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class CivilightEterna() : TheresaCardModel(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    // 基础重现数量
    private const int ReplayCountBase = 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => 
    [
        CardKeyword.Ethereal,  // 虚无
        ReplayKeyword.Replay,  // 重现
        DimKeyword.Dim
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Ethereal),
        HoverTipFactory.FromKeyword(ReplayKeyword.Replay)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ReplayCount", ReplayCountBase)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || CombatState == null) return;

        int replayCount = (int)DynamicVars["ReplayCount"].BaseValue;

        MainFile.Logger?.Info($"[CivilightEterna] Playing with replay count: {replayCount}");

        // 执行重现效果，并使重现的卡牌在整局游戏中升级
        await ReplayHelper.ExecuteReplay(
            choiceContext,
            this,
            CombatState,
            replayCount,
            upgradeForRun: true
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ReplayCount"].UpgradeValueBy(1);
    }
}
