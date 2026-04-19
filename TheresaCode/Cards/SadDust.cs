using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 哀尘所埃 (SadDust) - Java原版
/// 1费技能牌，罕见稀有度
/// 
/// 效果：黯淡。向微尘可超出上限地放入 !M! 张消耗的尘埃。
/// 升级：保留。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class SadDust() : TheresaCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        int amount = DynamicVars.Cards.IntValue;

        for (int i = 0; i < amount; i++)
        {
            // 创建尘埃（Mote）的副本，使用 CombatState.CreateCard 确保正确初始化
            var mote = Owner.Creature.CombatState?.CreateCard(ModelDb.Card<Mote>(), Owner)
                ?? Owner.RunState.CreateCard<Mote>(Owner);
            
            // 添加消耗关键词
            mote.AddKeyword(CardKeyword.Exhaust);
            
            // 放入微尘（可超出上限）
            await DustManager.AddCardOverLimit(mote);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
