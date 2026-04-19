using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 碎片大厦的回忆 (TheRecall)
/// Boss 遗物
/// 
/// 效果：
/// 1. 你的希望和恨意提升2点数值上限
/// 2. 在你回合开始后，获得1层希望和恨意中的少数者（如果相等则两者都获得）
/// 
/// Java 原版：
/// - atTurnStartPostDraw: 检查希望/恨意层数，给较少者+1（相等则两者都+1）
/// - 通过 HatePower/HopePower 的 singleUpdate() 中的 TheRecall 检查实现上限+2
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class TheRecall : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    /// <summary>
    /// 玩家回合开始后（抽牌完成后）触发
    /// 获得1层希望和恨意中的少数者
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner != player) return;
        if (player.Creature == null) return;

        var creature = player.Creature;

        // 获取当前希望和恨意的有效层数
        var hopePower = creature.Powers.FirstOrDefault(p => p is TheresiasHopePower) as TheresiasHopePower;
        var hatePower = creature.Powers.FirstOrDefault(p => p is ZaakathHatePower) as ZaakathHatePower;

        int hopeAmount = hopePower?.GetEffectiveAmount() ?? 0;
        int hateAmount = hatePower?.GetEffectiveAmount() ?? 0;

        MainFile.Logger?.Info($"[TheRecall] Turn start - Hope={hopeAmount}, Hate={hateAmount}");

        // 给较少者 +1（如果相等则两者都+1）
        if (hopeAmount <= hateAmount)
        {
            Flash();
            await PowerCmd.Apply<TheresiasHopePower>(choiceContext, new[] { creature }, 1, creature, null);
            MainFile.Logger?.Info($"[TheRecall] Applied +1 Hope (hope={hopeAmount} <= hate={hateAmount})");
        }

        if (hateAmount <= hopeAmount)
        {
            Flash();
            await PowerCmd.Apply<ZaakathHatePower>(choiceContext, new[] { creature }, 1, creature, null);
            MainFile.Logger?.Info($"[TheRecall] Applied +1 Hate (hate={hateAmount} <= hope={hopeAmount})");
        }
    }
}
