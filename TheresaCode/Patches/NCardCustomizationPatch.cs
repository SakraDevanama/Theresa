using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using Theresa.TheresaCode.Cards;

namespace Theresa.TheresaCode.Patches;

// 将所有反射字段移到一个静态类中，方便管理
public static class NCardReflectionFields
{
    // 注意：这些字段在某些 NCard 变体（如卡牌库页面）中可能不存在
    public static readonly FieldInfo? AncientTextBgField = typeof(NCard).GetField("_ancientTextBg", BindingFlags.NonPublic | BindingFlags.Instance);
    public static readonly FieldInfo? AncientBorderField = typeof(NCard).GetField("_ancientBorder", BindingFlags.NonPublic | BindingFlags.Instance);
    public static readonly FieldInfo? AncientBorderGlassOverlayField = typeof(NCard).GetField("_ancientBorderGlassOverlay", BindingFlags.NonPublic | BindingFlags.Instance);
    public static readonly FieldInfo? AncientBannerField = typeof(NCard).GetField("_ancientBanner", BindingFlags.NonPublic | BindingFlags.Instance);
    public static readonly FieldInfo? AncientPortraitField = typeof(NCard).GetField("_ancientPortrait", BindingFlags.NonPublic | BindingFlags.Instance);
    public static readonly FieldInfo? PortraitField = typeof(NCard).GetField("_portrait", BindingFlags.NonPublic | BindingFlags.Instance);
    public static readonly FieldInfo? PortraitBorderField = typeof(NCard).GetField("_portraitBorder", BindingFlags.NonPublic | BindingFlags.Instance);
    public static readonly FieldInfo? FrameField = typeof(NCard).GetField("_frame", BindingFlags.NonPublic | BindingFlags.Instance);
    public static readonly FieldInfo? BannerField = typeof(NCard).GetField("_banner", BindingFlags.NonPublic | BindingFlags.Instance);
    public static readonly FieldInfo? TypePlaqueField = typeof(NCard).GetField("_typePlaque", BindingFlags.NonPublic | BindingFlags.Instance);
    public static readonly FieldInfo? TypeLabelField = typeof(NCard).GetField("_typeLabel", BindingFlags.NonPublic | BindingFlags.Instance);
    // 游戏更新后新增的字段
    public static readonly FieldInfo? PortraitCanvasGroupField = typeof(NCard).GetField("_portraitCanvasGroup", BindingFlags.NonPublic | BindingFlags.Instance);
}

internal static class NCardCustomizationDefaults
{
    // NCard 场景中 TypePlaque 的默认贴图路径
    public const string DefaultTypePlaqueTexturePath = "res://images/ui/cards/card_portrait_border_plaque2.png";
    
    private static Texture2D? _defaultTypePlaqueTexture;
    
    public static Texture2D? GetDefaultTypePlaqueTexture()
    {
        if (_defaultTypePlaqueTexture == null)
        {
            _defaultTypePlaqueTexture = GD.Load<Texture2D>(DefaultTypePlaqueTexturePath);
        }
        return _defaultTypePlaqueTexture;
    }
}

