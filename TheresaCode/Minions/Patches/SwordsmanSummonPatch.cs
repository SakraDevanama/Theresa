using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Theresa.TheresaCode.Minions.Models;
using Theresa.TheresaCode.Minions.Nodes;

namespace Theresa.TheresaCode.Minions.Patches;

/// <summary>
/// 特雷西斯（Swordsman）召唤入场动画补丁
/// </summary>
[HarmonyPatch]
public static class SwordsmanSummonPatch
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
            if (__instance?.Entity?.Monster is not SwordsmanMinion)
                return;

            if (__instance.Visuals is not Swordsman swordsmanVisuals)
                return;

            // 延迟播放召唤动画，确保节点完全就绪
            swordsmanVisuals.CallDeferred(nameof(Swordsman.PlaySummonAnimation));
        }
    }
}
