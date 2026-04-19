using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 自定义动态变量：凋亡层数
/// </summary>
public class ApoptosisVar(int baseValue) : DynamicVar(Key, baseValue)
{
    public const string Key = "ApoptosisPower";
}

/// <summary>
/// 衰亡倒计时
/// 1费 能力牌
/// 每当一张牌被消耗时，给予生命值最高的敌人2（+1）层ApoptosisPower。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class ApoptosisCountdown() : TheresaCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<ApoptosisPower>(),
    ];
    
    
    // 注册动态变量
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ApoptosisVar(IsUpgraded ? 3 : 2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 应用能力：获得一层"衰亡倒计时"效果
        // 传递凋亡层数（2或3）作为能力的初始层数，用于后续计算
        int apoptosisAmount = IsUpgraded ? 3 : 2;
        await PowerCmd.Apply<ApoptosisCountdownEffect>(new ThrowingPlayerChoiceContext(), Owner.Creature, apoptosisAmount, Owner.Creature, this);
    }
    
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

/// <summary>
/// 衰亡倒计时效果实现
/// 监听卡牌消耗事件，给予生命值最高的敌人凋亡层数
/// </summary>
public class ApoptosisCountdownEffect : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
    // 内部隐藏：不在 UI 上显示这个能力图标
    // protected override bool IsVisibleInternal => false;

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        await base.AfterCardExhausted(choiceContext, card, causedByEthereal);
        OnCardExhausted(card);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await base.AfterCombatEnd(room);
    }

    /// <summary>
    /// 处理卡牌消耗事件
    /// </summary>
    private void OnCardExhausted(CardModel card)
    {
        // 确保战斗状态有效
        if (Owner == null) return;
        
        var combatState = Owner.CombatState;
        if (combatState == null) return;
        
        // 关键检查：只处理自己消耗的卡牌
        if (card.Owner != Owner.Player)
        {
            return;
        }

        // 获取凋亡层数（存储在能力的 Amount 中）
        int apoptosisAmount = (int)this.Amount;
        if (apoptosisAmount <= 0) apoptosisAmount = 2;

        // 获取生命值最高的敌人
        var target = GetHighestHpEnemy((CombatState)combatState);
        if (target != null)
        {
            // 异步应用凋亡能力
            _ = ApplyApoptosisAsync(target, apoptosisAmount);
        }
    }

    /// <summary>
    /// 异步应用凋亡能力
    /// </summary>
    private async Task ApplyApoptosisAsync(Creature target, int amount)
    {
        if (Owner == null) return;
        
        await PowerCmd.Apply<ApoptosisPower>(new ThrowingPlayerChoiceContext(), target, amount, Owner, null);
    }

    /// <summary>
    /// 获取生命值最高的敌人
    /// </summary>
    private Creature? GetHighestHpEnemy(CombatState combatState)
    {
        if (Owner == null) return null;

        // 获取所有敌方单位
        var enemies = combatState.GetOpponentsOf(Owner)
            .Where(c => c.IsAlive)
            .ToList();

        if (enemies.Count == 0) return null;

        // 返回生命值最高的敌人
        return enemies.OrderByDescending(c => c.CurrentHp).FirstOrDefault();
    }

    //public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    // {
        // 玩家回合结束后不移除此效果，因为它是持续整场战斗的
        // }
}