// 补丁类: 处理 NCard 的 Reload 事件，整合所有自定义逻辑
[HarmonyPatch(typeof(NCard), "Reload")]
public class NCardCustomizationPatch
{
    [HarmonyPostfix]
    public static void ReloadPostfix(NCard __instance)
    {
        // 增强的空值检查
        if (__instance == null)
        {
            MainFile.Logger.Debug("[NCardCustomizationPatch] __instance is null, skipping.");
            return;
        }

        var cardModel = __instance.Model;
        if (cardModel == null) 
        {
            MainFile.Logger.Debug("[NCardCustomizationPatch] cardModel is null, skipping.");
            return; // 如果模型为空，直接返回
        }

        // 检查卡牌是否属于 TheresaCardModel 池
        bool isTheresaCard = cardModel is TheresaCardModel;

        // 如果不是 Theresa 卡牌，需要恢复可能被对象池复用污染的状态，然后直接返回
        if (!isTheresaCard)
        {
            RestoreNormalCardAppearance(__instance);
            return;
        }

        // 如果是 Theresa 卡牌，才继续应用所有修改
        var cardType = cardModel.Type;
        var cardRarity = cardModel.Rarity;
        
        MainFile.Logger.Debug($"[NCardCustomizationPatch] Processing: {cardModel.Id.Entry}, Type={cardType}, Rarity={cardRarity}");

        // --- 获取所有节点引用 ---
        var ancientTextBg = NCardReflectionFields.AncientTextBgField?.GetValue(__instance) as TextureRect;
        var ancientBorder = NCardReflectionFields.AncientBorderField?.GetValue(__instance) as TextureRect;
        var ancientBorderGlassOverlay = NCardReflectionFields.AncientBorderGlassOverlayField?.GetValue(__instance) as TextureRect;
        var ancientBanner = NCardReflectionFields.AncientBannerField?.GetValue(__instance) as Control;
        var ancientPortrait = NCardReflectionFields.AncientPortraitField?.GetValue(__instance) as TextureRect;
        
        var portrait = NCardReflectionFields.PortraitField?.GetValue(__instance) as TextureRect;
        var portraitBorder = NCardReflectionFields.PortraitBorderField?.GetValue(__instance) as TextureRect;
        var frame = NCardReflectionFields.FrameField?.GetValue(__instance) as TextureRect;
        var banner = NCardReflectionFields.BannerField?.GetValue(__instance) as TextureRect;
        var portraitCanvasGroup = NCardReflectionFields.PortraitCanvasGroupField?.GetValue(__instance) as CanvasGroup;

        // 检查字段是否存在（某些NCard变体可能没有这些字段）
        if (ancientBorder == null || ancientBanner == null)
        {
            MainFile.Logger.Debug($"[NCardCustomizationPatch] Ancient border/banner fields not found for {cardModel.Id.Entry}, this is expected for some NCard variants.");
        }

        // ===== 关键修复：对 Theresa 卡牌强制显示 Ancient 元素，隐藏普通元素 =====
        // 新版本 Reload() 不再走 Ancient 分支（因为 Rarity 欺骗已禁用），
        // 所以我们需要在 Postfix 中完全接管 Ancient 元素的显示和纹理设置
        
        // 1. 强制显示 Ancient 元素
        if (ancientTextBg != null) ((CanvasItem)ancientTextBg).Visible = true;
        if (ancientBorder != null) ((CanvasItem)ancientBorder).Visible = true;
        // _ancientBorderGlassOverlay 禁用：新版本新增的玻璃覆盖层，使用自定义边框时不需要
        if (ancientBorderGlassOverlay != null) ((CanvasItem)ancientBorderGlassOverlay).Visible = false;
        if (ancientBanner != null) ((CanvasItem)ancientBanner).Visible = true;
        if (ancientPortrait != null) ((CanvasItem)ancientPortrait).Visible = true;

        // 2. 隐藏普通元素
        if (portrait != null) ((CanvasItem)portrait).Visible = false;
        if (portraitBorder != null) ((CanvasItem)portraitBorder).Visible = false;
        if (banner != null) ((CanvasItem)banner).Visible = false;
        if (frame != null) ((CanvasItem)frame).Visible = false;
        if (portraitCanvasGroup != null) ((CanvasItem)portraitCanvasGroup).Visible = true;

        // 3. 设置 Ancient Border 纹理（自定义边框）
        if (ancientBorder != null)
        {
            // 清除原版 material（CanvasItemMaterial_wfyvd 带有 modulate 和 blend 效果，会淡化自定义边框）
            ancientBorder.Material = null;
            // 恢复完全不透明的白色调制，让自定义边框贴图以原色显示
            ancientBorder.Modulate = Colors.White;
            ancientBorder.SelfModulate = Colors.White;
            
            var ancientBorderTexturePath = CardFramePaths.GetAncientBorderTexturePathForTypeAndRarity(cardType, cardRarity);
            var customBorderTexture = LoadTextureFromPath(ancientBorderTexturePath);
            if (customBorderTexture != null)
            {
                ancientBorder.Texture = customBorderTexture;
            }
            else
            {
                MainFile.Logger?.Warn($"[NCardCustomizationPatch] Failed to load ancient border texture: {ancientBorderTexturePath}");
            }
        }

        // 4. 设置 Ancient Banner 纹理
        if (ancientBanner != null)
        {
            var ancientBannerTexturePath = GetAncientBannerTexturePathForType(cardType);
            var customBannerTexture = LoadTextureFromPath(ancientBannerTexturePath);
            if (customBannerTexture != null)
            {
                var bannerTextureRect = FindTextureRectInNode(ancientBanner);
                if (bannerTextureRect != null)
                {
                    bannerTextureRect.Texture = customBannerTexture;
                }
            }
        }

        // 5. 设置 Ancient Portrait 纹理（卡图）
        // 注意：有 CustomSpinePortraitScenePath 的卡牌由 SovereignSpinePortraitPatch 单独处理 Spine 动画卡面，这里跳过
        var theresaCardModel = cardModel as TheresaCardModel;
        if (ancientPortrait != null && string.IsNullOrWhiteSpace(theresaCardModel?.CustomSpinePortraitScenePath))
        {
            var portraitPath = cardModel.PortraitPath;
            if (!string.IsNullOrEmpty(portraitPath))
            {
                var tex = LoadTextureFromPath(portraitPath);
                if (tex != null)
                {
                    ancientPortrait.Texture = tex;
                }
            }
        }

        // 6. 设置 Ancient TextBg 纹理
        if (ancientTextBg != null)
        {
            var ancientTextBgPath = GetAncientTextBgPathForType(cardType);
            var customAncientTextBgTexture = LoadTextureFromPath(ancientTextBgPath);
            if (customAncientTextBgTexture != null)
            {
                ancientTextBg.Texture = customAncientTextBgTexture;
            }
        }

        // 7. 清除 portrait 的 material（防止非 Ancient 分支设置的 blur material 残留）
        if (portrait != null) portrait.Material = null;
        if (ancientPortrait != null) ancientPortrait.Material = null;
        
        // 8. 设置 portraitCanvasGroup 的 material（Visible状态下的Ancient卡牌需要mask material）
        if (portraitCanvasGroup != null)
        {
            // 使用反射获取原版的 _canvasGroupMaskMaterial，或者设为null
            // 由于无法直接访问私有静态字段，我们设为null让Godot使用默认行为
            // 实际上原版Ancient分支会设置 _canvasGroupMaskMaterial，但我们不需要blur效果
            portraitCanvasGroup.Material = null;
        }

        // --- 3. 修改文本颜色和阴影 ---
        var descriptionLabel = __instance.GetNode<MegaRichTextLabel>("CardContainer/DescriptionLabel");
        if (descriptionLabel != null)
        {
            // 对于 Theresa 卡牌，始终应用古老文本样式
            var ancientShadowColor = GetAncientTextColor(); // 例如，定义一个古老阴影颜色
            descriptionLabel.AddThemeColorOverride("font_shadow_color", ancientShadowColor);
        }
        else
        {
            GD.PrintErr("Could not find DescriptionLabel node on NCard instance for text color modification in Reload patch.");
        }

        // --- 4. 修改 TypePlaque 外观 (基于 CardType) ---
        SetTypePlaqueAppearance(__instance, cardType);

        // --- 5. 修改 TypeLabel 内容 (基于 CardType) ---
        SetTypeLabelContent(__instance, cardType);

        // --- 6. 修改 TypePlaque 和 TypeLabel 的位置 ---
        ModifyTypeNodePositions(__instance);
    }

