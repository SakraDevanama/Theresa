using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Theresa.TheresaCode.Minions.Models;
using Theresa.TheresaCode.Minions.Nodes;

namespace Theresa.TheresaCode.Minions.Patches;

/// <summary>
/// 维什戴尔死亡动画补丁 - 确保安全播放死亡动画
/// </summary>
[HarmonyPatch(typeof(NCreature), "StartDeathAnim")]
public static class WisdelStartDeathAnimPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreature __instance, bool shouldRemove)
    {
        // 检查 NCreature 和 Entity 是否有效
        if (__instance == null) return;
        if (__instance.Entity == null) return;

        // 检查是否是维什戴尔随从（通过 Monster 类型判断）
        var monster = __instance.Entity.Monster;
        if (monster == null) return;

        // 检查是否是 WisdelMinion
        if (monster is not WisdelMinion) return;

        // 检查 Visuals 是否是 Wisdel
        if (__instance.Visuals is not Wisdel wisdelVisuals) return;

        // 安全地播放死亡动画
        try
        {
            wisdelVisuals.PlayDeathAnimation();
        }
        catch
        {
            // 忽略错误，让游戏继续处理死亡流程
        }
    }
}

/// <summary>
/// 修复 AnimDie 中的 NullReferenceException 并添加延迟
/// </summary>
[HarmonyPatch(typeof(NCreature), "AnimDie")]
public static class WisdelAnimDiePatch
{
    [HarmonyPrefix]
    public static bool Prefix(NCreature __instance, bool shouldRemove, CancellationToken cancelToken)
    {
        // 检查是否是维什戴尔随从
        if (__instance?.Visuals is not Wisdel wisdelVisuals)
        {
            // 不是维什戴尔，让原方法执行
            return true;
        }

        // 维什戴尔随从：延迟执行死亡流程，让动画播放完成
        _ = DelayedAnimDie(__instance, shouldRemove, cancelToken, wisdelVisuals);
        
        // 跳过原方法，由我们处理
        return false;
    }

    private static async Task DelayedAnimDie(NCreature nCreature, bool shouldRemove, CancellationToken cancelToken, Wisdel wisdelVisuals)
    {
        try
        {
            // 等待死亡动画播放完成（Die 动画约 1.5 秒）
            await Task.Delay(1500, cancelToken);
            
            // 隐藏 SpineSprite
            wisdelVisuals.HideSpineSprite();
            
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
            // 简化处理：直接移除节点
            if (shouldRemove && nCreature != null && GodotObject.IsInstanceValid(nCreature))
            {
                nCreature.QueueFree();
            }
        }
    }
}
