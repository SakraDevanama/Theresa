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
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 挽悼 (MourningInMourning)
/// 1费攻击牌 / 罕见
/// 给予1层逝尘。造成6（+3）点伤害。
/// 若目标已有逝尘，再造成1次伤害，并消耗掉此牌。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class MourningInMourning() : TheresaCardModel(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DustGone>(),
    ];

    // 基础伤害
    private const int BaseDamage = 6;
    // 升级后伤害增加
    private const int UpgradeDamageBonus = 3;
    // 逝尘层数（固定1层）
    private const int DustGoneAmount = 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];

    // 定义自定义变量
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Damage", BaseDamage),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        var target = cardPlay.Target as Creature;
        if (target == null || !target.IsAlive) return;

        int damageAmount = (int)DynamicVars["Damage"].BaseValue;

        // ✅ 1. 先检查目标是否已有逝尘（在给予之前）
        bool hadDustGoneBefore = target.GetPower<DustGone>() != null;

        // 2. 给予1层逝尘
        await PowerCmd.Apply<DustGone>(new ThrowingPlayerChoiceContext(), 
            target,
            DustGoneAmount,
            Owner.Creature,
            this
        );

        // 3. ✅ 使用 DamageCmd.Attack() 替代 CreatureCmd.Damage（参考 Hardship.cs）
        await DamageCmd.Attack(damageAmount)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash", null, null)
            .Execute(choiceContext);

        // 4. 如果之前就有逝尘，额外造成1次伤害并消耗
        if (hadDustGoneBefore)
        {
            // 额外造成1次伤害
            await DamageCmd.Attack(damageAmount)
                .FromCard(this)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash", null, null)
                .Execute(choiceContext);

            // ✅ 消耗此牌 - 使用 AddKeyword 替代 CardCmd.Exhaust
            AddKeyword(CardKeyword.Exhaust);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Damage"].UpgradeValueBy(UpgradeDamageBonus);
    }
}
