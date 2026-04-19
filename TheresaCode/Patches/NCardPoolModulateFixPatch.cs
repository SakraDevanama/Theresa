using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 修复NCard对象池复用时Modulate可能保持透明状态的问题。
/// 
/// 问题根源：NCard.OnReturnedFromPool() 只在 IsNodeReady() 为true时重置Modulate。
/// 但 NodePool.Get() 调用 OnReturnedFromPool() 时，NCard 还未被添加到场景树中，
/// 所以 IsNodeReady() 返回 false，Modulate 不会被重置。
/// 
/// 如果NCard在上次使用时Modulate被设为透明（如Tween动画、消散效果等），
/// 当它被对象池复用时，会保持透明状态，导致卡牌显示为透明。
/// 
/// 修复方案：
/// 1. 在 OnReturnedFromPool 执行后，强制重置 Modulate 和 SelfModulate 为 Colors.White
/// 2. 在 NCard 进入场景树时（_EnterTree），再次重置 Modulate 和 SelfModulate
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.OnReturnedFromPool))]
public static class NCardPoolModulateFixPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCard __instance)
    {
        // 强制重置Modulate，不管IsNodeReady的状态
        if (GodotObject.IsInstanceValid(__instance))
        {
            __instance.Modulate = Colors.White;
            __instance.SelfModulate = Colors.White;
        }
    }
}

/// <summary>
/// 在NCard进入场景树时重置Modulate，作为额外的保护措施。
/// 这可以覆盖任何在对象池中可能发生的延迟Tween修改。
/// </summary>
[HarmonyPatch(typeof(NCard), "_EnterTree")]
public static class NCardEnterTreeModulateFixPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCard __instance)
    {
        if (GodotObject.IsInstanceValid(__instance))
        {
            __instance.Modulate = Colors.White;
            __instance.SelfModulate = Colors.White;
        }
    }
}
