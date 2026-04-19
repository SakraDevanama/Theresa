using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Cards;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 宽恕追踪器 - 隐藏Power，用于监听玩家受到的伤害
/// 当玩家受到伤害时，增加手牌中所有"宽恕"卡牌的MagicNumber
/// 
/// 对应原版 Java Forgive 卡牌的 tookDamage() 机制
/// </summary>
public class ForgiveTrackerPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 不显示图标（隐藏Power）
    protected override bool IsVisibleInternal => false;

    /// <summary>
    /// 当拥有者受到伤害后触发
    /// </summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        MainFile.Logger?.Info($"[ForgiveTrackerPower] AfterDamageReceived called. target={target?.Name}, Owner={Owner?.Name}, UnblockedDamage={result.UnblockedDamage}");

        // 只处理玩家受到的实际伤害（未被格挡的部分）
        if (target != Owner)
        {
            MainFile.Logger?.Debug("[ForgiveTrackerPower] target != Owner, skipping");
            return;
        }
        if (result.UnblockedDamage <= 0)
        {
            MainFile.Logger?.Debug("[ForgiveTrackerPower] UnblockedDamage <= 0, skipping");
            return;
        }

        // 查找手牌中所有的 Forgive 卡牌并增加 MagicNumber
        if (Owner == null)
        {
            MainFile.Logger?.Warn("[ForgiveTrackerPower] Owner is null!");
            return;
        }

        var player = Owner.Player;
        if (player == null)
        {
            MainFile.Logger?.Warn("[ForgiveTrackerPower] Player is null!");
            return;
        }

        var handPile = PileType.Hand.GetPile(player);
        if (handPile == null)
        {
            MainFile.Logger?.Warn("[ForgiveTrackerPower] Hand pile is null!");
            return;
        }

        MainFile.Logger?.Info($"[ForgiveTrackerPower] Checking {handPile.Cards.Count} cards in hand");

        int forgiveCount = 0;
        foreach (var card in handPile.Cards)
        {
            if (card is Forgive forgiveCard)
            {
                forgiveCard.IncrementMagicNumber();
                forgiveCount++;
            }
        }

        MainFile.Logger?.Info($"[ForgiveTrackerPower] Found {forgiveCount} Forgive card(s), incremented MagicNumber");

        await Task.CompletedTask;
    }
}
