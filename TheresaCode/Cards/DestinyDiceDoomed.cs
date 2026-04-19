using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 终结一切的选择 (DestinyDiceDoomed)
/// 3费技能牌 / 罕见 / 升级后2费
/// 对敌方造成14（+4）点伤害，并且给予1层茧缚。
/// 检测自身当前生命值，若为96则伤害增加12倍，若不为96则进入下面的判定计算伤害。
/// 自身当前生命值为双数时伤害增加3倍，自身当前生命值为单数时伤害减半。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class DestinyDiceDoomed() : TheresaCardModel(3, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 基础伤害
    private const int BaseDamage = 14;
    // 升级后伤害增加
    private const int UpgradeDamageBonus = 4;
    // 茧缚层数（固定）
    private const int CocoonAmount = 1;
    // 特殊阈值
    private const int SpecialHpValue = 96;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<SilkCocoon>(),
    ];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Damage", BaseDamage),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        var target = cardPlay.Target as Creature;
        if (target == null || !target.IsAlive) return;

        // ✅ 获取自身当前生命值（不是怪物）
        int playerHp = Owner.Creature.CurrentHp;
        
        // 计算最终伤害（基于自身HP）
        int baseDamage = (int)DynamicVars["Damage"].BaseValue;
        int finalDamage = CalculateDamage(baseDamage, playerHp);

        // 1. 造成伤害
        await DamageCmd.Attack(finalDamage)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_heavy_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        // 2. 给予1层茧缚
        await PowerCmd.Apply<SilkCocoon>(new ThrowingPlayerChoiceContext(), 
            target,
            CocoonAmount,
            Owner.Creature,
            this
        );
    }

    /// <summary>
    /// ✅ 计算最终伤害（基于自身生命值，不是目标）
    /// </summary>
    private int CalculateDamage(int baseDamage, int playerHp)
    {
        // 优先判定：自身HP = 96时 ×12
        if (playerHp == SpecialHpValue)
        {
            return baseDamage * 12;
        }
        
        // 自身HP为双数时 ×3
        if (playerHp % 2 == 0)
        {
            return baseDamage * 3;
        }
        
        // 自身HP为单数时 ÷2
        return baseDamage / 2;
    }

    protected override void OnUpgrade()
    {
        // 升级后费用-1（3→2），伤害+4（14→18）
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Damage"].UpgradeValueBy(UpgradeDamageBonus);
    }
}
