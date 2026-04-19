using MegaCrit.Sts2.Core.Entities.Cards;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 卡牌边框贴图路径配置，方便单独修改和维护
/// </summary>
public static class CardFramePaths
{
    /// <summary>
    /// 根据卡牌类型和稀有度返回对应的 AncientBorder 贴图路径//主要贴图
    /// 优先级：Type+Rarity 组合贴图 > 纯 Type 贴图 > 默认贴图
    /// </summary>
    public static string GetAncientBorderTexturePathForTypeAndRarity(CardType type, CardRarity rarity)
    {
        // 先尝试 Type + Rarity 组合路径
        string? comboPath = rarity switch
        {
            CardRarity.None => type switch
            {
                CardType.Attack => "res://Theresa/images/card_frames/attack/border_attack_basic.png",
                CardType.Skill => "res://Theresa/images/card_frames/Skill/border_skill_basic.png",
                CardType.Power => "res://Theresa/images/card_frames/power/border_power_basic.png",
                CardType.Status => "res://Theresa/images/card_frames/border_status_none.png",
                CardType.Curse => "res://Theresa/images/card_frames/border_curse_none.png",
                CardType.Quest => "res://Theresa/images/card_frames/border_quest_none.png",
                _ => "res://Theresa/images/card_frames/border_default_none.png"
            },
            CardRarity.Basic => type switch
            {
                CardType.Attack => "res://Theresa/images/card_frames/attack/border_attack_basic.png",
                CardType.Skill => "res://Theresa/images/card_frames/Skill/border_skill_basic.png",
                CardType.Power => "res://Theresa/images/card_frames/power/border_power_basic.png",
                CardType.Status => "res://Theresa/images/card_frames/attack/border_attack_basic.png",
                CardType.Curse => "res://Theresa/images/card_frames/attack/border_attack_basic.png",
                CardType.Quest => "res://Theresa/images/card_frames/attack/border_attack_basic.png",
                _ => "res://Theresa/images/card_frames/attack/border_attack_basic.png"
            },
            CardRarity.Common => type switch
            {
                CardType.Attack => "res://Theresa/images/card_frames/attack/border_attack_basic.png",
                CardType.Skill => "res://Theresa/images/card_frames/Skill/border_skill_basic.png",
                CardType.Power => "res://Theresa/images/card_frames/power/border_power_basic.png",
                CardType.Status => "res://Theresa/images/card_frames/border_status_common.png",
                CardType.Curse => "res://Theresa/images/card_frames/border_curse_common.png",
                CardType.Quest => "res://Theresa/images/card_frames/border_quest_common.png",
                _ => "res://Theresa/images/card_frames/border_default_common.png"
            },
            CardRarity.Uncommon => type switch
            {
                CardType.Attack => "res://Theresa/images/card_frames/attack/border_attack_uncommon.png",
                CardType.Skill => "res://Theresa/images/card_frames/Skill/border_skill_uncommon.png",
                CardType.Power => "res://Theresa/images/card_frames/power/border_power_uncommon.png",
                CardType.Status => "res://Theresa/images/card_frames/border_status_uncommon.png",
                CardType.Curse => "res://Theresa/images/card_frames/border_curse_uncommon.png",
                CardType.Quest => "res://Theresa/images/card_frames/border_quest_uncommon.png",
                _ => "res://Theresa/images/card_frames/border_default_uncommon.png"
            },
            CardRarity.Rare => type switch
            {
                CardType.Attack => "res://Theresa/images/card_frames/attack/border_attack_rare.png",
                CardType.Skill => "res://Theresa/images/card_frames/Skill/border_skill_rare.png",
                CardType.Power => "res://Theresa/images/card_frames/power/border_power_rare.png",
                CardType.Status => "res://Theresa/images/card_frames/border_status_rare.png",
                CardType.Curse => "res://Theresa/images/card_frames/border_curse_rare.png",
                CardType.Quest => "res://Theresa/images/card_frames/border_quest_rare.png",
                _ => "res://Theresa/images/card_frames/border_default_rare.png"
            },
            CardRarity.Ancient => type switch
            {
                CardType.Attack => "res://Theresa/images/card_frames/Skill/border_skill_ancient.png",
                CardType.Skill => "res://Theresa/images/card_frames/Skill/border_skill_ancient.png",
                CardType.Power => "res://Theresa/images/card_frames/Skill/border_skill_ancient.png",
                CardType.Status => "res://Theresa/images/card_frames/Skill/border_skill_ancient.png",
                CardType.Curse => "res://Theresa/images/card_frames/Skill/border_skill_ancient.png",
                CardType.Quest => "res://Theresa/images/card_frames/Skill/border_skill_ancient.png",
                _ => "res://Theresa/images/card_frames/Skill/border_skill_ancient.png"
            },
            CardRarity.Event => type switch
            {
                CardType.Attack => "res://Theresa/images/card_frames/attack/border_attack_event.png",
                CardType.Skill => "res://Theresa/images/card_frames/Skill/border_skill_event.png",
                CardType.Power => "res://Theresa/images/card_frames/power/border_power_event.png",
                CardType.Status => "res://Theresa/images/card_frames/border_status_event.png",
                CardType.Curse => "res://Theresa/images/card_frames/border_curse_event.png",
                CardType.Quest => "res://Theresa/images/card_frames/Quest/border_quest_event.png",
                _ => "res://Theresa/images/card_frames/border_default_event.png"
            },
            CardRarity.Token => type switch
            {
                CardType.Attack => "res://Theresa/images/card_frames/attack/border_attack_token.png",
                CardType.Skill => "res://Theresa/images/card_frames/Skill/border_skill_token.png",
                CardType.Power => "res://Theresa/images/card_frames/power/border_power_token.png",
                CardType.Status => "res://Theresa/images/card_frames/border_status_token.png",
                CardType.Curse => "res://Theresa/images/card_frames/border_curse_token.png",
                CardType.Quest => "res://Theresa/images/card_frames/border_quest_token.png",
                _ => "res://Theresa/images/card_frames/border_default_token.png"
            },
            CardRarity.Status => type switch
            {
                CardType.Attack => "res://Theresa/images/card_frames/border_attack_status.png",
                CardType.Skill => "res://Theresa/images/card_frames/border_skill_status.png",
                CardType.Power => "res://Theresa/images/card_frames/border_power_status.png",
                CardType.Status => "res://Theresa/images/card_frames/border_status_status.png",
                CardType.Curse => "res://Theresa/images/card_frames/border_curse_status.png",
                CardType.Quest => "res://Theresa/images/card_frames/border_quest_status.png",
                _ => "res://Theresa/images/card_frames/border_default_status.png"
            },
            CardRarity.Curse => type switch
            {
                CardType.Attack => "res://Theresa/images/card_frames/border_attack_curse.png",
                CardType.Skill => "res://Theresa/images/card_frames/border_skill_curse.png",
                CardType.Power => "res://Theresa/images/card_frames/border_power_curse.png",
                CardType.Status => "res://Theresa/images/card_frames/border_status_curse.png",
                CardType.Curse => "res://Theresa/images/card_frames/border_curse_curse.png",
                CardType.Quest => "res://Theresa/images/card_frames/border_quest_curse.png",
                _ => "res://Theresa/images/card_frames/border_default_curse.png"
            },
            CardRarity.Quest => type switch
            {
                CardType.Attack => "res://Theresa/images/card_frames/border_attack_quest.png",
                CardType.Skill => "res://Theresa/images/card_frames/border_skill_quest.png",
                CardType.Power => "res://Theresa/images/card_frames/border_power_quest.png",
                CardType.Status => "res://Theresa/images/card_frames/border_status_quest.png",
                CardType.Curse => "res://Theresa/images/card_frames/border_curse_quest.png",
                CardType.Quest => "res://Theresa/images/card_frames/border_quest_quest.png",
                _ => "res://Theresa/images/card_frames/border_default_quest.png"
            },
            _ => null
        };

        // 直接返回组合路径，不再用 FileExists 检查
        // 因为在导出/Mod 环境中 Godot 的文件系统检查经常不可靠
        if (!string.IsNullOrEmpty(comboPath))
        {
            return comboPath;
        }

        // 回退到纯 Type 路径
        switch (type)
        {
            case CardType.Attack:
                return "res://Theresa/images/card_frames/border_attack.png";
            case CardType.Skill:
                return "res://Theresa/images/card_frames/border_skill.png";
            case CardType.Power:
                return "res://Theresa/images/card_frames/border_power.png";
            case CardType.Status:
                return "res://Theresa/images/card_frames/border_default2.png";
            case CardType.Curse:
                return "res://Theresa/images/card_frames/border_default2.png";
            case CardType.Quest:
                return "res://Theresa/images/card_frames/border_default2.png";
            case CardType.None:
            default:
                return "res://Theresa/images/card_frames/border_default2.png";
        }
    }
}
