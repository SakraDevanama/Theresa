using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 前尘影事
/// 2费技能牌
/// 抽3（+1）张牌
/// 每有1张牌未能抽到手中，造成12点伤害，否则获得3点格挡
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class BeforeDust() : TheresaCardModel(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 动态变量：抽牌数量、伤害值、格挡值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DrawCount", 3m),
        new DamageVar("MissDamage", 12m, ValueProp.Move),
        new BlockVar("HitBlock", 3m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        int drawCount = (int)DynamicVars["DrawCount"].BaseValue;
        
        // 获取抽牌前的手牌数量
        int handCountBefore = PileType.Hand.GetPile(Owner).Cards.Count();
        int drawPileCount = PileType.Draw.GetPile(Owner).Cards.Count();
        int discardPileCount = PileType.Discard.GetPile(Owner).Cards.Count();
        
        // 计算实际能抽的牌数（抽牌堆 + 弃牌堆）
        int totalAvailable = drawPileCount + discardPileCount;
        int actualDraw = Math.Min(drawCount, totalAvailable);
        
        // 执行抽牌
        if (actualDraw > 0)
        {
            await CardPileCmd.Draw(choiceContext, actualDraw, Owner);
        }
        
        // 计算实际抽到的牌数（通过比较抽牌前后的手牌数量）
        int handCountAfter = PileType.Hand.GetPile(Owner).Cards.Count();
        int actuallyDrawn = handCountAfter - handCountBefore;
        int missedDraw = drawCount - actuallyDrawn;
        
        // 如果有未能抽到的牌，造成伤害
        if (missedDraw > 0)
        {
            decimal damagePerMiss = DynamicVars["MissDamage"].BaseValue;
            decimal totalDamage = damagePerMiss * missedDraw;
            
            // 对随机敌人造成伤害
            var combatState = Owner.Creature.CombatState;
            if (combatState != null)
            {
                var target = GetRandomEnemy(combatState);
                if (target != null)
                {
                    await DamageCmd.Attack(totalDamage)
                        .FromCard(this)
                        .Targeting(target)
                        .Execute(choiceContext);
                }
            }
        }
        // 如果全部抽到，获得格挡
        else if (actuallyDrawn >= drawCount)
        {
            decimal blockAmount = DynamicVars["HitBlock"].BaseValue;
            await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(blockAmount, ValueProp.Move), cardPlay);
        }
    }

    /// <summary>
    /// 获取随机敌人作为目标
    /// </summary>
    private Creature? GetRandomEnemy(CombatState combatState)
    {
        if (Owner?.Creature == null) return null;

        var enemies = combatState.GetOpponentsOf(Owner.Creature)
            .Where(c => c.IsAlive && c.IsHittable)
            .ToList();

        if (enemies.Count == 0) return null;

        // 随机选择一个敌人
        return enemies[Random.Shared.Next(enemies.Count)];
    }

    protected override void OnUpgrade()
    {
        // 升级后抽牌数 +1（3 -> 4）
        DynamicVars["DrawCount"].UpgradeValueBy(1m);
    }
}
