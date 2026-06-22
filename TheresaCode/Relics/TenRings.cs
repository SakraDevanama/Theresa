using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Dust;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 十戒 (TenRings)
/// Boss 遗物
/// 
/// 效果：
/// 1. 战斗开始时获得 1 层回响。
/// 2. 每经过 2 个回合，增加 1 点微尘上限（上限 10）。
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class TenRings : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    /// <summary>
    /// 战斗开始时：获得1层回响
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null) return;

        Flash();
        await PowerCmd.Apply<EchoismPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, null);
    }

    /// <summary>
    /// 玩家回合开始时：每2回合增加1点微尘上限
    /// 使用 CombatState.Round 计算，确保联机同步
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player == null || player.NetId != Owner.NetId)
            return;

        var combatState = player.Creature?.CombatState;
        if (combatState == null) return;

        // 使用 CombatState.Round 判断触发时机，确保联机下两端一致
        // Round 从 1 开始，第 2、4、6... 回合触发
        if (combatState.RoundNumber >= 2 && combatState.RoundNumber % 2 == 0)
        {
            Flash();
            DustManager.IncreaseMaxDust(1, player);
        }

        await Task.CompletedTask;
    }
}
