using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using System.Linq;
using Theresa.TheresaCode.Enchantments;

namespace Theresa.TheresaCode.Commands;

/// <summary>
/// 编织动作 - 统一处理丝线的赋予
/// 
/// 对应原版 Java 的 SetSilkAction + RandomSilkAction 的组合。
/// 
/// 核心逻辑：
/// 1. 检查目标卡是否可以被附魔（CanSetWhenSet）
/// 2. 如果目标已有丝线，检查新丝线是否可以替换（CanReplace）
/// 3. 如果替换，继承原丝线的 Amount
/// 4. 调用 CardCmd.Enchant 附魔
/// 5. 调用 ApplyPowers 更新数值
/// </summary>
public static class WeaveCmd
{
    /// <summary>
    /// 对单张卡牌编织指定丝线
    /// </summary>
    /// <param name="target">目标卡牌</param>
    /// <param name="silk">要编织的丝线（可变副本）</param>
    /// <param name="mustReplace">是否必须替换已有丝线</param>
    /// <param name="canReplace">是否可以替换已有丝线</param>
    /// <returns>是否成功编织</returns>
    public static bool Weave(CardModel target, AbstractSilkEnchantment silk, bool mustReplace = false, bool canReplace = true)
    {
        if (target == null) return false;
        if (silk == null) return false;

        // 检查是否可以附魔到目标卡
        if (!silk.CanSetWhenSet(target))
            return false;

        var existingSilk = target.Enchantment as AbstractSilkEnchantment;

        // 目标已有丝线
        if (existingSilk != null)
        {
            // 同类型，不处理
            if (existingSilk.GetType() == silk.GetType())
                return false;

            // 必须替换但无法替换
            if (mustReplace && !silk.CanReplace(existingSilk))
                return false;

            // 可以替换的情况
            if (canReplace && silk.CanReplace(existingSilk))
            {
                // 继承原丝线的数值
                silk.Amount = existingSilk.Amount;
                silk.BaseAmount = existingSilk.BaseAmount;
            }
            else
            {
                // 不能替换，跳过
                return false;
            }
        }

        try
        {
            // 清除旧附魔（如果有）
            if (target.Enchantment != null)
            {
                CardCmd.ClearEnchantment(target);
            }

            // 附魔新丝线
            CardCmd.Enchant(silk, target, silk.Amount);

            // 应用 Power 影响
            silk.ApplyPowers();

            return true;
        }
        catch (InvalidOperationException)
        {
            // 某些卡牌不能被附魔，跳过
            return false;
        }
    }

    /// <summary>
    /// 从候选卡牌中随机选择一张编织丝线
    /// </summary>
    /// <param name="candidates">候选卡牌列表</param>
    /// <param name="silkPrototype">丝线原型（会被克隆）</param>
    /// <param name="mustReplace">是否必须替换已有丝线</param>
    /// <param name="canReplace">是否可以替换已有丝线</param>
    /// <param name="typeOnly">仅对指定类型的卡牌编织</param>
    /// <returns>是否成功编织</returns>
    public static bool WeaveRandom(IEnumerable<CardModel> candidates, AbstractSilkEnchantment silkPrototype, bool mustReplace = false, bool canReplace = true, CardType? typeOnly = null)
    {
        var validCandidates = new List<CardModel>();
        var replaceCandidates = new List<CardModel>();

        foreach (var card in candidates)
        {
            if (card == null) continue;

            // 类型过滤
            if (typeOnly.HasValue && card.Type != typeOnly.Value)
                continue;

            // 检查是否可以附魔
            if (!silkPrototype.CanSetWhenSet(card))
                continue;

            var existingSilk = card.Enchantment as AbstractSilkEnchantment;

            if (existingSilk == null)
            {
                // 无丝线，直接可以编织
                if (!mustReplace)
                    validCandidates.Add(card);
            }
            else if (existingSilk.GetType() != silkPrototype.GetType())
            {
                // 有丝线但类型不同
                if (mustReplace && silkPrototype.CanReplace(existingSilk))
                {
                    validCandidates.Add(card);
                }
                else if (canReplace && silkPrototype.CanReplace(existingSilk))
                {
                    replaceCandidates.Add(card);
                }
            }
        }

        CardModel? targetCard = null;

        // 使用同步 RNG 随机选择（确保联机一致性）
        var player = candidates.FirstOrDefault(c => c?.Owner != null)?.Owner;
        
        // 优先选择无丝线的卡牌
        if (validCandidates.Count > 0)
        {
            int index = player != null 
                ? player.RunState.Rng.CombatTargets.NextInt(validCandidates.Count)
                : new Random().Next(validCandidates.Count);
            targetCard = validCandidates[index];
        }
        // 其次选择可以替换的卡牌
        else if (replaceCandidates.Count > 0)
        {
            int index = player != null 
                ? player.RunState.Rng.CombatTargets.NextInt(replaceCandidates.Count)
                : new Random().Next(replaceCandidates.Count);
            targetCard = replaceCandidates[index];
        }

        if (targetCard == null)
            return false;

        // 克隆丝线实例
        var silk = (AbstractSilkEnchantment)silkPrototype.MutableClone();

        // 如果替换，继承原数值
        var existing = targetCard.Enchantment as AbstractSilkEnchantment;
        if (existing != null && silk.CanReplace(existing))
        {
            silk.Amount = existing.Amount;
            silk.BaseAmount = existing.BaseAmount;
        }

        return Weave(targetCard, silk, mustReplace, canReplace);
    }

    /// <summary>
    /// 对牌堆中所有无丝线卡牌编织
    /// </summary>
    public static int WeaveAll(IEnumerable<CardModel> targets, AbstractSilkEnchantment silkPrototype)
    {
        int count = 0;
        foreach (var card in targets)
        {
            if (card?.Enchantment == null)
            {
                var silk = (AbstractSilkEnchantment)silkPrototype.MutableClone();
                if (Weave(card, silk))
                    count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 对牌堆中所有卡牌编织（包括替换已有丝线）
    /// </summary>
    public static int WeaveAllForce(IEnumerable<CardModel> targets, AbstractSilkEnchantment silkPrototype)
    {
        int count = 0;
        foreach (var card in targets)
        {
            var silk = (AbstractSilkEnchantment)silkPrototype.MutableClone();
            if (Weave(card, silk, mustReplace: false, canReplace: true))
                count++;
        }
        return count;
    }
}
