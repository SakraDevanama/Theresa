using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Stances;


namespace Theresa.TheresaCode.Cards;

[Pool(typeof(TheresaCardPool))]
public class DustBaneCard() : TheresaCardModel(baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Common,
    target: TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        DustKeyword.Dust,
        DivinityStanceKeyword.DivinityStance
    ];
    

    
    
    protected override bool IsPlayable =>
        Owner.Creature.Powers.OfType<MantraPower>().Any(p => (decimal)p.Amount > 0);
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [new BlockVar(8m, ValueProp.Move)];
    

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner.Creature;
        if (owner.CombatState is not { } combatState) return;

        {
            await PowerCmd.Apply<MantraPower>(new ThrowingPlayerChoiceContext(), owner, -1m, owner, this);
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay); 
             
        }

        // 3. 给予自身格挡
        
    }
    protected override void OnUpgrade() // 重写OnUpgrade方法，处理卡牌升级
    {
        DynamicVars.Block.UpgradeValueBy(2m); 
        EnergyCost.UpgradeBy(-1);
    }
}