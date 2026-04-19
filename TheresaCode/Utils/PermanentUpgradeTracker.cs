using MegaCrit.Sts2.Core.Entities.Cards;

namespace Theresa.TheresaCode.Utils;

/// <summary>
/// 整局游戏升级追踪器
/// 用于追踪卡牌是否在整局游戏中被升级过
/// </summary>
public static class PermanentUpgradeTracker
{
    // 记录每张卡牌在整局游戏中是否已经被升级过
    private static readonly Dictionary<string, HashSet<string>> _upgradedCardsThisRun = new();

    /// <summary>
    /// 检查卡牌是否已在整局游戏中被升级过
    /// </summary>
    public static bool IsUpgradedForRun(string runId, string cardId)
    {
        if (!_upgradedCardsThisRun.ContainsKey(runId))
            return false;
        return _upgradedCardsThisRun[runId].Contains(cardId);
    }

    /// <summary>
    /// 标记卡牌已在整局游戏中被升级
    /// </summary>
    public static void MarkAsUpgradedForRun(string runId, string cardId)
    {
        if (!_upgradedCardsThisRun.ContainsKey(runId))
        {
            _upgradedCardsThisRun[runId] = new HashSet<string>();
        }
        _upgradedCardsThisRun[runId].Add(cardId);
    }

    /// <summary>
    /// 清除运行记录（新游戏开始时调用）
    /// </summary>
    public static void ClearRunRecord(string runId)
    {
        if (_upgradedCardsThisRun.ContainsKey(runId))
        {
            _upgradedCardsThisRun.Remove(runId);
        }
    }
}
