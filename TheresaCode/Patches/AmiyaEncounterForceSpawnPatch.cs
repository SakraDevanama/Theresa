using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Map;
using Theresa.TheresaCode.Events;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 强制阿米娅的邂逅事件在Act1必定出现的Patch
/// 1. 强制第一个 ? 节点变成事件（而不是战斗）
/// 2. 强制第一个事件是阿米娅事件
/// </summary>
public static class AmiyaEncounterForceSpawnPatch
{
    /// <summary>
    /// 标记是否已经在当前Act触发过阿米娅事件
    /// </summary>
    private static bool _hasAmiyaEventBeenTriggered = false;

    /// <summary>
    /// 标记是否已经进入过 ? 节点
    /// </summary>
    private static bool _hasEnteredUnknownPoint = false;

    /// <summary>
    /// 重置状态（新游戏时调用）
    /// </summary>
    public static void ResetState()
    {
        _hasAmiyaEventBeenTriggered = false;
        _hasEnteredUnknownPoint = false;
        MainFile.Logger?.Info("[AmiyaEncounterForceSpawnPatch] State reset");
    }

    /// <summary>
    /// 根据存档状态同步 static 字段，防止读档后重复触发
    /// </summary>
    private static void SyncStateFromSave(SerializableRun save)
    {
        if (save == null) return;

        var amiyaEvent = ModelDb.AllEvents.OfType<AmiyaEncounterEvent>().FirstOrDefault();
        if (amiyaEvent != null)
        {
            _hasAmiyaEventBeenTriggered = save.EventsSeen.Contains(amiyaEvent.Id);
        }

        // 检查 Act1 是否已经访问过任何事件房间（通过存档的 SerializableRoomSet.EventsVisited）
        var act1Save = save.Acts.ElementAtOrDefault(0);
        if (act1Save?.SerializableRooms != null)
        {
            _hasEnteredUnknownPoint = act1Save.SerializableRooms.EventsVisited > 0;
        }

        MainFile.Logger?.Info($"[AmiyaEncounterForceSpawnPatch] Synced state from save: AmiyaTriggered={_hasAmiyaEventBeenTriggered}, UnknownPointEntered={_hasEnteredUnknownPoint}");
    }

    /// <summary>
    /// 检查当前角色是否为 Theresa
    /// </summary>
    private static bool IsTheresa(RunState runState)
    {
        var player = runState.Players.FirstOrDefault();
        return player != null && player.Character.Id.Entry == Theresa.TheresaCode.Character.Theresa.CharacterId;
    }

    /// <summary>
    /// Patch RollRoomTypeFor - 强制第一个 ? 节点变成事件
    /// </summary>
    [HarmonyPatch(typeof(RunManager), "RollRoomTypeFor")]
    public static class RollRoomTypeForPatch
    {
        private static void Postfix(ref RoomType __result, MapPointType pointType)
        {
            // 只在 Act1 生效
            var runManager = RunManager.Instance;
            var state = runManager?.DebugOnlyGetState();
            if (state == null || state.CurrentActIndex != 0)
                return;

            // 只限制 Theresa 角色触发
            if (!IsTheresa(state))
                return;

            // 只在第一个 ? 节点生效
            if (_hasEnteredUnknownPoint)
                return;

            // 检查是否是 ? 节点
            if (pointType != MapPointType.Unknown)
                return;

            // 如果结果是事件，标记已进入 ? 节点
            if (__result == RoomType.Event)
            {
                _hasEnteredUnknownPoint = true;
                MainFile.Logger?.Info("[AmiyaEncounterForceSpawnPatch] First ? node is already Event");
                return;
            }

            // 强制改成事件
            MainFile.Logger?.Info($"[AmiyaEncounterForceSpawnPatch] Forced first ? node from {__result} to Event");
            __result = RoomType.Event;
            _hasEnteredUnknownPoint = true;
        }
    }

    /// <summary>
    /// Patch PullNextEvent - 强制第一个事件是阿米娅事件
    /// </summary>
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.PullNextEvent))]
    public static class PullNextEventPatch
    {
        private static bool Prefix(ActModel __instance, RunState runState, ref EventModel __result)
        {
            // 只限制 Theresa 角色触发
            if (!IsTheresa(runState))
            {
                return true; // 让原始方法执行
            }

            // 只在 Act1 执行
            if (__instance.ActNumber() != 1)
            {
                return true; // 让原始方法执行
            }

            // 如果已经触发过阿米娅事件，让原始方法执行
            if (_hasAmiyaEventBeenTriggered)
            {
                MainFile.Logger?.Info("[AmiyaEncounterForceSpawnPatch] Amiya event already triggered, using normal flow");
                return true;
            }

            // 获取阿米娅事件
            var amiyaEvent = ModelDb.AllEvents.OfType<AmiyaEncounterEvent>().FirstOrDefault();
            if (amiyaEvent == null)
            {
                MainFile.Logger?.Warn("[AmiyaEncounterForceSpawnPatch] AmiyaEncounterEvent not found!");
                return true; // 让原始方法执行
            }

            // 检查阿米娅事件是否允许（IsAllowed）
            if (!amiyaEvent.IsAllowed(runState))
            {
                MainFile.Logger?.Warn("[AmiyaEncounterForceSpawnPatch] AmiyaEncounterEvent is not allowed!");
                return true; // 让原始方法执行
            }

            // 返回阿米娅事件
            __result = amiyaEvent;
            _hasAmiyaEventBeenTriggered = true;
            
            // 标记事件为已访问
            runState.AddVisitedEvent(amiyaEvent);
            
            MainFile.Logger?.Info("[AmiyaEncounterForceSpawnPatch] Forced AmiyaEncounterEvent as first event");
            return false; // 跳过原始方法
        }
    }

    /// <summary>
    /// 当进入新的Act时，重置状态
    /// </summary>
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterAct))]
    public static class EnterActPatch
    {
        private static void Prefix(int currentActIndex)
        {
            if (currentActIndex == 0) // Act1
            {
                ResetState();
            }
        }
    }

    /// <summary>
    /// 读档后同步状态，防止重复触发阿米娅事件
    /// </summary>
    [HarmonyPatch(typeof(RunManager), "InitializeSavedRun")]
    public static class InitializeSavedRunPatch
    {
        private static void Postfix(SerializableRun save)
        {
            if (save.CurrentActIndex == 0)
            {
                SyncStateFromSave(save);
            }
        }
    }
}
