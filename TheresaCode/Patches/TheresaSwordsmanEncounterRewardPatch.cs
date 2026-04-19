using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Theresa.TheresaCode.Encounters;
using Theresa.TheresaCode.Minions.Cards;

namespace Theresa.TheresaCode.Patches;

/// <summary>
/// 特雷西斯遭遇固定奖励Patch
/// 击败该怪物后固定奖励一张"约定：特雷西斯"牌
/// </summary>
public static class TheresaSwordsmanEncounterRewardPatch
{
    /// <summary>
    /// Patch CombatRoom.StartCombat - 在战斗开始时设置固定奖励
    /// </summary>
    [HarmonyPatch(typeof(CombatRoom), "StartCombat")]
    public static class StartCombatPatch
    {
        private static void Postfix(CombatRoom __instance)
        {
            // 只处理特雷西斯遭遇
            if (__instance.Encounter is not TheresaSwordsmanEncounter)
                return;

            MainFile.Logger?.Info("[TheresaSwordsmanEncounterRewardPatch] Setting up fixed reward for TheresaSwordsman encounter");

            foreach (Player player in __instance.CombatState.Players)
            {
                // 创建一张"约定：特雷西斯"牌
                var swordsmanCard = player.RunState.CreateCard(ModelDb.Card<TheSwordsman>(), player);
                if (swordsmanCard != null)
                {
                    var cardReward = new SpecialCardReward(swordsmanCard, player);
                    cardReward.SetCustomDescriptionEncounterSource(__instance.Encounter.Id);
                    __instance.AddExtraReward(player, cardReward);
                    MainFile.Logger?.Info($"[TheresaSwordsmanEncounterRewardPatch] Added TheSwordsman card reward for player {player.NetId}");
                }
            }
        }
    }
}