    /// <summary>
    /// 恢复普通卡牌的外观状态，防止对象池复用时 Theresa 卡牌的 Ancient 状态残留污染其他卡牌
    /// </summary>
    private static void RestoreNormalCardAppearance(NCard __instance)
    {
        // 获取所有可能的状态字段
        var ancientTextBg = NCardReflectionFields.AncientTextBgField?.GetValue(__instance) as TextureRect;
        var ancientBorder = NCardReflectionFields.AncientBorderField?.GetValue(__instance) as TextureRect;
        var ancientBorderGlassOverlay = NCardReflectionFields.AncientBorderGlassOverlayField?.GetValue(__instance) as TextureRect;
        var ancientBanner = NCardReflectionFields.AncientBannerField?.GetValue(__instance) as Control;
        var ancientPortrait = NCardReflectionFields.AncientPortraitField?.GetValue(__instance) as TextureRect;
        var portrait = NCardReflectionFields.PortraitField?.GetValue(__instance) as TextureRect;
        var portraitBorder = NCardReflectionFields.PortraitBorderField?.GetValue(__instance) as TextureRect;
        var frame = NCardReflectionFields.FrameField?.GetValue(__instance) as TextureRect;
        var banner = NCardReflectionFields.BannerField?.GetValue(__instance) as TextureRect;
        var typePlaque = NCardReflectionFields.TypePlaqueField?.GetValue(__instance) as NinePatchRect;
        var typeLabel = NCardReflectionFields.TypeLabelField?.GetValue(__instance) as Label;

        // 获取卡牌的稀有度，根据原版逻辑恢复正确的可见性
        var cardModel = __instance.Model;
        if (cardModel == null) return;
        
        bool isAncient = cardModel.Rarity == CardRarity.Ancient;
        
        // 按照原版 Reload 方法的逻辑设置可见性
        if (ancientTextBg != null) ((CanvasItem)ancientTextBg).Visible = isAncient;
        if (ancientBorder != null) ((CanvasItem)ancientBorder).Visible = isAncient;
        if (ancientBorderGlassOverlay != null) ((CanvasItem)ancientBorderGlassOverlay).Visible = isAncient;
        if (ancientBanner != null) ((CanvasItem)ancientBanner).Visible = isAncient;
        if (ancientPortrait != null) ((CanvasItem)ancientPortrait).Visible = isAncient;
        if (portrait != null) ((CanvasItem)portrait).Visible = !isAncient;
        if (portraitBorder != null) ((CanvasItem)portraitBorder).Visible = !isAncient;
        if (frame != null) ((CanvasItem)frame).Visible = !isAncient;
        if (banner != null) ((CanvasItem)banner).Visible = !isAncient;
        
        // 恢复 TypePlaque 和 TypeLabel 为默认状态，让原版 UpdateTypePlaque 处理
        if (typePlaque != null)
        {
            typePlaque.Texture = NCardCustomizationDefaults.GetDefaultTypePlaqueTexture(); // 恢复默认贴图
            typePlaque.Material = cardModel.BannerMaterial; // 恢复原版材质着色
            typePlaque.Modulate = Colors.White;
            typePlaque.SelfModulate = Colors.White;
            // 重置位置和缩放为 card.tscn 中的默认值
            typePlaque.OffsetLeft = -30.5f;
            typePlaque.OffsetTop = 1.0f;
            typePlaque.OffsetRight = 30.5f;
            typePlaque.OffsetBottom = 38.0f;
            typePlaque.Scale = Vector2.One;
        }
        if (typeLabel != null)
        {
            // 重置位置和缩放为 card.tscn 中的默认值
            typeLabel.OffsetLeft = -22.0f;
            typeLabel.OffsetTop = -14.0f;
            typeLabel.OffsetRight = 22.0f;
            typeLabel.OffsetBottom = 14.0f;
            typeLabel.Scale = Vector2.One;
            // 恢复为 card.tscn 中的默认字体颜色（黑色半透明）
            typeLabel.AddThemeColorOverride("font_color", new Color(0.0f, 0.0f, 0.0f, 0.752941f));
        }
        
        // 注意：不手动调用 UpdateTypePlaque，也不修改 _portraitCanvasGroup
        // 原版 Reload 方法会自己处理这些，手动调用在卡牌库等变体中会导致 Godot 内部空引用刷屏
    }

