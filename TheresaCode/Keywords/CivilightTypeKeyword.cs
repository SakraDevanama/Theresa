using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Theresa.TheresaCode.Keywords;

/// <summary>
/// 文明的存续 - 可重现的牌类型提示关键词
/// 这些关键词仅用于卡牌描述中的提示，不触发实际游戏逻辑
/// </summary>
public static class CivilightTypeKeyword
{
    /// <summary>
    /// 从攻击牌中重现
    /// </summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword CivilightAttack;

    /// <summary>
    /// 从技能牌中重现
    /// </summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword CivilightSkill;

    /// <summary>
    /// 从任意牌中重现
    /// </summary>
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.Before)]
    public static CardKeyword CivilightAny;
}
