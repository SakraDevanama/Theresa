using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Enchantments;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 破碎终曲 (ShatteredFinale)
/// 0费攻击牌 罕见
/// 仅当拥有丝线的牌不少于24张才能打出。
/// 对所有敌人造成80点伤害（升级后+20）。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class ShatteredFinale : TheresaCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];

    public ShatteredFinale() : base(
        baseCost: 0, 
        type: CardType.Attack, 
        rarity: CardRarity.Rare,  
        target: TargetType.AllEnemies
    )
    {
        GD.Print(">>> ShatteredFinale 构造函数被调用 <<<");
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(80m, ValueProp.Move),
        new DynamicVar("ThreadCount", 24m)
    ];

    // 检查是否满足打出条件：拥有丝线的牌不少于24张
    protected override bool IsPlayable
    {
        get
        {
            if (Owner == null) return false;
            
            // 获取丝线附魔的ID
            var threadEnchantmentId = ModelDb.GetId<SilkThreadEnchantment>();
            
            // 统计所有牌堆中有丝线附魔的牌数量
            int threadCardCount = 0;
            
            // 检查抽牌堆
            var drawPile = PileType.Draw.GetPile(Owner);
            if (drawPile != null)
            {
                foreach (var card in drawPile.Cards)
                {
                    if (card.Enchantment?.Id == threadEnchantmentId)
                        threadCardCount++;
                }
            }
            
            // 检查手牌
            var handPile = PileType.Hand.GetPile(Owner);
            if (handPile != null)
            {
                foreach (var card in handPile.Cards)
                {
                    if (card.Enchantment?.Id == threadEnchantmentId)
                        threadCardCount++;
                }
            }
            
            // 检查弃牌堆
            var discardPile = PileType.Discard.GetPile(Owner);
            if (discardPile != null)
            {
                foreach (var card in discardPile.Cards)
                {
                    if (card.Enchantment?.Id == threadEnchantmentId)
                        threadCardCount++;
                }
            }
            
            // 检查消耗堆
            var exhaustPile = PileType.Exhaust.GetPile(Owner);
            if (exhaustPile != null)
            {
                foreach (var card in exhaustPile.Cards)
                {
                    if (card.Enchantment?.Id == threadEnchantmentId)
                        threadCardCount++;
                }
            }
            
            return threadCardCount >= 24 && base.IsPlayable;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        GD.Print(">>> ShatteredFinale OnPlay 被调用 <<<");
        var owner = Owner.Creature;
        decimal damage = DynamicVars.Damage.BaseValue;
        
        var allEnemies = owner.CombatState?.GetOpponentsOf(owner).ToList() ?? new List<Creature>();
        
        foreach (var enemy in allEnemies.Where(e => !e.IsDead))
        {
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(enemy)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(20m);
    }
}