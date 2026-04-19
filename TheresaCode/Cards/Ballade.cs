using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Enchantments;
using Theresa.TheresaCode.Keywords;
using MegaCrit.Sts2.Core.Combat;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 叙事曲
/// 1费技能牌（升级后0费）
/// 将弃牌堆的10张卡洗到抽牌堆顶，使抽牌堆中的卡牌获得丝线
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class Ballade() : TheresaCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [SilkKeyword.Silk];
    
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 1. 获取弃牌堆的卡牌，最多10张
        var discardPile = PileType.Discard.GetPile(Owner);
        var discardCards = discardPile.Cards.Take(10).ToList();

        if (discardCards.Count > 0)
        {
            // 2. 将这些卡牌移动到抽牌堆顶
            await CardPileCmd.Add(discardCards, PileType.Draw, CardPilePosition.Top, this);
            
            // 3. 给这些卡牌添加丝线附魔
            // 从 ModelDb 获取附魔原型并克隆
            var enchantmentPrototype = ModelDb.GetById<EnchantmentModel>(ModelDb.GetId<SilkThreadEnchantment>());
            foreach (var card in discardCards)
            {
                try
                {
                    var enchantment = (EnchantmentModel)enchantmentPrototype.MutableClone();
                    CardCmd.Enchant(enchantment, card, 1);
                }
                catch (InvalidOperationException)
                {
                    // 某些卡牌（如 ENTHRALLED）不能被附魔，跳过
                }
            }
            
            // 4. 洗牌
            if (Owner.PlayerCombatState != null && CombatState != null)
            {
                Owner.PlayerCombatState.DrawPile.RandomizeOrderInternal(
                    Owner,
                    Owner.RunState.Rng.Shuffle,
                    (CombatState)CombatState
                );
            }
        }
        
        // 丝线附魔的抽牌效果和传播逻辑由附魔自身处理，不需要额外的 Power
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
