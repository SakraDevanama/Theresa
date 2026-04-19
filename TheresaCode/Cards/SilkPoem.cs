using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 丝之诗
/// 1费技能牌
/// 抽1张牌
/// 给予所有敌人1（+2）层茧缚
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class SilkPoem() : TheresaCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SilkCocoon>(),
        HoverTipFactory.FromPower<Broken>(),
    ];
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new PowerVar<SilkCocoon>(1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 抽牌
        if (Owner != null)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        }

        // 给予所有敌人茧缚
        if (CombatState != null)
        {
            var silkCocoonAmount = DynamicVars["SilkCocoon"].BaseValue;
        
            //使用 ToList() 创建副本，避免在遍历时修改原集合报错
            var enemiesCopy = CombatState.Enemies.ToList();
        
            foreach (var enemy in enemiesCopy)
            {
                if (enemy.IsAlive)
                {
                    await PowerCmd.Apply<SilkCocoon>(new ThrowingPlayerChoiceContext(), enemy, silkCocoonAmount, Owner?.Creature, this);
                }
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars["SilkCocoon"].UpgradeValueBy(2m);
    }
}
