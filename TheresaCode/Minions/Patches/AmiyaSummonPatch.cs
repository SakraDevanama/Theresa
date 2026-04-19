using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Theresa.Minions.Models;
using Theresa.TheresaCode.Minions.Models;

namespace Theresa.TheresaCode.Minions.Patches;

/// <summary>
/// 阿米娅召唤入场动画补丁
/// </summary>
[HarmonyPatch]
public static class AmiyaSummonPatch
{
    /// <summary>
    /// 在 NCreature 初始化完成后播放召唤动画
    /// </summary>
    [HarmonyPatch(typeof(NCreature), "_Ready")]
    public static class NCreatureReadyPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NCreature __instance)
        {
            if (__instance?.Entity?.Monster is not AmiyaMinion)
                return;

            if (__instance.Visuals is not AmiyaVisuals amiyaVisuals)
                return;

            // 延迟播放召唤动画，确保节点完全就绪
            amiyaVisuals.CallDeferred(nameof(AmiyaVisuals.PlaySummonAnimation));
        }
    }
}
