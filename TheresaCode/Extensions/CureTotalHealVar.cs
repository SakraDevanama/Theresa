using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Extensions;

/// <summary>
/// 愈合卡牌的可回复生命值动态变量
/// </summary>
public class CureTotalHealVar() : DynamicVar(Key, 0m)
{
    // 在描述中用作占位符的键
    public const string Key = "Theresa-CureTotalHeal";
    // 本地化键
    public static readonly string LocKey = Key.ToUpperInvariant();

    // 每损失多少生命回复1点
    private const int HpLostPerHeal = 5;

    /// <summary>
    /// 更新卡牌预览时的数值
    /// </summary>
    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
    {
        BaseValue = CalculateHealAmount(card.Owner?.Creature);
    }

    /// <summary>
    /// 计算可回复的生命值
    /// </summary>
    private static decimal CalculateHealAmount(Creature? creature)
    {
        if (creature == null) return 0m;
        
        int maxHp = creature.MaxHp;
        int currentHp = creature.CurrentHp;
        int hpLost = maxHp - currentHp;
        
        if (hpLost <= 0) return 0m;
        
        return (decimal)hpLost / HpLostPerHeal;
    }
}
