using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Theresa.TheresaCode.Patches;

[HarmonyPatch(typeof(PersonalHivePower), nameof(PersonalHivePower.AfterDamageReceived))]
public static class PersonalHivePowerNullFixPatch
{
    [HarmonyPrefix]
    public static bool Prefix(PersonalHivePower __instance, Creature target, DamageResult _, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != __instance.Owner || dealer == null || !props.IsPoweredAttack())
            return true;

        // 只有真正的玩家攻击才触发人体蜂房
        // Minion、Pet、Monster 等不应该触发
        if (dealer.Monster is Osty)
        {
            if (dealer.PetOwner?.Creature == null)
                return false;
            dealer = dealer.PetOwner.Creature;
        }
        else if (dealer.Monster != null)
        {
            // 其他怪物/Minion 不触发
            return false;
        }

        if (dealer.Player == null)
            return false;

        return true;
    }
}