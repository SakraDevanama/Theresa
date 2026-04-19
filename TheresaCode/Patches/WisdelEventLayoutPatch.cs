using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using Theresa.TheresaCode.Events;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 修改维什戴尔事件的布局，添加自定义背景场景到 Portrait
/// </summary>
[HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.SetEvent))]
public static class WisdelEventLayoutPatch
{
    private static void Postfix(NEventLayout __instance, EventModel eventModel)
    {
        // 只处理维什戴尔事件
        if (eventModel is not WisdelEncounterEvent)
            return;

        MainFile.Logger?.Info("[WisdelEventLayoutPatch] Setting up Wisdel event background");

        // 加载并实例化自定义场景
        var scene = GD.Load<PackedScene>("res://Theresa/room/wisdel_room/wisdel_event_room.tscn");
        if (scene == null)
        {
            MainFile.Logger?.Error("[WisdelEventLayoutPatch] Failed to load scene!");
            return;
        }

        var instance = scene.Instantiate<Control>();
        if (instance == null)
        {
            MainFile.Logger?.Error("[WisdelEventLayoutPatch] Failed to instantiate scene!");
            return;
        }

        // 添加到 Portrait 作为背景
        __instance.AddVfxAnchoredToPortrait(instance);
        MainFile.Logger?.Info("[WisdelEventLayoutPatch] Added background to portrait");
    }
}
