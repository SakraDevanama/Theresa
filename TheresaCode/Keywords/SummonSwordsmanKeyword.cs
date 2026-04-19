using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Keywords;

/// <summary>
/// 召唤特雷西斯关键词
/// </summary>
public static class SummonSwordsmanKeyword
{
    /// <summary>
    /// 召唤特雷西斯
    /// 效果：召唤特雷西斯（25点生命，3点力量）。特雷西斯会在玩家回合开始时自动攻击随机敌人。
    /// </summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword SummonSwordsman;
}
