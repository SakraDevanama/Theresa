
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Theresa.TheresaCode.Keywords;








public static class SilkKeyword

{
    [CustomEnum]
    // 放在原版卡牌描述的位置（例如消耗、虚无等标签的位置）
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Silk;
}