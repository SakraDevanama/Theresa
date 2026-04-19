using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

[Pool(typeof(TheresaCardPool))]
public sealed class Hardship() : TheresaCardModel(2, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(14m, ValueProp.Move), // 这是条件不满足时的伤害
        new DamageVar("HighDamage", 25m, ValueProp.Move) // 这是条件满足时的伤害
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<ApoptosisPower>(),
    ];
    
    

    // --- 修正 IsPlayable/ShouldGlowGoldInternal 以支持 AllEnemies ---
    protected override bool ShouldGlowGoldInternal => 
        CombatState?.Enemies.Any() == true; // 只要存在敌人，就发光（表示可用）
    // --- ShouldGlowGoldInternal 结束 ---
    

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState != null)
        {
            // 先复制敌人列表，避免遍历时集合被修改
            var enemies = CombatState.Enemies.ToList();
            
            foreach (var enemy in enemies)
            {
                // 检查敌人是否仍然存活
                if (!enemy.IsAlive) continue;
                
                var apoptosisAmount = enemy.GetPowerAmount<ApoptosisPower>();
                
                decimal damageToDeal;
                string fxName;
                
                // --- 修改触发条件 ---
                if (apoptosisAmount > enemy.CurrentHp)
                // --- 触发条件结束 ---
                {
                    // 条件满足：造成高额伤害
                    damageToDeal = DynamicVars["HighDamage"].BaseValue;
                    fxName = "vfx/vfx_heavy_blunt";
                }
                else
                {
                    // 条件不满足：造成基础伤害
                    damageToDeal = DynamicVars.Damage.BaseValue;
                    fxName = "vfx/vfx_attack_slash";
                }

                await DamageCmd.Attack(damageToDeal)
                    .FromCard(this)
                    .Targeting(enemy) // 对单个敌人造成计算出的伤害
                    .WithHitFx(fxName, null, "blunt_attack.mp3")
                    .Execute(choiceContext);
            }
        }
    }

    // 添加 OnUpgrade 方法来处理升级逻辑
    protected override void OnUpgrade()
    {
        // 基础伤害增加 3
        DynamicVars.Damage.UpgradeValueBy(3m);
        // 高额伤害增加 5
        DynamicVars["HighDamage"].UpgradeValueBy(5m);
    }
}