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
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 强制维什戴尔的邂逅事件在Act2必定出现的Patch
/// 1. 强制Act2第一个 ? 节点变成事件（而不是战斗）
/// 2. 强制Act2第一个事件是维什戴尔事件
/// </summary>
public static class WisdelEncounterForceSpawnPatch
{
    /// <summary>
    /// 标记是否已经在当前Act触发过维什戴尔事件
    /// </summary>
    private static bool _hasWisdelEventBeenTriggered = false;

    /// <summary>
    /// 标记是否已经进入过 ? 节点
    /// </summary>
    private static bool _hasEnteredUnknownPoint = false;

    /// <summary>
    /// 重置状态（新游戏时调用）
    /// </summary>
    public static void ResetState()
    {
        _hasWisdelEventBeenTriggered = false;
        _hasEnteredUnknownPoint = false;
        MainFile.Logger?.Info("[WisdelEncounterForceSpawnPatch] State reset");
    }

    /// <summary>
    /// 根据存档状态同步 static 字段，防止读档后重复触发
    /// </summary>
    private static void SyncStateFromSave(SerializableRun save)
    {
        if (save == null) return;

        var wisdelEvent = ModelDb.AllEvents.OfType<WisdelEncounterEvent>().FirstOrDefault();
        if (wisdelEvent != null)
        {
            _hasWisdelEventBeenTriggered = save.EventsSeen.Contains(wisdelEvent.Id);
        }

        // 检查 Act2 是否已经访问过任何事件房间
        var act2Save = save.Acts.ElementAtOrDefault(1);
        if (act2Save?.SerializableRooms != null)
        {
            _hasEnteredUnknownPoint = act2Save.SerializableRooms.EventsVisited > 0;
        }

        MainFile.Logger?.Info($"[WisdelEncounterForceSpawnPatch] Synced state from save: WisdelTriggered={_hasWisdelEventBeenTriggered}, UnknownPointEntered={_hasEnteredUnknownPoint}");
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
    /// Patch RollRoomTypeFor - 强制Act2第一个 ? 节点变成事件
    /// </summary>
    [HarmonyPatch(typeof(RunManager), "RollRoomTypeFor")]
    public static class RollRoomTypeForPatch
    {
        private static void Postfix(ref RoomType __result, MapPointType pointType)
        {
            // 联机模式下不触发强制事件（避免地图生成分歧）
            if (RunManager.Instance?.NetService?.Type.IsMultiplayer() == true)
                return;

            // 只在 Act2 生效
            var runManager = RunManager.Instance;
            var state = runManager?.DebugOnlyGetState();
            if (state == null || state.CurrentActIndex != 1)
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
                MainFile.Logger?.Info("[WisdelEncounterForceSpawnPatch] First ? node is already Event");
                return;
            }

            // 强制改成事件
            MainFile.Logger?.Info($"[WisdelEncounterForceSpawnPatch] Forced first ? node from {__result} to Event");
            __result = RoomType.Event;
            _hasEnteredUnknownPoint = true;
        }
    }

    /// <summary>
    /// Patch PullNextEvent - 强制Act2第一个事件是维什戴尔事件
    /// </summary>
    [HarmonyPatch(typeof(ActModel), nameof(ActModel.PullNextEvent))]
    public static class PullNextEventPatch
    {
        private static bool Prefix(ActModel __instance, RunState runState, ref EventModel __result)
        {
            // 联机模式下不触发强制事件（避免事件选择分歧）
            if (RunManager.Instance?.NetService?.Type.IsMultiplayer() == true)
                return true;

            // 只限制 Theresa 角色触发
            if (!IsTheresa(runState))
            {
                return true; // 让原始方法执行
            }

            // 只在 Act2 执行
            if (__instance.ActNumber() != 2)
            {
                return true; // 让原始方法执行
            }

            // 如果已经触发过维什戴尔事件，让原始方法执行
            if (_hasWisdelEventBeenTriggered)
            {
                MainFile.Logger?.Info("[WisdelEncounterForceSpawnPatch] Wisdel event already triggered, using normal flow");
                return true;
            }

            // 获取维什戴尔事件
            var wisdelEvent = ModelDb.AllEvents.OfType<WisdelEncounterEvent>().FirstOrDefault();
            if (wisdelEvent == null)
            {
                MainFile.Logger?.Warn("[WisdelEncounterForceSpawnPatch] WisdelEncounterEvent not found!");
                return true; // 让原始方法执行
            }

            // 检查维什戴尔事件是否允许（IsAllowed）
            if (!wisdelEvent.IsAllowed(runState))
            {
                MainFile.Logger?.Warn("[WisdelEncounterForceSpawnPatch] WisdelEncounterEvent is not allowed!");
                return true; // 让原始方法执行
            }

            // 返回维什戴尔事件
            __result = wisdelEvent;
            _hasWisdelEventBeenTriggered = true;
            
            // 标记事件为已访问
            runState.AddVisitedEvent(wisdelEvent);
            
            MainFile.Logger?.Info("[WisdelEncounterForceSpawnPatch] Forced WisdelEncounterEvent as first event");
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
            if (currentActIndex == 1) // Act2
            {
                ResetState();
            }
        }
    }

    /// <summary>
    /// 读档后同步状态，防止重复触发维什戴尔事件
    /// </summary>
    [HarmonyPatch(typeof(RunManager), "InitializeSavedRun")]
    public static class InitializeSavedRunPatch
    {
        private static void Postfix(SerializableRun save)
        {
            if (save.CurrentActIndex == 1)
            {
                SyncStateFromSave(save);
            }
        }
    }
}
