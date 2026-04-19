using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Extensions;

/// <summary>
/// 终结卡牌 - 自动打出牌数变量
/// 用于显示自动打出的牌数（X张，升级后X+1张）
/// </summary>
public class FinalePlayCountVar : DynamicVar
{
    // 在描述中用作占位符的键
    public const string Key = "Theresa-FinalePlayCount";
    // 本地化键
    public static readonly string LocKey = Key.ToUpperInvariant();

    public FinalePlayCountVar(decimal baseValue) : base(Key, baseValue)
    {
        this.WithTooltip(LocKey);
    }
}

/// <summary>
/// 终结卡牌 - 伤害变量
/// 用于显示伤害值
/// </summary>
public class FinaleDamageVar : DamageVar
{
    public FinaleDamageVar(decimal baseValue) : base(baseValue, ValueProp.Move)
    {
    }
}
