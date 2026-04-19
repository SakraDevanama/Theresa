using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 小方块 (LittleCube)
/// Boss 遗物
/// 
/// 效果：你每自动打出3张牌，分别执行：
/// 1. 获得5点格挡
/// 2. 抽1张牌
/// 3. 使随机手牌本回合耗能-1
/// 
/// Java 原版：
/// - counter 计数器记录自动打出次数
/// - onTrigger: counter++，根据 counter 值触发不同效果
/// - counter 1: GainBlockAction(5)
/// - counter 2: DrawCardAction(1)
/// - counter >=3: 随机手牌费用-1，counter 重置为0
/// - TriggerPatch.OnUseCardActionPatch 中检测 card.isInAutoplay 调用 onTrigger
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class LittleCube : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShowCounter => true;

    // 当前计数（1=格挡, 2=抽牌, 3=降费）
    private int _counter;

    public override int DisplayAmount => _counter;

    /// <summary>
    /// 战斗开始时重置计数器
    /// </summary>
    public override Task BeforeCombatStart()
    {
        _counter = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 卡牌打出后触发
    /// 检测是否为自动打出，是则触发计数器效果
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;
        if (!cardPlay.IsAutoPlay) return;

        _counter++;
        InvokeDisplayAmountChanged();

        MainFile.Logger?.Info($"[LittleCube] AutoPlay triggered, counter={_counter}");

        switch (_counter)
        {
            case 1:
                // 获得5点格挡
                Flash();
                await CreatureCmd.GainBlock(Owner.Creature, 5, ValueProp.Move, null);
                MainFile.Logger?.Info($"[LittleCube] Effect 1: Gained 5 block");
                break;

            case 2:
                // 抽1张牌
                Flash();
                await CardPileCmd.Draw(choiceContext, 1, Owner);
                MainFile.Logger?.Info($"[LittleCube] Effect 2: Drew 1 card");
                break;

            case >= 3:
                // 随机手牌本回合费用-1，然后重置计数器
                Flash();
                await ReduceRandomHandCardCost();
                _counter = 0;
                InvokeDisplayAmountChanged();
                MainFile.Logger?.Info($"[LittleCube] Effect 3: Reduced random hand card cost, counter reset");
                break;
        }
    }

    /// <summary>
    /// 随机手牌本回合费用-1
    /// 对应原版：筛选 cost > 0 且 costForTurn > 0 且不是 freeToPlayOnce 的卡牌
    /// </summary>
    private async Task ReduceRandomHandCardCost()
    {
        if (Owner == null) return;

        var handPile = PileType.Hand.GetPile(Owner);
        if (handPile == null) return;

        // 筛选有费用且当前回合费用 > 0 的卡牌
        var validCards = handPile.Cards
            .Where(c => c.EnergyCost.GetResolved() > 0)
            .ToList();

        if (validCards.Count == 0)
        {
            MainFile.Logger?.Info($"[LittleCube] No valid cards to reduce cost");
            return;
        }

        // 随机选择一张卡牌
        var targetCard = validCards[Owner.RunState.Rng.CombatTargets.NextInt(validCards.Count)];

        // 本回合费用-1
        targetCard.EnergyCost.AddThisTurnOrUntilPlayed(-1, reduceOnly: true);

        MainFile.Logger?.Info($"[LittleCube] Reduced cost of {targetCard.Id.Entry} by 1 for this turn");

        await Task.CompletedTask;
    }

    /// <summary>
    /// 战斗胜利后重置计数器
    /// </summary>
    public override Task AfterCombatVictory(CombatRoom room)
    {
        _counter = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
