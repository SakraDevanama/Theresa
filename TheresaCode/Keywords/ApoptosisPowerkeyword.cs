using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Keywords;


public static class Apoptosis
{
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Apopto;

    /// <summary>
    /// 判断卡牌是否带有“Apopto”关键词
    /// </summary>
    public static bool IsMemory(this CardModel card)
    {
        return card.Keywords.Contains(Apopto);
    }
}