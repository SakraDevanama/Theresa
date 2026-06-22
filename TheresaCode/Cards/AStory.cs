using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization; 
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Utils;
using MegaCrit.Sts2.Core.Combat;

namespace Theresa.TheresaCode.Cards;

[Pool(typeof(TheresaCardPool))]
public sealed class Astory() : TheresaCardModel(1, CardType.Skill, CardRarity.Common, TargetType.Self) 
{ 
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, ReplayKeyword.Replay, DimKeyword.Dim]; 
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(7m, ValueProp.Move)]; 
     
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) 
    { 
        // 1. 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 2. 执行重现效果（从本局移除的卡牌中选牌，复制到手牌，添加消耗，耗能-1）
        if (CombatState != null)
        {
            await ReplayHelper.ExecuteReplay(
                choiceContext,
                this,
                (CombatState)CombatState,
                count: 1,
                upgradeForRun: false
            );
        }
    } 
    
    protected override void OnUpgrade() 
    { 
        DynamicVars.Block.UpgradeValueBy(3m);
    } 
}
