using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Theresa.TheresaCode.Cards;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Enchantments;

/// <summary>
/// 记忆丝线（原 MemorySilk）
/// 
/// 特性：
/// 1. 不会在回合结束时传播（CanSpreadAtTurnEnd 返回 false）
/// 2. 打出后触发记忆效果：
///    - 如果有恨意，移除至多3层
///    - 移除1层：获得金属化（Plating）
///    - 移除2层：额外获得荆棘（Thorns）
///    - 移除3层：再打出一次此牌（已记忆版本）
/// </summary>
public class MemorySilkEnchantment : AbstractSilkEnchantment
{
    protected override string? CustomIconPath => "res://Theresa/images/icons/silk_thread2.png";

    /// <summary>
    /// 标记此记忆丝线是否已经触发过"再打出一次"效果
    /// 对应原版 Java 的 isMemoried
    /// </summary>
    public bool IsMemoried { get; set; } = false;

    public MemorySilkEnchantment()
    {
        BaseAmount = 1;
        Amount = 1;
    }

    /// <summary>
    /// 记忆丝线不会传播（无论是回合结束还是主动传播）
    /// </summary>
    public override bool CanSpreadAtTurnEnd(CardModel cardToSpread, bool atTurnEnd)
    {
        return false;
    }

    /// <summary>
    /// 卡牌打出时触发记忆效果
    /// 对应原版 Java MemorySilk.afterPlayed()
    /// 
    /// 注意：在 STS2 中，EnchantmentModel.OnPlay 在卡牌打出后被调用，
    /// 所以记忆丝线覆盖 OnPlay 而不是 AfterPlayed。
    /// AfterPlayed 方法被覆盖为空实现，防止 SilkTriggerPatch 双重触发。
    /// </summary>
    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        var playerId = Card?.Owner?.NetId ?? 0;
        var cardName = Card?.Id.Entry ?? "null";
        Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay START: card={cardName}, player={playerId}, IsMemoried={IsMemoried}");

        if (Card?.Owner?.Creature == null)
        {
            Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay EARLY RETURN: Card.Owner.Creature is null");
            return;
        }

        var owner = Card.Owner.Creature;
        var hatePower = owner.Powers.FirstOrDefault(p => p is ZaakathHatePower) as ZaakathHatePower;

        Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: hatePower={hatePower?.Id.Entry ?? "null"}, amount={hatePower?.Amount ?? 0}");

        if (hatePower == null || hatePower.Amount <= 0)
        {
            Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay EARLY RETURN: hatePower is null or amount <= 0");
            return;
        }

        // 计算移除层数：已记忆的移除2层，未记忆的移除3层
        int maxRemove = IsMemoried ? 2 : 3;
        int removeAmount = Math.Min((int)hatePower.Amount, maxRemove);
        Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: maxRemove={maxRemove}, removeAmount={removeAmount}");

        if (removeAmount <= 0)
        {
            Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay EARLY RETURN: removeAmount <= 0");
            return;
        }

        // 移除3层时：再打出一次此牌（已记忆版本）
        if (removeAmount == 3 && Card != null)
        {
            Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: Triggering third layer effect (AutoPlay copy)");

            // BornInDarkness（生于黑夜）升级后的特殊效果：获得1层希望
            // 对应原版 Java: FromNight.onSpecialTrigger()
            if (Card is BornInDarkness bornInDarkness && bornInDarkness.IsUpgraded)
            {
                Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: Applying TheresiasHopePower");
                await PowerCmd.Apply<TheresiasHopePower>(choiceContext, owner, 1, owner, Card);
            }

            // 创建卡牌副本并标记为已记忆
            // 使用 CombatState.CloneCard 正确复制并注册到 CombatState
            var combatState = Card.CombatState;
            Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: combatState={combatState != null}");
            if (combatState == null)
            {
                Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay EARLY RETURN: combatState is null");
                return;
            }

            var cardCopy = combatState.CloneCard(Card);
            Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: cardCopy created, id={cardCopy?.Id.Entry ?? "null"}, owner={cardCopy?.Owner?.NetId ?? 0}");
            if (cardCopy.Enchantment is MemorySilkEnchantment memorySilk)
            {
                memorySilk.IsMemoried = true;
                Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: cardCopy.IsMemoried set to true");
            }

            // 将副本加入手牌并免费自动打出
            Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: Adding cardCopy to Hand");
            await CardPileCmd.Add(cardCopy, PileType.Hand, CardPilePosition.Top, this);
            cardCopy.SetToFreeThisTurn();

            var target = GetTargetForCard(cardCopy);
            Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: AutoPlay target={target?.Name ?? "null"}");
            await CardCmd.AutoPlay(choiceContext, cardCopy, target, AutoPlayType.Default);
            Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: AutoPlay completed");
        }

        // 移除2层或以上时：获得荆棘
        if (removeAmount >= 2)
        {
            Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: Applying ThornsPower");
            await PowerCmd.Apply<ThornsPower>(new ThrowingPlayerChoiceContext(), owner, Amount, owner, Card);
        }

        // 移除1层或以上时：获得金属化
        if (removeAmount >= 1)
        {
            Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: Applying PlatingPower");
            await PowerCmd.Apply<PlatingPower>(new ThrowingPlayerChoiceContext(), owner, Amount, owner, Card);
        }

        // 移除恨意
        Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: Modifying hatePower by {-removeAmount}, current={hatePower.Amount}");
        var newAmount = await PowerCmd.ModifyAmount(choiceContext, hatePower, -removeAmount, owner, Card);
        Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay: HatePower modified, newAmount={newAmount}");
        Theresa.MainFile.Logger?.Info($"[MemorySilk] OnPlay END: card={cardName}, player={playerId}");
    }

    /// <summary>
    /// 覆盖 AfterPlayed 防止 SilkTriggerPatch 双重触发。
    /// MemorySilk 的效果已经在 OnPlay 中执行（由 CardModel.OnPlayWrapper 调用 Enchantment.OnPlay），
    /// 不需要再通过 SilkTriggerPatch.AfterCardPlayedPatch 执行。
    /// </summary>
    public override async Task AfterPlayed(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        // 空实现：防止 AbstractSilkEnchantment.AfterPlayed 默认调用 OnPlay 导致双重触发
        await Task.CompletedTask;
    }

    /// <summary>
    /// 根据卡牌目标类型选择目标
    /// 参考 FINALE 的实现
    /// </summary>
    private Creature? GetTargetForCard(CardModel card)
    {
        if (card.CombatState == null) return null;

        return card.TargetType switch
        {
            TargetType.AnyEnemy => card.CombatState.HittableEnemies.FirstOrDefault(),
            TargetType.AllEnemies => card.CombatState.HittableEnemies.FirstOrDefault(),
            TargetType.RandomEnemy => card.CombatState.HittableEnemies.FirstOrDefault(),
            TargetType.AnyAlly => card.Owner?.Creature,
            TargetType.AllAllies => card.Owner?.Creature,
            TargetType.Self => card.Owner?.Creature,
            TargetType.AnyPlayer => card.Owner?.Creature,
            _ => null
        };
    }
}
