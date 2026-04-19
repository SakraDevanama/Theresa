using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 痛觉相连
/// 0费能力牌
/// 打出后消耗。本回合每次给予敌人茧缚时都会自身获得相同层数
/// 且所有茧缚会额外触发2（+2）次
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class PainfulConnection() : TheresaCardModel(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    // 基础额外触发次数
    private const int BaseExtraTriggers = 2;
    private const int UpgradeExtraTriggersDelta = 2; // 升级后额外+2次
    
    // 添加消耗关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<SilkCocoon>(),
        HoverTipFactory.FromPower<Broken>()
    ];
    
    // 动态变量：额外触发次数
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ExtraTriggers", BaseExtraTriggers)
    ];
    

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 给自身添加痛觉相连效果（用于本回合跟踪茧缚给予）
        var effect = Owner.Creature.Powers.OfType<PainfulConnectionEffect>().FirstOrDefault();
        if (effect == null)
        {
            await PowerCmd.Apply<PainfulConnectionEffect>(Owner.Creature, GetExtraTriggerCount(), Owner.Creature, this);
        }
        else
        {
            // 如果已有效果，更新层数（额外触发次数）
            await PowerCmd.ModifyAmount(effect, GetExtraTriggerCount() - effect.Amount, Owner.Creature, this);
        }
    }

    /// <summary>
    /// 获取额外触发次数
    /// </summary>
    private int GetExtraTriggerCount() => (int)DynamicVars["ExtraTriggers"].BaseValue;

    protected override void OnUpgrade()
    {
        // 升级后额外触发次数+2（从2次增加到4次）
        DynamicVars["ExtraTriggers"].UpgradeValueBy(UpgradeExtraTriggersDelta);
    }
}

/// <summary>
/// 痛觉相连效果 - 本回合内每次给予敌人茧缚时，自身也获得相同层数
/// </summary>
public sealed class PainfulConnectionEffect : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 内部隐藏：不在 UI 上显示这个能力图标
    protected override bool IsVisibleInternal => true;

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // 只处理茧缚的变化，且不是自身的变化
        if (power is not SilkCocoon || power == this) return;
        
        // 检查是否是玩家给敌人施加的茧缚
        if (applier?.Side != CombatSide.Player) return;
        if (power.Owner?.Side != CombatSide.Enemy) return;
        
        // amount 是层数变化量，只要变化量大于0就触发
        if (amount <= 0) return;
        
        // 给玩家施加相同层数的茧缚
        if (Owner != null && Owner.IsAlive)
        {
            await PowerCmd.Apply<SilkCocoon>(Owner, amount, Owner, cardSource);
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // 回合结束时，如果持有者是玩家，则移除这个效果
        if (Owner?.Side == side)
        {
            await PowerCmd.Remove(this);
        }
    }
}
