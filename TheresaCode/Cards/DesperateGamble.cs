using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 绝死绝命 (DesperateGamble)
/// 2费攻击牌（升级后1费）
/// 将弃牌库所有手牌移动到抽牌库。
/// 每移动2张则造成1点伤害，此伤害被格挡则损失20点生命值。
/// 升级后：每移动1张则造成3点伤害，被格挡则损失30点生命值。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class DesperateGamble : TheresaCardModel
{
    // 基础版参数
    private const int BaseCardsPerDamage = 2;
    private const int BaseDamage = 1;
    private const int BaseHpLossOnBlock = 20;
    
    // 升级变化量
    private const int UpgradeCardsPerDamageDelta = -1; // 2 -> 1
    private const int UpgradeDamageDelta = 2;          // 1 -> 3
    private const int UpgradeHpLossDelta = 10;         // 20 -> 30
    
    public DesperateGamble() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }
    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CardsPerDamage", BaseCardsPerDamage),
        new DynamicVar("Damage", BaseDamage),
        new DynamicVar("HpLoss", BaseHpLossOnBlock)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 获取目标敌人
        var target = cardPlay.Target as Creature;
        if (target == null || !target.IsAlive) return;

        // 1. 将弃牌库所有卡牌移动到抽牌库
        var discardPile = PileType.Discard.GetPile(Owner);
        var discardCards = discardPile.Cards.ToList();
        int movedCount = discardCards.Count;

        if (movedCount > 0)
        {
            await CardPileCmd.Add(discardCards, PileType.Draw, CardPilePosition.Top, this);
        }

        // 2. 计算伤害参数
        int cardsPerDamage = (int)DynamicVars["CardsPerDamage"].BaseValue;
        int damagePerHit = (int)DynamicVars["Damage"].BaseValue;
        int hpLossOnBlock = (int)DynamicVars["HpLoss"].BaseValue;

        if (cardsPerDamage <= 0 || damagePerHit <= 0 || movedCount < cardsPerDamage) return;

        int hitCount = movedCount / cardsPerDamage;

        // 3. 逐次造成伤害
        for (int i = 0; i < hitCount && target.IsAlive; i++)
        {
            // 记录伤害前的格挡值
            decimal preBlock = target.Block;
            
            await DamageCmd.Attack(damagePerHit)
                .FromCard(this)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            // 如果目标在伤害前有格挡，则玩家损失生命值
            if (preBlock > 0 && Owner?.Creature?.IsAlive == true)
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    [Owner.Creature],
                    hpLossOnBlock,
                    ValueProp.Unpowered,
                    Owner.Creature,
                    this
                );
            }
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后费用变为1
        EnergyCost.UpgradeBy(-1);
        
        // 升级变量
        DynamicVars["CardsPerDamage"].UpgradeValueBy(UpgradeCardsPerDamageDelta);
        DynamicVars["Damage"].UpgradeValueBy(UpgradeDamageDelta);
        DynamicVars["HpLoss"].UpgradeValueBy(UpgradeHpLossDelta);
    }
}
