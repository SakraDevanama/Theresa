using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Relics;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// KnownRelic / UnknownRelic Action 计数补丁
/// 
/// Java 原版通过 SpirePatch 拦截 GameActionManager.addToBottom/addToTop 来计数。
/// STS2 中使用 Harmony Patch 拦截 GameAction.Execute 来近似实现。
/// 
/// 注意：这会计数所有 GameAction 的执行，包括网络同步的 Action。
/// </summary>
[HarmonyPatch(typeof(GameAction), nameof(GameAction.Execute))]
public static class KnownRelicActionCounterPatch
{
    [HarmonyPostfix]
    public static void Postfix(GameAction __instance)
    {
        // 获取当前战斗状态
        var combatState = CombatManager.Instance?.DebugOnlyGetState();
        if (combatState == null) return;

        // 获取本地玩家
        var player = LocalContext.GetMe(combatState);
        if (player == null) return;

        // 只处理 Theresa 角色
        if (player.Character?.Id?.Entry != Theresa.TheresaCode.Character.Theresa.CharacterId)
            return;

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
