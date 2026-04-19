using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Theresa.TheresaCode.Keywords;

/// <summary>
/// Dust 状态标记 - 用于标记 ExhaustPile 中的 Dust 卡牌
/// 这是一个内部标记，不显示在卡牌 UI 上
/// </summary>
public static class DustMarkKeyword
{
    /// <summary>
    /// 标记卡牌当前处于 Dust 状态（存储在 ExhaustPile 中）
    /// </summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Mark;
}
