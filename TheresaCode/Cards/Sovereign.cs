using BaseLib.Abstracts;
using BaseLib.Utils;         
using MegaCrit.Sts2.Core.Entities.Cards; 
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using Theresa.TheresaCode.Character; 
using Theresa.TheresaCode.Commands;
using Theresa.TheresaCode.Keywords;


namespace Theresa.TheresaCode.Cards;

// 1. 定义卡牌池
[Pool(typeof(TheresaCardPool))]
public sealed class Sovereign() : TheresaCardModel(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    
    // 2. 定义卡牌的关键字
    // 注意：Sovereign 原本没有 Retain 或 Exhaust，所以这里不添加。
    // 如果需要，可以替换成正确的关键字。
    public override HashSet<CardKeyword> CanonicalKeywords => [DivinityStanceKeyword.DivinityStance,CardKeyword.Exhaust];

    /// <summary>
    /// Sovereign 使用自定义 Spine 动画场景作为卡面
    /// </summary>
    public override string? CustomSpinePortraitScenePath => "res://Theresa/images/cards/test.tscn";

    // 5. 定义卡牌升级逻辑
    // 与 Crescendo 一样，消耗减少1点。
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    // 6. 定义卡牌播放时的核心行为
    // 参考 Crescendo 的 OnPlay 方法，使用 Owner.Creature 作为目标。
    // 并且确保是异步的 (async Task)。
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 使用 Owner.Creature 作为目标，这与 Crescendo 的做法一致，更加健壮。
        await StanceCmd.EnterDivinity(Owner.Creature, cardPlay.Card);
    }
}