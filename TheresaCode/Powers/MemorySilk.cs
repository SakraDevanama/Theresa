using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Theresa.TheresaCode.Powers;

/// <summary>
/// 记忆
/// 回合结束后结算：
/// 移除至多3层众萨卡兹的恨意
/// 若移除数不少于1获得3点敏捷（持续2回合）
/// 不少于2获得荆棘（持续2回合）
/// 不少于3额外再触发一次记忆
/// </summary>
public class MemorySilk : TheresaPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 记录临时效果的剩余回合：层数 -> 剩余回合
    private int _dexterityRemainingTurns = 0;
    private int _thornsRemainingTurns = 0;
    // 记录本回合开始时拥有的层数，用于回合结束扣除
    private int _dexterityToRemove = 0;
    private int _thornsToRemove = 0;

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Side != side) return;

        // 1. 结算临时效果的持续时间
        await HandleTemporaryEffects();

        // 2. 结算恨意移除和奖励
        await ResolveHateRemoval();
    }

    private async Task HandleTemporaryEffects()
    {
        // 扣除本回合开始时需要移除的层数
        if (_dexterityToRemove > 0 && Owner != null)
        {
            await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), Owner, -_dexterityToRemove, Owner, null);
            _dexterityToRemove = 0;
        }
        if (_thornsToRemove > 0 && Owner != null)
        {
            await PowerCmd.Apply<ThornsPower>(new ThrowingPlayerChoiceContext(), Owner, -_thornsToRemove, Owner, null);
            _thornsToRemove = 0;
        }

        // 减少剩余回合，设置下回合要移除的层数
        if (_dexterityRemainingTurns > 0)
        {
            _dexterityRemainingTurns--;
            if (_dexterityRemainingTurns == 0)
                _dexterityToRemove = 3; // 需要移除3层敏捷
        }
        if (_thornsRemainingTurns > 0)
        {
            _thornsRemainingTurns--;
            if (_thornsRemainingTurns == 0)
                _thornsToRemove = 1; // 需要移除1层荆棘
        }
    }

    private async Task ResolveHateRemoval()
    {
        var hatePower = Owner!.Powers.FirstOrDefault(p => p is ZaakathHatePower) as ZaakathHatePower;
        if (hatePower == null) return;

        int removeAmount = Math.Min(hatePower.Amount, 3);
        if (removeAmount <= 0) return;

        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), hatePower, -removeAmount, Owner, null);

        if (removeAmount >= 1)
        {
            await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), Owner, 3, Owner, null);
            _dexterityRemainingTurns = 2; // 持续2回合
        }

        if (removeAmount >= 2)
        {
            await PowerCmd.Apply<ThornsPower>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);
            _thornsRemainingTurns = 2; // 持续2回合
        }

        if (removeAmount >= 3)
            await PowerCmd.Apply<MemorySilk>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);
    }
}
