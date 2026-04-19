using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Theresa.Minions.Models;
using Theresa.TheresaCode.Minions.Models;

namespace Theresa.TheresaCode.Minions.Patches;

/// <summary>
/// 阿米娅死亡动画补丁 - 确保安全播放死亡动画
/// </summary>
[HarmonyPatch(typeof(NCreature), "StartDeathAnim")]
public static class AmiyaStartDeathAnimPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance, bool shouldRemove)
    {
        // 检查 NCreature 和 Entity 是否有效
        if (__instance == null) return;
        if (__instance.Entity == null) return;

        // 检查是否是阿米娅随从（通过 Monster 类型判断）
        var monster = __instance.Entity.Monster;
        if (monster == null) return;

        // 检查是否是 AmiyaMinion
        if (monster is not AmiyaMinion amiyaMinion) return;

        // 检查 Visuals 是否是 AmiyaVisuals
        if (__instance.Visuals is not AmiyaVisuals amiyaVisuals) return;

        // 安全地播放死亡动画
        try
        {
            amiyaVisuals.PlayDeathAnimation();
        }
        catch
        {
            // 忽略错误，让游戏继续处理死亡流程
        }

        // 播放死亡音效
        try
        {
            amiyaMinion.PlayDeathSound();
        }
        catch
        {
            // 忽略音效错误
        }
    }
}

/// <summary>
/// 修复 AnimDie 中的 NullReferenceException 并添加延迟
/// </summary>
[HarmonyPatch(typeof(NCreature), "AnimDie")]
public static class AmiyaAnimDiePatch
{
    [HarmonyPrefix]
    public static bool Prefix(NCreature __instance, bool shouldRemove, CancellationToken cancelToken)
    {
        // 检查是否是阿米娅随从
        if (__instance?.Visuals is not AmiyaVisuals amiyaVisuals)
        {
            // 不是阿米娅，让原方法执行
            return true;
        }

        // 阿米娅随从：延迟执行死亡流程，让动画播放完成
        _ = DelayedAnimDie(__instance, shouldRemove, cancelToken, amiyaVisuals);
        
        // 跳过原方法，由我们处理
        return false;
    }

    private static async Task DelayedAnimDie(NCreature nCreature, bool shouldRemove, CancellationToken cancelToken, AmiyaVisuals amiyaVisuals)
    {
        try
        {
            // 等待死亡动画播放完成（die 动画约 1.5 秒）
            await Task.Delay(1500, cancelToken);
            
            // 隐藏 SpineSprite
            amiyaVisuals.HideSpineSprite();
            
            // 再等待一小段时间确保隐藏完成
            await Task.Delay(100, cancelToken);
            
            // 调用原始的死亡处理
            if (nCreature != null && GodotObject.IsInstanceValid(nCreature))
            {
                // 手动执行原 AnimDie 的逻辑
                await OriginalAnimDie(nCreature, shouldRemove, cancelToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 取消操作，忽略
        }
        catch
        {
            // 其他错误，尝试继续执行原始逻辑
            try
            {
                await OriginalAnimDie(nCreature, shouldRemove, cancelToken);
            }
            catch { }
        }
    }

    /// <summary>
    /// 执行原始的 AnimDie 逻辑（简化版）
    /// </summary>
    private static async Task OriginalAnimDie(NCreature nCreature, bool shouldRemove, CancellationToken cancelToken)
    {
        // 调用原方法使用反射
        var method = typeof(NCreature).GetMethod("AnimDie", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (method != null)
        {
            // 创建一个不带补丁的调用来避免递归
            // 这里我们直接调用基类实现或手动处理
            
            // 简化处理：直接移除节点
            if (shouldRemove && nCreature != null && GodotObject.IsInstanceValid(nCreature))
            {
                nCreature.QueueFree();
            }
        }
    }
}

/// <summary>
/// 修复 MoveChild 中的 NullReferenceException
/// </summary>
[HarmonyPatch(typeof(Node), "MoveChild")]
public static class NodeMoveChildPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Node __instance, Node childNode, int toIndex)
    {
        // 如果 childNode 为 null，跳过 MoveChild 调用
        if (childNode == null)
        {
            return false;
        }
        return true;
    }
}
