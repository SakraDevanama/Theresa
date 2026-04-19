using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 阡陌微光 (ManyLight)
/// 1费技能牌 / 罕见 / 消耗
/// 将抽牌堆超出上限的牌转化为微尘。将2（+1）张牌放入手中。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class ManyLight() : TheresaCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 手牌上限常量
    private const int HandLimit = 10;
    // 抽取的基础数量
    private const int BaseDrawCount = 2;
    // 升级后增加
    private const int UpgradeDrawBonus = 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, LingerKeyword.Linger];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MantraPower>(),
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DrawCount", BaseDrawCount),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // ✅ 获取手牌堆（用于检测实际进入手牌的牌数）
        var handPile = PileType.Hand.GetPile(Owner);
        
        // 记录抽取前的手牌数量
        int handCountBefore = handPile?.Cards.Count() ?? 0;

        // 1. 执行抽取2(+1)张牌
        int drawCount = (int)DynamicVars["DrawCount"].BaseValue;
        int actuallyDrawn = 0; // 实际成功抽取的牌数
        
        for (int i = 0; i < drawCount; i++)
        {
            var drawPile = PileType.Draw.GetPile(Owner);
            if (drawPile?.Cards.Any() != true) break;
            
            await CardPileCmd.Draw(choiceContext, 1, Owner);
            actuallyDrawn++;
        }

        // ✅ 2. 检测实际进入手牌的牌数
        int handCountAfter = handPile?.Cards.Count() ?? 0;
        int handIncrease = handCountAfter - handCountBefore;
        
        // 计算因为上限而没有进入手牌的牌数
        // 如果手牌增加了，说明牌进入了手牌
        // 如果手牌没增加（满了），说明牌被弃掉/销毁了
        int excessCount = actuallyDrawn - handIncrease;

        // ✅ 3. 每有1张牌因为上限被弃掉，获得1个微尘
        if (excessCount > 0)
        {
            await PowerCmd.Apply<MantraPower>(
                Owner.Creature,
                excessCount,
                Owner.Creature,
                this
            );
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DrawCount"].UpgradeValueBy(UpgradeDrawBonus);
    }
}
