using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 情感谐振
/// 2费攻击牌
/// 对所有敌人造成9点伤害（+3）
/// 每命中一个敌人就给予自身1层TheresiasHopePower
/// </summary>

[Pool(typeof(TheresaCardPool))]
public sealed class EmotionalResonance()
    : TheresaCardModel(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<TheresiasHopePower>()
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int hitCount = 0;
        
        // 对所有敌人造成伤害
        if (CombatState != null)
        {
            // 使用 ToList() 避免集合被修改时出错
            var enemies = CombatState.Enemies.Where(e => e.IsHittable).ToList();
            foreach (Creature enemy in enemies)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this)
                    .Targeting(enemy)
                    .Execute(choiceContext);
                hitCount++;
            }
        }
        
        // 每命中一个敌人，给予自身1层TheresiasHopePower
        if (hitCount > 0)
        {
            await PowerCmd.Apply<TheresiasHopePower>(Owner.Creature, hitCount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}