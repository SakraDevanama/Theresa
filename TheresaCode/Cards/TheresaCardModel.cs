using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Cards;

[Pool(typeof(TheresaCardPool))]
public abstract partial class TheresaCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary = true, bool autoAdd = true) : CustomCardModel(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
{
    /// <summary>
    /// 当此牌在牌堆间移动时触发（由 GlobalMoveSystem 直接调用）。
    /// </summary>
    public virtual Task OnGlobalMove(PileType from, PileType to, AbstractModel? source) => Task.CompletedTask;
    // 静态只读正则表达式，用于将驼峰命名法转换为下划线命名法
    private static readonly Regex CamelCaseRegex = MyRegex();

    // 生成卡片的唯一标识符
    protected virtual string CardId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    // 卡片肖像的基础路径
    protected virtual string PortraitBasePath => $"res://Theresa/images/card_portraits/{CardId}";

    // 自定义卡牌肖像路径 - 使用 BaseLib 的 CustomPortraitPath 属性
    public override string? CustomPortraitPath => GetEffectivePortraitPath();

    /// <summary>
    /// 自定义 Spine 动画肖像场景路径。如果子类 override 返回一个 .tscn 路径，
    /// SovereignSpinePortraitPatch 会用它来替换 AncientPortrait 的纹理。
    /// </summary>
    public virtual string? CustomSpinePortraitScenePath => null;

    // 卡片肖像的默认路径
    private static readonly string DefaultPortraitPath = "res://Theresa/images/card_portraits/default_card.png"; 

    /// <summary>
    /// 获取有效的卡片肖像路径。如果特定卡牌的图片存在，则返回其路径；否则返回默认路径。
    /// </summary>
    private string GetEffectivePortraitPath()
    {
        try
        {
            string specificPortraitPath = $"{PortraitBasePath}.png";
            
            if (!string.IsNullOrWhiteSpace(specificPortraitPath) && specificPortraitPath.StartsWith("res://"))
            {
                if (ResourceLoader.Exists(specificPortraitPath))
                {
                    return specificPortraitPath;
                }
            }
        }
        catch
        {
            // 忽略所有异常
        }
        
        return DefaultPortraitPath;
    }

    // 生成正则表达式
    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
