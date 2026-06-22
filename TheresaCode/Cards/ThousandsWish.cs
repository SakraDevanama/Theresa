using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Enchantments;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 万千愿景 (ThousandsWish) 
/// 1费能力牌，稀有稀有度
/// 
/// 效果：选择抽牌堆1张牌，对其编织：愿景。你每抽13张牌，萦绕 1 次。
/// 升级：固有。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class ThousandsWish() : TheresaCardModel(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 1. 选择抽牌堆1张可以转化为微尘的牌，对其编织愿景
        var drawPile = PileType.Draw.GetPile(Owner);
        if (drawPile != null)
        {
            // 过滤：仅攻击/技能牌，且没有已有附魔
            var eligibleCards = drawPile.Cards
                .Where(c => (c.Type == CardType.Attack || c.Type == CardType.Skill) && c.Enchantment == null)
                .ToList();

            if (eligibleCards.Any())
            {
                var selectionPrompt = new LocString("static_hover_tips", "thousands_wish_select_card");
                var prefs = new CardSelectorPrefs(
                    selectionPrompt,
                    1,
                    1
                )
                {
                    Cancelable = false,
                };

                var selected = await CardSelectCmd.FromSimpleGrid(
                    choiceContext,
                    eligibleCards,
                    Owner,
                    prefs
                );

                var targetCard = selected.FirstOrDefault();
                if (targetCard != null)
                {
                    // 编织愿景附魔
                    var wishEnchantment = ModelDb.Enchantment<WishSilkEnchantment>().ToMutable();
                    CardCmd.Enchant(wishEnchantment, targetCard, 1);
                }
            }
        }

        // 2. 获得/升级 ThousandsWishPower
        var existingPower = Owner.Creature.GetPower<ThousandsWishPower>();
        if (existingPower == null)
        {
            await PowerCmd.Apply<ThousandsWishPower>(new ThrowingPlayerChoiceContext(), 
                Owner.Creature,
                DynamicVars["Amount"].BaseValue,
                Owner.Creature,
                this
            );
        }
        else
        {
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), existingPower, DynamicVars["Amount"].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