    // 新增方法：修改 TypePlaque 和 TypeLabel 的位置
    // 使用与自定义 card.tscn 中 OffsetLeft/Top/Right/Bottom 完全一致的值，
    // 因为 Godot Anchor 布局下 Position/Size 和 Inspector 的 offset 语义不同
    private static void ModifyTypeNodePositions(NCard nCardInstance)
    {
        // 获取 TypePlaque 节点
        var typePlaque = NCardReflectionFields.TypePlaqueField?.GetValue(nCardInstance) as NinePatchRect;
        if (typePlaque != null)
        {
            typePlaque.OffsetLeft = -133f;
            typePlaque.OffsetTop = -162f;
            typePlaque.OffsetRight = -77f;
            typePlaque.OffsetBottom = -110f;
            typePlaque.Scale = new Vector2(0.7429195f, 0.7200006f);
        }

        // 获取 TypeLabel 节点
        var typeLabel = NCardReflectionFields.TypeLabelField?.GetValue(nCardInstance) as Label;
        if (typeLabel != null)
        {
            typeLabel.OffsetLeft = 24.495605f;
            typeLabel.OffsetTop = -19.055567f;
            typeLabel.OffsetRight = 68.495605f;
            typeLabel.OffsetBottom = 8.944435f;
            typeLabel.Scale = new Vector2(1.3086567f, 1.3875498f);
        }
    }

