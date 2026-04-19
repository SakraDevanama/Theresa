using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Enchantments;

/// <summary>
/// 丝线（茧笼）附魔
/// 回合结束时：攻击牌对随机敌人造成3点伤害，技能牌获得3点格挡
/// 同时会向相邻卡牌传播丝线
/// </summary>
public class SilkThreadEnchantment : CustomEnchantmentModel
{
    protected override string? CustomIconPath => "res://Theresa/images/icons/silk_thread.png";

    /// <summary>
    /// 检查是否可以附魔到此卡。每张牌最多1张丝线。
    /// </summary>
    public override bool CanEnchant(CardModel card)
    {
        if (!base.CanEnchant(card)) return false;
        // 如果卡已有任何丝线附魔，不能再添加
        if (card.Enchantment is SilkThreadEnchantment) return false;
        return true;
    }
}
