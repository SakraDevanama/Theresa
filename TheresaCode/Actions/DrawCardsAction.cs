using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Theresa;

namespace Theresa.TheresaCode.Actions;

/// <summary>
/// DrawCardsAction：一个自定义游戏动作（GameAction）
/// 用于在战斗中安全地执行"抽牌"操作，支持单机、多人联机、回放和同步
/// </summary>
public sealed class DrawCardsAction : GameAction
{
    // 要抽牌的玩家实例
    private readonly Player _player;

    // 要抽取的卡牌数量
    private readonly uint _count;

    /// <summary>
    /// 构造函数：用于本地创建动作
    /// </summary>
    public DrawCardsAction(Player player, uint count)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _count = count;
    }

    /// <summary>
    /// 构造函数：用于从网络反序列化时重建动作
    /// </summary>
    public DrawCardsAction(Player player, uint count, bool fromNet)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _count = count;
    }

    /// <summary>
    /// 返回该动作的所有者（用于多人游戏中确定执行权限）
    /// </summary>
    public override ulong OwnerId 
    { 
        get 
        { 
            if (_player == null)
            {
                MainFile.Logger?.Error("DrawCardsAction: _player is null!");
                return 0;
            }
            return _player.NetId;
        }
    }

    /// <summary>
    /// 动作类型：Combat（战斗内动作）
    /// </summary>
    public override GameActionType ActionType => GameActionType.Combat;

    /// <summary>
    /// 执行抽牌逻辑的核心方法
    /// </summary>
    protected override async Task ExecuteAction()
    {
        if (_player == null)
        {
            MainFile.Logger?.Error("DrawCardsAction: Cannot execute - player is null");
            return;
        }

        MainFile.Logger?.Info($"DrawCardsAction: Executing for player {_player.NetId}, count={_count}");
        
        PlayerChoiceContext context = new GameActionPlayerChoiceContext(this);
        await CardPileCmd.Draw(context, _count, _player);
        
        MainFile.Logger?.Info($"DrawCardsAction: Completed for player {_player.NetId}");
    }

    /// <summary>
    /// 将本地动作转换为网络动作（用于多人游戏同步）
    /// </summary>
    public override INetAction ToNetAction()
    {
        MainFile.Logger?.Info($"DrawCardsAction: Converting to NetAction for player {_player?.NetId ?? 0}, count={_count}");
        return new NetDrawCardsAction { Count = _count };
    }
}
