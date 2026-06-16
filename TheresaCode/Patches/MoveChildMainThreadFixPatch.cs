using Godot;
using HarmonyLib;
using Theresa;

namespace TheresaCode.Patches;

/// <summary>
/// 修复非主线程调用 Node.MoveChild 导致 Godot 报错的问题。
/// BaseLib 的 HealthBarForecastPatch.EnsureOverlayOrder 会在 CombatStateTracker 的线程池回调里触发 MoveChild，
/// 而 Godot 只允许在主线程中调整 SceneTree 子节点顺序。
/// </summary>
[HarmonyPatch(typeof(Node), "MoveChild")]
public static class MoveChildMainThreadFixPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Node __instance, Node childNode, int toIndex)
    {
        // 保持既有保护：null 子节点直接跳过，避免 NullReferenceException
        if (childNode == null)
            return false;

        // 主线程直接放行
        if (MainFile.MainThreadId == 0 || System.Environment.CurrentManagedThreadId == MainFile.MainThreadId)
            return true;

        // 非主线程时推迟到主线程执行
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(__instance) || !GodotObject.IsInstanceValid(childNode))
                return;

            __instance.MoveChild(childNode, toIndex);
        }).CallDeferred();

        return false;
    }
}
