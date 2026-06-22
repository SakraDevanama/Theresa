using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Theresa.TheresaCode.Dust;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 回响
/// 回合开始时额外触发一次萦绕
/// </summary>
public sealed class EchoismPower : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 玩家回合开始时触发：额外萦绕 Amount 次
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature != Owner) return;
        if (Amount <= 0) return;

        Flash();

        for (int i = 0; i < Amount; i++)
        {
            // 回合开始处于同步路径，直接同步执行萦绕，避免 enqueue GameAction 导致 checksum 时机不一致
            await DustManager.DustItSync(player, false, false);
        }
    }
}
