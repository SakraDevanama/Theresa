using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Relics;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// KnownRelic / UnknownRelic Action 计数补丁
///
/// - 本地模式：拦截 GameAction.Execute，为所有 Theresa 玩家精确计数。
/// - 联机模式：跳过 Action 级计数，改用回合开始/结束固定 +20 的近似方案（在遗物类中实现），
///   避免联机时各端本地玩家不一致导致状态分歧。
/// </summary>
[HarmonyPatch(typeof(GameAction), nameof(GameAction.Execute))]
public static class KnownRelicActionCounterPatch
{
    [HarmonyPostfix]
    public static void Postfix(GameAction __instance)
    {
        // 联机模式下关闭 Action 级计数，改用回合开始/结束固定 +20
        if (RunManager.Instance?.NetService?.Type.IsMultiplayer() == true)
            return;

        // 获取当前战斗状态
        var combatState = CombatManager.Instance?.DebugOnlyGetState();
        if (combatState == null) return;

        // 遍历所有玩家，为每个 Theresa 角色的遗物计数
        foreach (var player in combatState.Players)
        {
            if (player.Character?.Id?.Entry != Theresa.TheresaCode.Character.Theresa.CharacterId)
                continue;

            // 查找 UnknownRelic 或 KnownRelic
            var unknownRelic = player.Relics.FirstOrDefault(r => r is UnknownRelic) as UnknownRelic;
            var knownRelic = player.Relics.FirstOrDefault(r => r is KnownRelic) as KnownRelic;

            // 优先计数到 UnknownRelic（如果存在）
            if (unknownRelic != null)
            {
                unknownRelic.IncrementActionCount();
            }
            // 否则计数到 KnownRelic
            else if (knownRelic != null)
            {
                knownRelic.IncrementActionCount();
            }
        }
    }
}
