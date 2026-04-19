using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 故事的起点 (StoryTeller)
/// 0费技能牌
/// 虚无。消耗。抽1张牌。手牌中每有1张能力牌，额外抽1（+1）张牌。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class StoryTeller() : TheresaCardModel(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 基础抽牌数
    private const int BaseDraw = 1;
    // 每张能力牌额外抽牌数
    private const int DrawPerPower = 1;
    // 升级后额外抽牌增量
    private const int UpgradeDrawDelta = 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => 
    [
        CardKeyword.Ethereal,  // 虚无
        CardKeyword.Exhaust,   // 消耗
        DimKeyword.Dim         // 黯淡
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar("BaseDraw", BaseDraw),
        new DynamicVar("DrawPerPower", DrawPerPower)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 计算总抽牌数（基于当前手牌中的能力牌数量，排除自身）
        int powerCardCount = PileType.Hand.GetPile(Owner)?.Cards.Count(c => c.Type == CardType.Power && c != this) ?? 0;
        int drawPerPower = (int)DynamicVars["DrawPerPower"].BaseValue;
        int totalDraw = BaseDraw + (powerCardCount * drawPerPower);
        
        if (totalDraw > 0)
        {
            await CardPileCmd.Draw(choiceContext, totalDraw, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后每张能力牌额外抽牌+1
        DynamicVars["DrawPerPower"].UpgradeValueBy(UpgradeDrawDelta);
    }
}
