using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 索拉里斯之海效果
/// 不可叠加。
/// 主动打出牌后，选择抽牌堆中1张费用更低的牌放入手中并免费打出。
/// </summary>
public sealed class SolarisSeaPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    protected override bool IsVisibleInternal => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 只监听持有者自己打出的牌
        if (cardPlay.Card.Owner?.Creature != Owner) return;

        // 排除自动打出的牌（参考Java版本的 !card.isInAutoplay）
        if (cardPlay.IsAutoPlay) return;

        // 获取打出牌的费用（参考Java版本的 card.costForTurn）
        int playedCardCost = cardPlay.Card.EnergyCost?.GetResolved() ?? 0;

        // 只处理费用大于0的牌（参考Java版本的 theCost > 0）
        if (playedCardCost <= 0) return;

        // 获取抽牌堆
        var drawPile = PileType.Draw.GetPile(cardPlay.Card.Owner);
        if (drawPile == null || !drawPile.Cards.Any()) return;

        // 筛选费用更低的牌（参考Java版本的 c.costForTurn >= 0 && c.costForTurn < amount）
        var cheaperCards = drawPile.Cards
            .Where(c => {
                int cardCost = c.EnergyCost?.GetResolved() ?? 0;
                return cardCost >= 0 && cardCost < playedCardCost;
            })
            .ToList();

        if (!cheaperCards.Any()) return;

        // 播放Power闪烁效果（参考Java版本的 power.flashWithoutSound()）
        Flash();

        // 如果只有一张符合条件的牌，直接打出
        if (cheaperCards.Count == 1)
        {
            await PlayCardFromDrawPile(context, cheaperCards.First());
            return;
        }

        // 多张牌时让玩家选择
        var selectionPrompt = new LocString("static_hover_tips", "solaris_sea_select_card");
        var prefs = new CardSelectorPrefs(
            selectionPrompt,
            1,
            1
        )
        {
            Cancelable = false,
        };

        var selectedCards = (await CardSelectCmd.FromSimpleGrid(
            context,
            cheaperCards,
            cardPlay.Card.Owner,
            prefs
        )).ToList();

        if (!selectedCards.Any()) return;

        await PlayCardFromDrawPile(context, selectedCards.First());
    }

    /// <summary>
    /// 从抽牌堆打出卡牌（参考Java版本的 playCard 方法）
    /// </summary>
    private async Task PlayCardFromDrawPile(PlayerChoiceContext context, CardModel card)
    {
        if (Owner == null) return;

        // 设置卡牌为免费打出
        card.SetToFreeThisTurn();

        // 根据卡牌目标类型选择合适的目标
        var target = GetTargetForCard(card);

        // 使用 CardCmd.AutoPlay 直接从抽牌堆打出卡牌
        // 这个方法不需要卡牌在手牌中
        await CardCmd.AutoPlay(context, card, target, AutoPlayType.Default, skipXCapture: false, skipCardPileVisuals: false);
    }

    /// <summary>
    /// 玩家回合结束后移除自身
    /// </summary>
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 确保是拥有此Power的生物所在的阵营回合结束
        if (Owner?.Side != side) return;

        // 回合结束，移除此效果
        await PowerCmd.Remove(this);
    }

    /// <summary>
    /// 根据卡牌目标类型选择合适的目标
    /// </summary>
    private Creature? GetTargetForCard(CardModel card)
    {
        if (Owner?.CombatState == null) return null;

        return card.TargetType switch
        {
            TargetType.AnyEnemy => Owner.CombatState.HittableEnemies.FirstOrDefault(),
            TargetType.AllEnemies => Owner.CombatState.HittableEnemies.FirstOrDefault(),
            TargetType.RandomEnemy => Owner.CombatState.HittableEnemies.FirstOrDefault(),
            TargetType.AnyAlly => card.Owner?.Creature,
            TargetType.AllAllies => card.Owner?.Creature,
            TargetType.Self => card.Owner?.Creature,
            TargetType.AnyPlayer => card.Owner?.Creature,
            _ => null
        };
    }
}
