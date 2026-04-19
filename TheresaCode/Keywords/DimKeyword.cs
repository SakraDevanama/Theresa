using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Theresa.TheresaCode.Keywords;

public static class DimKeyword
{
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword Dim;
}
