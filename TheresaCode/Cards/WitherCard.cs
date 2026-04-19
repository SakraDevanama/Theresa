using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 凋零 (Wither)
/// 1费攻击牌
/// 造成5（+5）点伤害。
/// 给予5（+3）层凋亡。
/// 如果微尘大于3层则再给予5（+9）层凋亡。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class WitherCard() : TheresaCardModel(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 基础伤害
    private const int BaseDamage = 5;
    // 升级后伤害增加
    private const int UpgradeDamageBonus = 2;
    
    // 基础凋亡层数
    private const int BaseApoptosis = 2;
    // 升级后凋亡增加
    private const int UpgradeApoptosisBonus = 1;
    
    // 额外凋亡层数（微尘>5时）
    private const int BonusApoptosis = 2;
    // 升级后额外凋亡增加
    private const int UpgradeBonusApoptosis = 1;
    
    // 微尘阈值
    private const int MantraThreshold = 3;

    // 定义自定义变量
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move),
        new DynamicVar("ApoptosisAmount", BaseApoptosis),
        new DynamicVar("BonusApoptosisAmount", BonusApoptosis)
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<ApoptosisPower>(),

    ];
    
    
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        // 获取目标敌人
        var target = cardPlay.Target as Creature;
        if (target == null || !target.IsAlive) return;

        // 1. 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 给予基础凋亡层数
        int apoptosisAmount = (int)DynamicVars["ApoptosisAmount"].BaseValue;
        await PowerCmd.Apply<ApoptosisPower>(new ThrowingPlayerChoiceContext(), target, apoptosisAmount, Owner.Creature, this);

        // 3. 检查微尘是否大于5层，如果是则再给予额外凋亡
        int mantraAmount = Owner.Creature.GetPowerAmount<MantraPower>();
        if (mantraAmount > MantraThreshold)
        {
            int bonusApoptosis = (int)DynamicVars["BonusApoptosisAmount"].BaseValue;
            await PowerCmd.Apply<ApoptosisPower>(new ThrowingPlayerChoiceContext(), target, bonusApoptosis, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后伤害+1
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
        // 升级后基础凋亡+3
        DynamicVars["ApoptosisAmount"].UpgradeValueBy(UpgradeApoptosisBonus);
        // 升级后额外凋亡+9
        DynamicVars["BonusApoptosisAmount"].UpgradeValueBy(UpgradeBonusApoptosis);
    }
}
