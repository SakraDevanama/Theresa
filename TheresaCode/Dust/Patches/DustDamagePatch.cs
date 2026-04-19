using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Dust.Patches;

/// <summary>
/// 伤害抵挡补丁 - 微尘抵挡玩家受到的伤害
/// </summary>
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage), [typeof(PlayerChoiceContext), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel)])]
public static class DustDamagePatch
{
    private static bool IsTheresa(Creature creature) => creature?.Player?.Character?.Id?.Entry == Theresa.TheresaCode.Character.Theresa.CharacterId;

    [HarmonyPrefix]
    public static void Prefix(PlayerChoiceContext choiceContext, Creature target, ref decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (amount <= 0 || target == null || !target.IsPlayer) return;
        if (!IsTheresa(target)) return;

        int damage = (int)amount;
        int blocked = DustManager.BlockDamage(damage, target);
        amount = blocked;
    }
}
