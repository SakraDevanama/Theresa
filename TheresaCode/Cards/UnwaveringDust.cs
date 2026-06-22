using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Enchantments;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 不渝尘埃 (UnwaveringDust)
/// 1费技能牌，罕见稀有度，消耗
///
/// 效果：获得 1 点能量。
/// 将此牌的复制可超出上限地放入微尘。
/// 初始附有 2 层意志丝线（不可被其他丝线替换）。
/// 升级：意志丝线层数 +1。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class UnwaveringDust() : TheresaCardModel(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    private const int BaseMindSilkAmount = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new DynamicVar("MindSilkAmount", BaseMindSilkAmount)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            foreach (var tip in HoverTipFactory.FromEnchantment<MindSilkEnchantment>())
            {
                yield return tip;
            }
        }
    }

    /// <summary>
    /// 锁定丝线，对应原版 Java 的 Theresa_Silk_Cannot_Replaced 标签。
    /// 一旦设为 true，AbstractSilkEnchantment.CanSetWhenSet 会拒绝附魔。
    /// </summary>
    public override bool IsSilkLocked => true;

    /// <summary>
    /// 构造时附上意志丝线（等价于 Java 的 setSilkWithoutTrigger，不触发事件）。
    /// 必须按当前升级等级初始化 BaseAmount，否则从存档加载时 AfterCreated 会覆盖
    /// OnUpgrade 已经加过的数值，导致两端状态分歧。
    /// </summary>
    public override void AfterCreated()
    {
        var mindSilk = (MindSilkEnchantment)ModelDb.Enchantment<MindSilkEnchantment>().ToMutable();
        int amount = BaseMindSilkAmount + CurrentUpgradeLevel;
        mindSilk.BaseAmount = amount;
        mindSilk.Amount = amount;
        DynamicVars["MindSilkAmount"].BaseValue = amount;
        this.EnchantInternal(mindSilk, amount);
    }

    /// <summary>
    /// 打出：获得 1 点能量 + 将此牌复制（可超出上限）放入微尘
    /// </summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        await PlayerCmd.GainEnergy(1, Owner);

        var copy = Owner.Creature.CombatState?.CreateCard(ModelDb.Card<UnwaveringDust>(), Owner)
            ?? Owner.RunState.CreateCard<UnwaveringDust>(Owner);
        await DustManager.AddCardOverLimit(copy);
    }

    /// <summary>
    /// 升级：意志丝线层数 +1
    /// </summary>
    protected override void OnUpgrade()
    {
        if (Enchantment is MindSilkEnchantment mindSilk)
        {
            mindSilk.BaseAmount += 1;
            mindSilk.Amount = mindSilk.BaseAmount;
            DynamicVars["MindSilkAmount"].BaseValue = mindSilk.BaseAmount;
        }
    }
}
