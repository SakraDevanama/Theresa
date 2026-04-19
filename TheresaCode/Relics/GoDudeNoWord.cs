using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 戈渎不语 (GoDudeNoWord)
/// 罕见遗物
/// 
/// 效果：
/// 1. 战斗开始时，获得6层多层护甲。
/// 2. 当你失去生命后，获得2层多层护甲。
/// 3. 战斗结束时，每2层多层护甲为你回复1点生命。
/// 
/// Java 原版：
/// - atBattleStart: ApplyPowerAction(PlatedArmorPower, 6)
/// - wasHPLost: ApplyPowerAction(PlatedArmorPower, 2)
/// - onVictory: heal(PlatedArmor amount / 2)
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class GoDudeNoWord : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    /// <summary>
    /// 战斗开始时：获得6层多层护甲
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null) return;

        Flash();
        await PowerCmd.Apply<PlatingPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 6, Owner.Creature, null);
        MainFile.Logger?.Info($"[GoDudeNoWord] Applied 6 Plated Armor at combat start");

        await base.BeforeCombatStart();
    }

    /// <summary>
    /// 失去生命后：获得2层多层护甲
    /// 对应原版 wasHPLost
    /// </summary>
    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        // 只处理拥有者
        if (creature.Player != Owner) return;

        // delta < 0 表示失去生命
        if (delta < 0)
        {
            Flash();
            await PowerCmd.Apply<PlatingPower>(new ThrowingPlayerChoiceContext(), creature, 2, creature, null);
            MainFile.Logger?.Info($"[GoDudeNoWord] HP lost, applied 2 Plated Armor");
        }
    }

    /// <summary>
    /// 战斗胜利后：每2层多层护甲回复1点生命
    /// </summary>
    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner?.Creature == null) return;

        // 获取多层护甲层数
        var platedArmor = Owner.Creature.Powers.FirstOrDefault(p => p is PlatingPower) as PlatingPower;
        if (platedArmor == null) return;

        int amount = (int)(platedArmor.Amount / 2);
        if (amount <= 0) return;

        Flash();
        await CreatureCmd.Heal(Owner.Creature, amount);
        MainFile.Logger?.Info($"[GoDudeNoWord] Combat victory, healed {amount} HP from {platedArmor.Amount} Plated Armor");

        await base.AfterCombatVictory(room);
    }
}
