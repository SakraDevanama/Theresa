using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Keywords;

/// <summary>
/// 召唤阿米娅关键词
/// </summary>
public static class SummonAmiyaKeyword
{
    /// <summary>
    /// 召唤阿米娅
    /// 效果：召唤阿米娅（20点生命）。阿米娅会在玩家回合结束时为所有友方单位恢复生命，并且可以使用[渐强]行动对敌人造成伤害。
    /// </summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword SummonAmiya;
}
