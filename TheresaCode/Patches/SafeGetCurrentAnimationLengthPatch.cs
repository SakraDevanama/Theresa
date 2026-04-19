using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 修复 NCreature.GetCurrentAnimationLength 中的 NullReferenceException
/// 当 Spine 轨道的 Animation 为 null 时安全返回 0
/// </summary>
[HarmonyPatch(typeof(NCreature), nameof(NCreature.GetCurrentAnimationLength))]
public static class SafeGetCurrentAnimationLengthPatch
{
    [HarmonyFinalizer]
    public static Exception? Finalizer(NCreature __instance, Exception? __exception, ref float __result)
    {
        if (__exception != null)
        {
            // 捕获任何异常并安全返回 0
            __result = 0f;
            return null;
        }
        return __exception;
    }
}
