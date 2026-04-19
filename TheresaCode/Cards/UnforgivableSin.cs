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

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 罪无可赦 (UnforgivableSin)
/// 1费攻击牌
/// 造成 7 点伤害。你每消耗过1张牌，额外造成 1（+1） 点伤害。
/// 打出时：向卡组加入1张 萨卡兹见证。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class UnforgivableSin() : TheresaCardModel(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 基础伤害
    private const int BaseDamage = 7;
    // 每消耗牌额外伤害
    private const int DamagePerExhaust = 1;
    // 升级后额外伤害增量
    private const int UpgradeDamageDelta = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move),
        new DynamicVar("DamagePerExhaust", DamagePerExhaust),
        new ExhaustBasedDamageVar("TotalDamage", BaseDamage)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SarkazSee>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
        
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 获取目标敌人
        var target = cardPlay.Target as Creature;
        if (target == null || !target.IsAlive) return;

        // 1. 计算消耗牌数量
        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        int exhaustCount = exhaustPile.Cards.Count;

        // 2. 计算总伤害
        int baseDamage = (int)DynamicVars.Damage.BaseValue;
        int damagePerExhaust = (int)DynamicVars["DamagePerExhaust"].BaseValue;
        int bonusDamage = exhaustCount * damagePerExhaust;
        int totalDamage = baseDamage + bonusDamage;

        MainFile.Logger?.Info($"[UnforgivableSin] Dealing damage: base={baseDamage}, exhaustCount={exhaustCount}, bonus={bonusDamage}, total={totalDamage}");

        // 3. 造成伤害
        await DamageCmd.Attack(totalDamage)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        // 4. 向手牌加入1张 萨卡兹见证
        await AddSarkazSeeToHand(choiceContext);
    }

    /// <summary>
    /// 向手牌加入1张萨卡兹见证
    /// </summary>
    private async Task AddSarkazSeeToHand(PlayerChoiceContext choiceContext)
    {
        if (Owner == null || CombatState == null) return;

        // 使用 CombatState.CreateCard 创建萨卡兹见证卡牌
        var sarkazSee = CombatState.CreateCard<SarkazSee>(Owner);

        // 添加到手牌
        await CardPileCmd.AddGeneratedCardToCombat(sarkazSee, PileType.Hand, true);

        MainFile.Logger?.Info($"[UnforgivableSin] Added SarkazSee to hand");
    }

    protected override void OnUpgrade()
    {
        // 升级后每消耗牌额外伤害+1
        DynamicVars["DamagePerExhaust"].UpgradeValueBy(UpgradeDamageDelta);
    }
}

/// <summary>
/// 基于消耗牌数量计算总伤害的动态变量。
/// 替代原 RitsuLib 的 ModCardVars.Computed。
/// </summary>
public class ExhaustBasedDamageVar(string name, decimal baseDamage) : DynamicVar(name, baseDamage)
{
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        PreviewValue = CalculateTotalDamage(card);
    }

    protected override decimal GetBaseValueForIConvertible()
    {
        if (_owner is CardModel card)
            return CalculateTotalDamage(card);
        return BaseValue;
    }

    private static decimal CalculateTotalDamage(CardModel? card)
    {
        if (card == null) return 7m;
        
        var creature = card.Owner?.Creature;
        if (creature == null) return 7m;
        
        var exhaustPile = PileType.Exhaust.GetPile(card.Owner);
        int exhaustCount = exhaustPile.Cards.Count;
        
        if (!card.DynamicVars.TryGetValue("DamagePerExhaust", out var damagePerExhaustVar))
            return 7m;
        
        int baseDamage = (int)card.DynamicVars.Damage.BaseValue;
        int damagePerExhaust = (int)damagePerExhaustVar.BaseValue;
        
        return baseDamage + (exhaustCount * damagePerExhaust);
    }
}
