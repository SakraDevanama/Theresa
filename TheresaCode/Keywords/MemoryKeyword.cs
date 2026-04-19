using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Keywords;



public static class MemoryKeyword
{
    /// <summary>
    /// 记忆（Memory）关键词
    /// 效果：打出后移除至多3层恨意。若移除数不少于1获得3点敏捷；不少于2获得荆棘；不少于3额外打出此牌一次。
    /// </summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Memory;

    /// <summary>
    /// 判断卡牌是否带有“记忆”关键词
    /// </summary>
    public static bool IsMemory(this CardModel card)
    {
        return card.Keywords.Contains(Memory);
    }
}