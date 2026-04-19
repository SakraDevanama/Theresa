using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Keywords;





public static class DivinityStanceKeyword
{
    /// <summary>
    /// 魔王残响
    /// </summary>
    [CustomEnum("Divin")]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword DivinityStance;
    
}