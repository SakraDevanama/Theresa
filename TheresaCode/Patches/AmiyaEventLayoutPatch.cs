using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using Theresa.TheresaCode.Events;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 修改阿米娅事件的布局，添加自定义背景场景到 Portrait
/// </summary>
[HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.SetEvent))]
public static class AmiyaEventLayoutPatch
{
    private static void Postfix(NEventLayout __instance, EventModel eventModel)
    {
        // 只处理阿米娅事件
        if (eventModel is not AmiyaEncounterEvent)
            return;

        MainFile.Logger?.Info("[AmiyaEventLayoutPatch] Setting up Amiya event background");

        // 加载并实例化自定义场景
        var scene = GD.Load<PackedScene>("res://Theresa/room/amiya3_room/Amiya_event_room.tscn");
        if (scene == null)
        {
            MainFile.Logger?.Error("[AmiyaEventLayoutPatch] Failed to load scene!");
            return;
        }

        var instance = scene.Instantiate<Control>();
        if (instance == null)
        {
            MainFile.Logger?.Error("[AmiyaEventLayoutPatch] Failed to instantiate scene!");
            return;
        }

        // 添加到 Portrait 作为背景
        __instance.AddVfxAnchoredToPortrait(instance);
        MainFile.Logger?.Info("[AmiyaEventLayoutPatch] Added background to portrait");
    }
}
