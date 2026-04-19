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
public sealed class SarkazSee() : TheresaCardModel(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{ 
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, ReplayKeyword.Replay]; 
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9m, ValueProp.Move)]; 
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) 
    { 
        // 1. 对目标敌人造成 9 点伤害
        ArgumentNullException.ThrowIfNull(cardPlay.Target); 
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue) 
            .FromCard(this).Targeting(cardPlay.Target) 
            .WithHitFx("vfx/vfx_attack_slash", null, "blunt_attack.mp3") 
            .Execute(choiceContext); 

        // 2. 执行重现效果（从本局移除的卡牌中选牌，复制到手牌，添加消耗，耗能-1）
        if ((CombatState)CombatState != null)
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
        DynamicVars.Damage.UpgradeValueBy(3m);
    } 
}
