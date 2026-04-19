using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 省略
/// 1费（升级后0费）技能牌
/// 直到回合结束，将抽牌变为获得能量，然后抽1张牌
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class Wordless() : TheresaCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<WordlessEffectPower>()
    ];

    /// <summary>
    /// 卡牌播放时的行为
    /// </summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        // 应用"省略效果"Power：将抽牌变为获得能量
        await PowerCmd.Apply<WordlessEffectPower>(
            Owner.Creature,
            1,
            Owner.Creature,
            this
        );

        // 然后抽1张牌
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    /// <summary>
    /// 卡牌升级逻辑
    /// </summary>
    protected override void OnUpgrade()
    {
        // 升级后费用变为0
        EnergyCost.UpgradeBy(-1);
    }
}