    // 辅助方法：根据 CardType 设置 TypePlaque 的外观
    private static void SetTypePlaqueAppearance(NCard nCardInstance, CardType cardType)
    {
        var typePlaque = NCardReflectionFields.TypePlaqueField?.GetValue(nCardInstance) as NinePatchRect;
        if (typePlaque != null)
        {
            // 根据卡牌类型选择不同的贴图
            string plaqueTexturePath = GetTypePlaqueTexturePathForType(cardType);
            var plaqueTexture = LoadTextureFromPath(plaqueTexturePath);
            typePlaque.Texture = plaqueTexture;

            // 清除材质，使用原色显示（类似原版无色牌的效果）
            typePlaque.Material = null;
            
            // 使用白色调制，保持贴图原色
            typePlaque.Modulate = Colors.White;
            typePlaque.SelfModulate = Colors.White;
        }
    }

    private static readonly Dictionary<string, Texture2D?> TextureCache = new();

    /// <summary>
    /// 从 res:// 路径加载 Texture2D；如果 Godot 资源导入失败，则回退到直接文件读取
    /// 使用缓存避免重复加载导致闪烁
    /// </summary>
    private static Texture2D? LoadTextureFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // 检查缓存
        if (TextureCache.TryGetValue(path, out var cached))
            return cached;

        // 优先尝试 Godot 标准加载
        var tex = GD.Load<Texture2D>(path);
        if (tex != null)
        {
            TextureCache[path] = tex;
            return tex;
        }

