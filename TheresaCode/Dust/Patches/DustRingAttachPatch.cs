using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust.Nodes;

namespace Theresa.TheresaCode.Dust.Patches;

/// <summary>
/// 微尘环附加补丁 - 在玩家 NCreature 初始化时附加 NDustRing
/// </summary>
[HarmonyPatch(typeof(NCreature), "_Ready")]
public static class DustRingAttachPatch
{
    private static bool IsTheresa(NCreature creature) => creature?.Entity?.Player?.Character?.Id?.Entry == Theresa.TheresaCode.Character.Theresa.CharacterId;

    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        if (__instance?.Entity == null) return;
        if (!__instance.Entity.IsPlayer) return;
        if (!IsTheresa(__instance)) return;

        // 检查是否已存在 NDustRing
        foreach (var child in __instance.GetChildren())
        {
            if (child is NDustRing)
                return;
        }

        var dustRing = GD.Load<PackedScene>("res://Theresa/animations/characters/dust_ring/dust_ring.tscn");
        if (dustRing == null) return;

        var instance = dustRing.Instantiate<NDustRing>();
        if (instance == null) return;

        __instance.AddChild(instance);
        instance.Position = new Godot.Vector2(0, -30);
        instance.Initialize(__instance);
        MainFile.Logger?.Info("[DustRingAttachPatch] NDustRing attached to player NCreature.");
    }
}
