using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;  // VulnerablePower
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 凋零玫瑰 (DyingRose)
/// 2费能力牌 / 罕见
/// 给予所有敌人6（+2）层凋亡和1层易伤。
/// 若其凋亡效果达到上限，再给予1层凋亡爆发。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class DyingRose() : TheresaCardModel(2, CardType.Power, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ApoptosisPower>(),
        HoverTipFactory.FromPower<ApoptosisBurstPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
    ];

    // 基础凋亡层数
    private const int BaseApoptosis = 6;
    // 升级后凋亡增加
    private const int UpgradeApoptosisBonus = 2;
    // 易伤层数（固定1层）
    private const int VulnerableAmount = 1;
    // 凋亡爆发层数（固定1层）
    private const int BurstAmount = 1;

    // 定义自定义变量
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Apoptosis", BaseApoptosis),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // ✅ 修复：通过 Owner.Creature 获取战斗状态
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        // ✅ 修复：使用 combatState 获取敌人
        var enemies = combatState.GetOpponentsOf(Owner.Creature)
            .Where(c => c.IsAlive)
            .ToList();

        if (enemies.Count == 0) return;

        int apoptosisAmount = (int)DynamicVars["Apoptosis"].BaseValue;

        foreach (var enemy in enemies)
        {
            // 1. 给予凋亡
            await PowerCmd.Apply<ApoptosisPower>(
                enemy,
                apoptosisAmount,
                Owner.Creature,
                this
            );

            // 2. ✅ 给予易伤（使用原版 VulnerablePower = TAUNTPOWER）
            await PowerCmd.Apply<VulnerablePower>(
                enemy,
                VulnerableAmount,
                Owner.Creature,
                this
            );

            // 3. 检查凋亡是否达到上限，给予凋亡爆发
            var apoptosisPower = enemy.GetPower<ApoptosisPower>();
            if (apoptosisPower != null && apoptosisPower.Amount >= enemy.CurrentHp)
            {
                await PowerCmd.Apply<ApoptosisBurstPower>(
                    enemy,
                    BurstAmount,
                    Owner.Creature,
                    this
                );
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Apoptosis"].UpgradeValueBy(UpgradeApoptosisBonus);
    }
}