        // 回退：直接读取文件系统创建 ImageTexture
        try
        {
            string filePath = ProjectSettings.GlobalizePath(path);
            if (Godot.FileAccess.FileExists(filePath))
            {
                var image = Image.LoadFromFile(filePath);
                if (image != null)
                {
                    var imgTex = ImageTexture.CreateFromImage(image);
                    TextureCache[path] = imgTex;
                    return imgTex;
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Warn($"[NCardCustomizationPatch] Failed to load texture from path '{path}': {ex.Message}");
        }

        TextureCache[path] = null;
        return null;
    }

    // 辅助方法：根据 CardType 设置 TypeLabel 的内容
    private static void SetTypeLabelContent(NCard nCardInstance, CardType cardType)
    {
        var typeLabel = NCardReflectionFields.TypeLabelField?.GetValue(nCardInstance) as Label;
        if (typeLabel != null)
        {
            typeLabel.Text = GetTypeLabelTextForType(cardType);
            typeLabel.AddThemeColorOverride("font_color", GetTypeLabelColorForType(cardType));
        }
    }

    // 辅助方法：定义古老卡牌的文本颜色
    private static Color GetAncientTextColor()
    {
        // 返回古老卡牌的文本或阴影颜色，例如金色
        return new Color(0.0f, 0.0f, 0.0f, 0.0f); // 示例：金色
    }

    #region Helper Methods - TypePlaque

    /// <summary>
    /// 根据卡牌类型返回对应的 TypePlaque 贴图路径//小图标
    /// </summary>
    private static string GetTypePlaqueTexturePathForType(CardType type)
    {
        switch (type)
        {
            case CardType.Attack:
                return "res://Theresa/images/type_plaque/type_attack.png";
            case CardType.Skill:
                return "res://Theresa/images/type_plaque/type_skill.png";
            case CardType.Power:
                return "res://Theresa/images/type_plaque/type_power.png";
            case CardType.Status:
                return "res://Theresa/images/type_plaque/type_status.png";
            case CardType.Curse:
                return "res://Theresa/images/type_plaque/type_curse.png";
            case CardType.Quest:
                return "res://Theresa/images/type_plaque/type_quest.png";
            case CardType.None:
            default:
                return "res://Theresa/images/type_plaque/type_default.png";
        }
    }

    /// <summary>
    /// 根据卡牌类型返回 TypePlaque 的调制颜色
    /// 使用亮色调制可以增强贴图颜色的鲜艳度（值>1.0会提亮）
    /// </summary>
    private static Color GetTypePlaqueColorForType(CardType type)
    {
        switch (type)
        {
            case CardType.Attack:
                
                return Colors.White;
            case CardType.Skill:

                return new Color(0.9f, 1.2f, 0.9f, 1f);
            case CardType.Power:

                return new Color(0.9f, 0.9f, 1.2f, 1f);
            case CardType.Status:

                return new Color(1.1f, 1.1f, 1.1f, 1f);
            case CardType.Curse:

                return new Color(1.2f, 0.9f, 1.2f, 1f);
            case CardType.Quest:
   
                return new Color(1.2f, 1.1f, 0.8f, 1f);
            case CardType.None:
            default:

                return Colors.White;
        }
    }

    #endregion

    #region Helper Methods - TypeLabel

    /// <summary>
    /// 根据卡牌类型返回 TypeLabel 的文本
    /// </summary>
    private static string GetTypeLabelTextForType(CardType type)
    {
        switch (type)
        {
            case CardType.Attack:
                return "攻击";
            case CardType.Skill:
                return "技能";
            case CardType.Power:
                return "能力";
            case CardType.Status:
                return "状态";
            case CardType.Curse:
                return "诅咒";
            case CardType.Quest:
                return "特殊";
            case CardType.None:
            default:
                return "其他";
        }
    }

    /// <summary>
    /// 根据卡牌类型返回 TypeLabel 的字体颜色
    /// </summary>
    private static Color GetTypeLabelColorForType(CardType type)
    {
        switch (type)
        {
            case CardType.Attack:
                return new Color(0.886f, 0.2f, 0.392f, 0.804f);
            case CardType.Skill:
                return Colors.Cyan;
            case CardType.Power:
                return Colors.LightGreen;
            case CardType.Status:
                return Colors.Gray;
            case CardType.Curse:
                return Colors.Purple;
            case CardType.Quest:
                return Colors.FloralWhite;
            case CardType.None:
            default:
                return Colors.White;
        }
    }

    #endregion

    #region New Helper Methods - Ancient Border & Banner

    /// <summary>
    /// 根据卡牌类型返回对应的 AncientBanner 贴图路径//横幅
    /// </summary>
    private static string GetAncientBannerTexturePathForType(CardType type)
    {
        switch (type)
        {
            case CardType.Attack:
                return "res://Theresa/images/card_frames/banner_attack.png";
            case CardType.Skill:
                return "res://Theresa/images/card_frames/banner_skill.png";
            case CardType.Power:
                return "res://Theresa/images/card_frames/banner_power.png";
            case CardType.Status:
                return "res://Theresa/images/card_frames/banner_status.png";
            case CardType.Curse:
                return "res://Theresa/images/card_frames/banner_curse.png";
            case CardType.Quest:
                return "res://Theresa/images/card_frames/banner_quest.png";
            case CardType.None:
            default:
                return "res://Theresa/images/card_frames/banner_default.png";
        }
    }

    #endregion

    #region Other Helper Methods

    /// <summary>
    /// 根据卡牌类型返回对应的 _ancientTextBg PNG 文件路径
    /// </summary>
    private static string GetAncientTextBgPathForType(CardType type)
    {
        switch (type)
        {
            case CardType.Attack:
                return "res://Theresa/images/card_frames/textbg_attack.png";
            case CardType.Skill:
                return "res://Theresa/images/card_frames/textbg_skill.png";
            case CardType.Power:
                return "res://Theresa/images/card_frames/textbg_power.png";
            case CardType.Status:
                return "res://Theresa/images/card_frames/textbg_status.png";
            case CardType.Curse:
                return "res://Theresa/images/card_frames/textbg_curse.png";
            case CardType.Quest:
                return "res://Theresa/images/card_frames/textbg_quest.png";
            case CardType.None:
            default:
                return "res://Theresa/images/card_frames/textbg_default.png";
        }
    }

    /// <summary>
    /// 根据卡牌稀有度返回自定义的 HSV 颜色值
    /// </summary>
    private static (float h, float s, float v) GetCustomHsvForRarity(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.None:
                return (1.0f, 1.0f, 1.0f);
            case CardRarity.Basic:
                return (1f, 1f, 1f);
            case CardRarity.Common:
                return (1f, 1f, 1f);
            case CardRarity.Uncommon:
                return (1f, 1f, 1f);
            case CardRarity.Rare:
                return (1f, 1f, 1f);
            case CardRarity.Ancient:
                return (1f, 1f, 1f);
            case CardRarity.Event:
                return (1f, 1f, 1f);
            case CardRarity.Token:
                return (1f, 1f, 1f);
            case CardRarity.Status:
                return (1f, 1f, 1f);
            case CardRarity.Curse:
                return (1f, 1f, 1f);
            case CardRarity.Quest:
                return (1f, 1f, 1f);
            default:
                return (1f, 1f, 1f);
        }
    }

