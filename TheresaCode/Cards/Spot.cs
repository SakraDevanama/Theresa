using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

[Pool(typeof(TheresaCardPool))]
public class Spot() : TheresaCardModel(0, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        DimKeyword.Dim,
        DustKeyword.Dust,
        LingerKeyword.Linger,
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(DimKeyword.Dim),
        HoverTipFactory.FromKeyword(DustKeyword.Dust),
        HoverTipFactory.FromKeyword(LingerKeyword.Linger),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 1. 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 从固有微尘中选择一张加入手牌
        await ChooseDustToHand(choiceContext, cardPlay.IsAutoPlay);
    }

    private async Task ChooseDustToHand(PlayerChoiceContext choiceContext, bool isAutoPlay)
    {
        if (Owner == null) return;

        var dustCards = DustManager.CardsFor(Owner).ToList();

        if (dustCards.Count == 0) return;

        CardModel selectedCard;

        if (isAutoPlay || choiceContext is ThrowingPlayerChoiceContext)
        {
            // 自动打出（如萦绕）时无法弹出选择界面，按确定性规则选择：
            // 优先选当前不在萦绕处理中的微尘牌，避免递归移除正在执行的卡。
            selectedCard = dustCards.FirstOrDefault(c => !DustManager.IsCurrentlyLingering(c))
                           ?? dustCards[0];
            MainFile.Logger?.Info($"[Spot] Auto-play selected dust card: {selectedCard.Id.Entry}");
        }
        else if (dustCards.Count == 1)
        {
            selectedCard = dustCards[0];
        }
        else
        {
            var prefs = new CardSelectorPrefs(
                new LocString("static_hover_tips", "choose_dust_to_hand"),
                1,
                1
            )
            {
                Cancelable = false
            };

            var selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                dustCards,
                Owner,
                prefs
            )).ToList();

            if (!selected.Any()) return;
            selectedCard = selected.First();
        }

        // 从 DustManager 移除（减少 Mantra）并加入手牌
        await DustManager.RemoveCard(selectedCard);
        await CardPileCmd.Add(selectedCard, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
