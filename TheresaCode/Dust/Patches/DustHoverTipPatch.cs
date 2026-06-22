using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Dust.Patches;

/// <summary>
/// 为玩家悬停提示添加微尘上限信息
/// </summary>
[HarmonyPatch(typeof(Creature), nameof(Creature.HoverTips), MethodType.Getter)]
public static class DustHoverTipPatch
{
    private static bool IsTheresa(Creature creature) => creature?.Player?.Character?.Id?.Entry == Theresa.TheresaCode.Character.Theresa.CharacterId;

    [HarmonyPostfix]
    public static void Postfix(Creature __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (__instance == null || !__instance.IsPlayer) return;
        if (!IsTheresa(__instance)) return;
        var player = __instance.Player;
        if (player == null) return;
        if (DustManager.CardsFor(player).Count == 0 && DustManager.MaxDust(player) <= 0) return;

        var list = __result.ToList();
        var title = new LocString("static_hover_tips", "THERESA-DUSTLIMIT.title");
        var desc = new LocString("static_hover_tips", "THERESA-DUSTLIMIT.description");
        desc.AddObj("amount", DustManager.MaxDust(player));
        list.Insert(0, new HoverTip(title, desc));
        __result = list;
    }
}
