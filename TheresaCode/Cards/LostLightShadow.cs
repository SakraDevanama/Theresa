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
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 失光之影
/// 1费攻击牌
/// 消耗所有MantraPower，每层造成6（+1）点伤害
/// 并给予3（+1）层+消耗的MantraPower数量的ApoptosisPower
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class LostLightShadow() : TheresaCardModel(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [LingerKeyword.Linger];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromPower<DivinityStance>(),
    ];
    
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move), // 基础伤害6，升级后+1变为7
        new PowerVar<ApoptosisPower>(3m)   // 基础凋亡3层，升级后+1变为4层
    ];

    protected override bool IsPlayable =>
        Owner.Creature.Powers.OfType<MantraPower>().Any(p => p.Amount > 0);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // 1. 获取 MantraPower 并计算层数
        var mantraPower = Owner.Creature.Powers.OfType<MantraPower>().FirstOrDefault();
        int consumedAmount = mantraPower?.Amount ?? 0;

        if (consumedAmount <= 0) return;

        // 2. 消耗所有 MantraPower（逐层消耗以触发其他效果）
        await PowerCmd.ModifyAmount(mantraPower!, -consumedAmount, Owner.Creature, this);

        // 3. 造成每层伤害
        decimal damagePerStack = DynamicVars.Damage.BaseValue;
        decimal totalDamage = damagePerStack * consumedAmount;

        await DamageCmd.Attack(totalDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 4. 给予目标凋亡：基础层数 + 消耗的MantraPower数量
        decimal baseApoptosis = DynamicVars["ApoptosisPower"].BaseValue;
        decimal totalApoptosis = baseApoptosis + consumedAmount;

        if (totalApoptosis > 0 && cardPlay.Target is Creature targetCreature)
        {
            await PowerCmd.Apply<ApoptosisPower>(
                targetCreature,
                totalApoptosis,
                Owner.Creature,
                this
            );
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后伤害 6 -> 7，凋亡基础层数 3 -> 4
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars["ApoptosisPower"].UpgradeValueBy(1m);
    }
}
