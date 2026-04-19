using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 卫国前夜 (NightBeforeWar)
/// 1费技能牌
/// 获得1层恨意 2（+1）次。
/// 消耗时：向手牌加入1张萨卡兹叙说。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class NightBeforeWar() : TheresaCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const int BaseHateAmount = 2;
    private const int UpgradeDelta = 1;

    // 标记此实例是否等待消耗触发
    internal bool IsAwaitingExhaust { get; private set; }

    // 添加消耗关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<ZaakathHatePower>(),
        HoverTipFactory.FromCard<Astory>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HateAmount", BaseHateAmount)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        int hateAmount = (int)DynamicVars["HateAmount"].BaseValue;

        // 获得1层恨意，重复 HateAmount 次
        for (int i = 0; i < hateAmount; i++)
        {
            await PowerCmd.Apply<ZaakathHatePower>(Owner.Creature, 1, Owner.Creature, this);
        }

        // 标记等待消耗触发
        IsAwaitingExhaust = true;
    }

    /// <summary>
    /// 当此卡被消耗时触发的效果
    /// </summary>
    internal void OnExhausted()
    {
        IsAwaitingExhaust = false;

        // 向手牌加入1张萨卡兹叙说
        if (CombatState != null && Owner != null)
        {
            var aStoryCard = CombatState.CreateCard<Astory>(Owner);
            _ = CardPileCmd.AddGeneratedCardToCombat(aStoryCard, PileType.Hand, true);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HateAmount"].UpgradeValueBy(UpgradeDelta);
    }
}
