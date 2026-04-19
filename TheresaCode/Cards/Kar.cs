using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;



[Pool(typeof(TheresaCardPool))]
public sealed class Kar() : TheresaCardModel(5, CardType.Skill, CardRarity.Rare, TargetType.None) // 修改目标类型为 None
{
    // 添加消耗关键字提示
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust,DimKeyword.Dim];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new HealVar(10m)];

    public override string PortraitPath => string.Empty;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 由于 TargetType.None, cardPlay.Target 会是 null, 所以移除相关检查

        // 1. 获取抽牌库中的所有卡牌
        var drawPile = PileType.Draw.GetPile(Owner);
        var cardsToExhaust = drawPile.Cards.ToList(); // ToList() 避免在迭代时修改集合

        // 2. 消耗抽牌库中的所有卡牌
        foreach (var card in cardsToExhaust)
        {
            // 使用 CardCmd.Exhaust 来消耗卡牌，这与 SecondWind 中的操作一致
            await CardCmd.Exhaust(choiceContext, card);
        }

        // 3. 回复生命值（升级后20点）
        // 注意：现在直接对 Owner.Creature 施放治疗
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);

        // 4. 获得30层 SilkCocoon
        // 注意：现在直接对 Owner.Creature 施加能力
        await PowerCmd.Apply<SilkCocoon>(new ThrowingPlayerChoiceContext(), Owner.Creature, 30m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级后治疗量 10 -> 20
        DynamicVars.Heal.UpgradeValueBy(10m);
    }
}