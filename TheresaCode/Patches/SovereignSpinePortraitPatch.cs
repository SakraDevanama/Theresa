using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Theresa.TheresaCode.Cards;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 为 Theresa 卡牌提供自定义 Spine 动画卡面支持。
/// 使用 SubViewportContainer + SubViewport 在 ancientPortrait 区域内渲染 Spine 场景。
/// 性能优化：只有卡牌被放大（Scale > 1.1）时才持续播放动画，否则只渲染一帧静态图。
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class SovereignSpinePortraitPatch
{
    public const string SpineOverlayNodeName = "SpinePortraitOverlay";
    
    public static readonly FieldInfo? PortraitField = 
        typeof(NCard).GetField("_portrait", BindingFlags.Instance | BindingFlags.NonPublic);
    public static readonly FieldInfo? AncientPortraitField = 
        typeof(NCard).GetField("_ancientPortrait", BindingFlags.Instance | BindingFlags.NonPublic);
    
    private static readonly Dictionary<string, PackedScene> SceneCache = new();
    private static readonly FieldInfo? NCardHolderIsHoveredField = 
        typeof(NCardHolder).GetField("_isHovered", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? NCardHolderIsFocusedField = 
        typeof(NCardHolder).GetField("_isFocused", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? NCardHolderCurrentPressedActionField = 
        typeof(NCardHolder).GetField("_currentPressedAction", BindingFlags.Instance | BindingFlags.NonPublic);
    
    /// <summary>新建 overlay 后预热帧数，确保 Spine 初始化完成前不会只渲染一帧空白</summary>
    private const int WarmUpFrames = 3;

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void ReloadPostfix(NCard __instance)
    {
        if (__instance?.Model is not TheresaCardModel cardModel)
        {
            RemoveSpineOverlay(__instance);
            return;
        }

        var scenePath = cardModel.CustomSpinePortraitScenePath;
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            RemoveSpineOverlay(__instance);
            return;
        }

        // 只有已经进入场景树时才直接应用；否则由 _EnterTree patch 处理
        if (__instance.IsInsideTree())
        {
            ApplySpinePortrait(__instance, scenePath);
        }
    }

    public static void ApplySpinePortrait(NCard cardNode, string scenePath)
    {
        if (!GodotObject.IsInstanceValid(cardNode))
            return;

        // 确保模型仍然匹配（防止对象池复用导致的竞态）
        if (cardNode.Model is not TheresaCardModel currentModel)
            return;
        if (string.IsNullOrWhiteSpace(currentModel.CustomSpinePortraitScenePath) ||
            currentModel.CustomSpinePortraitScenePath != scenePath)
            return;

        if (AncientPortraitField?.GetValue(cardNode) is not TextureRect ancientPortrait)
            return;

        RemoveAllSpineOverlays(ancientPortrait);

        var spineInstance = GetOrCreateSpineInstance(scenePath);
        if (spineInstance == null)
        {
            MainFile.Logger?.Warn($"[SovereignSpinePortraitPatch] Failed to load Spine scene: {scenePath}");
            return;
        }

        var subViewport = spineInstance.GetNodeOrNull<SubViewport>("SubViewport");
        if (subViewport == null)
        {
            MainFile.Logger?.Warn("[SovereignSpinePortraitPatch] SubViewport not found in Spine scene.");
            spineInstance.QueueFree();
            return;
        }

        var parent = subViewport.GetParent();
        if (parent != null)
        {
            parent.RemoveChild(subViewport);
        }

        // 保持 SubViewport 的原始大小，不强制修改，以免破坏 Spine 场景中的布局
        var vpSize = subViewport.Size;
        if (vpSize.X < 1 || vpSize.Y < 1)
        {
            vpSize = new Vector2I(250, 400);
            subViewport.Size = vpSize;
        }

        // 创建 SubViewportContainer 作为 ancientPortrait 的子节点
        var container = new SubViewportContainer
        {
            Name = SpineOverlayNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Stretch = true,
            ZIndex = 0,
            Size = new Vector2(vpSize.X, vpSize.Y)
        };
        
        // 居中显示在 ancientPortrait 区域内
        container.Position = (ancientPortrait.Size - container.Size) / 2.0f;

        container.AddChild(subViewport);
        ancientPortrait.AddChild(container);
        spineInstance.QueueFree();

        // 隐藏原图纹理，让 SubViewportContainer 显示 Spine 内容
        // 注意：Visible 由 NCardCustomizationPatch 控制，这里只处理 Texture 和 overlay
        ancientPortrait.Texture = null;

        // 初始强制 Always，让 Spine 完成初始化；后续由 updater 接管
        subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

        // 添加每帧更新的辅助节点，确保 Scale 和 Holder 悬停状态变化时实时响应
        var updater = new SpineAnimationUpdater();
        updater.Initialize(cardNode, container, subViewport);
        container.AddChild(updater);
    }

    private static Node? GetOrCreateSpineInstance(string scenePath)
    {
        if (!SceneCache.TryGetValue(scenePath, out var scene))
        {
            scene = GD.Load<PackedScene>(scenePath);
            if (scene == null)
            {
                MainFile.Logger?.Warn($"[SovereignSpinePortraitPatch] Failed to load PackedScene: {scenePath}");
                return null;
            }
            SceneCache[scenePath] = scene;
        }

        return scene.Instantiate<Node>();
    }

    private static void RemoveAllSpineOverlays(TextureRect ancientPortrait)
    {
        // 遍历并移除所有 SubViewportContainer 子节点，防止 QueueFree 延迟导致同名重命名残留
        foreach (var child in ancientPortrait.GetChildren())
        {
            if (child is SubViewportContainer && GodotObject.IsInstanceValid(child))
            {
                child.Free();
            }
        }
    }

    public static void RemoveSpineOverlay(NCard? cardNode)
    {
        if (cardNode == null) return;

        if (AncientPortraitField?.GetValue(cardNode) is TextureRect portraitRect)
        {
            RemoveAllSpineOverlays(portraitRect);
        }
    }

    /// <summary>
    /// 根据卡牌当前缩放状态决定是否播放 Spine 动画
    /// </summary>
    public static void UpdateSpineAnimationState(NCard cardNode, SubViewportContainer container, SubViewport subViewport, int framesSinceCreated)
    {
        if (!GodotObject.IsInstanceValid(cardNode) || !GodotObject.IsInstanceValid(container) || !GodotObject.IsInstanceValid(subViewport))
            return;

        // 预热期间强制 Always，防止 Spine 初始化前只渲染一帧空白/ColorRect
        if (framesSinceCreated < WarmUpFrames)
        {
            if (subViewport.RenderTargetUpdateMode != SubViewport.UpdateMode.Always)
                subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
            return;
        }

        bool shouldAnimate = false;
        
        if (cardNode.IsInsideTree())
        {
            // 1. 检测 parent 链中是否有处于悬停/聚焦状态的 NCardHolder
            bool isHolderActive = false;
            bool isInCardPlay = false;
            var current = cardNode.GetParent();
            while (current != null)
            {
                if (current is NCardHolder holder)
                {
                    bool isHovered = (bool?)NCardHolderIsHoveredField?.GetValue(holder) ?? false;
                    bool isFocused = (bool?)NCardHolderIsFocusedField?.GetValue(holder) ?? false;
                    if (isHovered || isFocused)
                    {
                        isHolderActive = true;
                    }
                    
                    // 检测是否处于鼠标按下/拖拽状态
                    var pressedAction = NCardHolderCurrentPressedActionField?.GetValue(holder);
                    if (pressedAction != null)
                    {
                        isInCardPlay = true;
                    }
                    
                    // 检测是否处于快捷键选中/出牌状态：
                    // 如果 NHandCardHolder 的 parent 是 NPlayerHand，且 NPlayerHand 下有 NCardPlay 关联到当前 holder
                    if (holder.GetParent() is NPlayerHand playerHand)
                    {
                        foreach (var child in playerHand.GetChildren())
                        {
                            if (child is NCardPlay cardPlay && cardPlay.Holder == holder)
                            {
                                isInCardPlay = true;
                                break;
                            }
                        }
                    }
                }
                current = current.GetParent();
            }
            
            // 2. 全局显著放大（覆盖选中、百科放大等情况）
            bool isEnlarged = ((Control)cardNode).GetGlobalTransform().Scale.Y > 1.1f;
            
            shouldAnimate = isHolderActive || isEnlarged || isInCardPlay;
        }
        
        var targetMode = shouldAnimate ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Once;
        
        if (subViewport.RenderTargetUpdateMode != targetMode)
        {
            subViewport.RenderTargetUpdateMode = targetMode;
        }
    }
}

/// <summary>
/// 在 NCard 进入场景树时应用 Spine 动画覆盖层
/// （处理 NCard.Create 时 Reload 在进树之前被调用的情况）
/// </summary>
[HarmonyPatch(typeof(NCard), "_EnterTree")]
public static class NCardSpineEnterTreePatch
{
    [HarmonyPostfix]
    public static void EnterTreePostfix(NCard __instance)
    {
        if (__instance?.Model is not TheresaCardModel cardModel)
            return;

        var scenePath = cardModel.CustomSpinePortraitScenePath;
        if (string.IsNullOrWhiteSpace(scenePath))
            return;

        SovereignSpinePortraitPatch.ApplySpinePortrait(__instance, scenePath);
    }
}

/// <summary>
/// 每帧检查 Spine 动画是否应该播放/停止
/// </summary>
public partial class SpineAnimationUpdater : Node
{
    private NCard _card = null!;
    private SubViewportContainer _container = null!;
    private SubViewport _subViewport = null!;

    private int _framesSinceCreated = 0;

    public void Initialize(NCard card, SubViewportContainer container, SubViewport subViewport)
    {
        _card = card;
        _container = container;
        _subViewport = subViewport;
        _framesSinceCreated = 0;
    }

    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_card) || !GodotObject.IsInstanceValid(_container) || !GodotObject.IsInstanceValid(_subViewport))
        {
            QueueFree();
            return;
        }
        SovereignSpinePortraitPatch.UpdateSpineAnimationState(_card, _container, _subViewport, _framesSinceCreated);
        _framesSinceCreated++;
    }
}

/// <summary>
/// 在 UpdateVisuals 时同步更新 Spine 动画状态和容器缩放
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class NCardSpineUpdateVisualsPatch
{
    [HarmonyPostfix]
    public static void UpdateVisualsPostfix(NCard __instance)
    {
        if (__instance?.Model is not TheresaCardModel cardModel)
            return;

        if (string.IsNullOrWhiteSpace(cardModel.CustomSpinePortraitScenePath))
            return;

        if (SovereignSpinePortraitPatch.AncientPortraitField?.GetValue(__instance) is not TextureRect ancientPortrait)
            return;

        var container = ancientPortrait.GetNodeOrNull<SubViewportContainer>(SovereignSpinePortraitPatch.SpineOverlayNodeName);
        if (container == null)
            return;

        var subViewport = container.GetNodeOrNull<SubViewport>("SubViewport");
        if (subViewport == null)
            return;

        SovereignSpinePortraitPatch.UpdateSpineAnimationState(__instance, container, subViewport, int.MaxValue);
    }
}