    /// <summary>
    /// 递归查找节点中的 TextureRect
    /// </summary>
    private static TextureRect? FindTextureRectInNode(Node node)
    {
        if (node is TextureRect textureRect)
        {
            return textureRect;
        }
        
        foreach (Node child in node.GetChildren())
        {
            var result = FindTextureRectInNode(child);
            if (result != null)
            {
                return result;
            }
        }
        
        return null;
    }

    #endregion
}

/// <summary>
/// 额外的 Patch：UpdateVisuals
/// 确保在卡牌视觉更新时（如抽牌、状态变化）也应用 Ancient 样式
/// 注意：不要调用 Reload，只需要确保可见性设置正确
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class NCardUpdateVisualsPatch
{
    [HarmonyPostfix]
    public static void UpdateVisualsPostfix(NCard __instance)
    {
        if (__instance?.Model is not TheresaCardModel cardModel)
        {
            return;
        }

        // 不要调用 Reload - 这会导致无限循环
        // 只需要确保 Ancient 元素可见，这些设置应该在 Reload 中已经完成
        // 如果需要动态更新，可以在这里添加非累积性的更新
    }
}

/// <summary>
/// 阻止 UpdateTypePlaqueSizeAndPosition 的延迟调用覆盖 Theresa 卡牌的 TypePlaque 位置，
/// 这是导致 TypeLabel/TypePlaque "不自觉漂移" 的根本原因。
/// </summary>
[HarmonyPatch(typeof(NCard), "UpdateTypePlaqueSizeAndPosition")]
public static class NCardUpdateTypePlaqueSizeAndPositionPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NCard __instance)
    {
        if (__instance?.Model is TheresaCardModel)
        {
            // 跳过原始方法，防止把我们设置的固定位置改回去
            return false;
        }
        return true;
    }
}
