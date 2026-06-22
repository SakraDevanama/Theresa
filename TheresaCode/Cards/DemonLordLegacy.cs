using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 魔王传承 (KingInheritance) - Java原版
/// 0费技能牌，罕见稀有度
/// 
/// 效果：黯淡。获得1个微尘上限。向手中放入1张保留的受伤。
/// 升级：虚无。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class DemonLordLegacy() : TheresaCardModel(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 1. 增加微尘上限
        DustManager.IncreaseMaxDust(1, Owner);

        // 2. 向手中放入1张保留的受伤
        if (CombatState != null)
        {
            var woundCard = CombatState.CreateCard<Wound>(Owner);
            // 添加保留关键词
            woundCard.AddKeyword(CardKeyword.Retain);
            await CardPileCmd.AddGeneratedCardToCombat(woundCard, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级：获得虚无
        AddKeyword(CardKeyword.Ethereal);
    }
}
