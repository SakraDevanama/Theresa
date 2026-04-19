using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Minions.Interfaces;

/// <summary>
/// 绑定随从的卡牌接口
/// 用于实现绑定到特定随从的卡牌功能
/// </summary>
public interface IMinionBoundCard
{
    public CardModel AsCardModel => (CardModel)this;

    public uint? BoundMinionCombatId { get; set; }

    public string? BoundMinionNameSnapshot { get; set; }
}

/// <summary>
/// IMinionBoundCard 的扩展方法
/// </summary>
public static class MinionBoundCardExtension
{
    /// <summary>
    /// 解析绑定的随从
    /// </summary>
    public static Creature? ResolveBoundMinion(this IMinionBoundCard minionBoundCard)
    {
        return minionBoundCard.AsCardModel.CombatState?.GetCreature(minionBoundCard.BoundMinionCombatId);
    }

    /// <summary>
    /// 绑定随从到此卡牌
    /// </summary>
    public static void BindMinion(this IMinionBoundCard minionBoundCard, Creature minion)
    {
        minionBoundCard.BoundMinionCombatId = minion.CombatId;
        minionBoundCard.BoundMinionNameSnapshot = minion.Name;
    }

    /// <summary>
    /// 添加绑定随从名称到卡牌描述
    /// </summary>
    public static void AddBoundNameToDescription(this IMinionBoundCard minionBoundCard, LocString description)
    {
        var deadSuffix = new LocString("cards", "bound_minion_dead_suffix").GetFormattedText();
        var minion = minionBoundCard.ResolveBoundMinion();

        string minionName;
        if (minion != null)
            minionName = minion.Name + (minion.IsAlive ? string.Empty : deadSuffix);
        else if (!string.IsNullOrEmpty(minionBoundCard.BoundMinionNameSnapshot))
            // If the bound minion no longer resolves by combat id, keep showing the last known name as dead.
            minionName = minionBoundCard.BoundMinionNameSnapshot + deadSuffix;
        else
            minionName = "???";

        description.Add("BoundMinionName", minionName);
    }
}
