using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Keywords;



public static class DustKeyword
{
    /// <summary>
    /// 微尘
    /// 效果：打出后移除至多3层恨意。若移除数不少于1获得3点敏捷；不少于2获得荆棘；不少于3额外打出此牌一次。
    /// </summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Dust;


    public static bool IsMemory(this CardModel card)
    {
        return card.Keywords.Contains(Dust);
    }
}