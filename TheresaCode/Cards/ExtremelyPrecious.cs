using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Patches;

namespace Theresa.TheresaCode.Cards;


[Pool(typeof(TheresaCardPool))]
public sealed class ExtremelyPrecious() : TheresaCardModel(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<BufferPower>(),
    ];
    
    // 缓冲层数
    private int BufferAmount => (int)DynamicVars["BufferAmount"].BaseValue;
    // 减耗数值
    private int CostReduction => (int)DynamicVars["CostReduction"].BaseValue;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BufferAmount", 1),
        new DynamicVar("CostReduction", 1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 1. 获得缓冲
        await PowerCmd.Apply<BufferPower>(
            Owner.Creature,
            BufferAmount,
            Owner.Creature,
            this
        );

        // 2. 从手中打出时，使相邻手牌本回合费用-1
        await ReduceAdjacentCardsCost();
    }

    private async Task ReduceAdjacentCardsCost()
    {
        if (Owner == null) return;

        // 获取手牌堆
        var handPile = PileType.Hand.GetPile(Owner);
        if (handPile?.Cards == null) return;

        var currentHandCards = handPile.Cards.ToList();
        
        // 从追踪器获取这张牌打出前在手牌中的位置
        if (!ExtremelyPreciousPositionTracker.LastPlayedPosition.TryGetValue(Owner, out int originalIndex))
        {
            return;
        }

        // 清除记录
        ExtremelyPreciousPositionTracker.LastPlayedPosition.Remove(Owner);

        // 计算相邻牌的索引（打出后，原位置左边的牌索引不变，右边的牌索引-1）
        // 左边相邻: originalIndex - 1（如果存在）
        // 右边相邻: originalIndex（因为右边的牌现在占据了这个位置）
        
        var cardsToReduce = new List<MegaCrit.Sts2.Core.Models.CardModel>();

        // 左边相邻牌
        int leftIndex = originalIndex - 1;
        if (leftIndex >= 0 && leftIndex < currentHandCards.Count)
        {
            cardsToReduce.Add(currentHandCards[leftIndex]);
        }

        // 右边相邻牌
        int rightIndex = originalIndex;
        if (rightIndex >= 0 && rightIndex < currentHandCards.Count)
        {
            cardsToReduce.Add(currentHandCards[rightIndex]);
        }

        // 应用减耗效果
        foreach (var card in cardsToReduce)
        {
            if (card != null)
            {
                card.EnergyCost.AddThisTurnOrUntilPlayed(-CostReduction);
                GD.Print($"[ExtremelyPrecious] 手牌 {card.Id} 本回合费用-{CostReduction}");
            }
        }

        await Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        // 升级后缓冲+1，减耗数值+1
        DynamicVars["BufferAmount"].UpgradeValueBy(1m);
        DynamicVars["CostReduction"].UpgradeValueBy(1m);
    }
}
