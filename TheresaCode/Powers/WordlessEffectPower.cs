using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 省略效果
/// 持续1回合
/// 将抽牌变为获得能量
/// </summary>
public sealed class WordlessEffectPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    /// <summary>
    /// 阻止抽牌：将抽牌变为获得能量
    /// </summary>
    public override bool ShouldDraw(Player player, bool fromHandDraw)
    {
        // 只处理拥有者玩家的抽牌
        if (player != Owner?.Player)
            return true;

        MainFile.Logger?.Info($"[WordlessEffect] ShouldDraw: blocking draw, giving 1 energy instead");
        
        // 给予1点能量替代抽牌
        PlayerCmd.GainEnergy(1, player).Wait();
        
        // 返回 false 阻止抽牌
        return false;
    }

    /// <summary>
    /// 回合结束时移除此效果
    /// </summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Owner?.Side != side) return;

        MainFile.Logger?.Info($"[WordlessEffect] AfterTurnEnd: removing power");
        await PowerCmd.Remove(this);
    }
}
