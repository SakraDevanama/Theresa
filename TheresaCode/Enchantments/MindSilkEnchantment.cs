using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Enchantments;

/// <summary>
/// 意志丝线（原 MindSilk）
/// 
/// 回合结束时效果：获得 {Amount} 层希望或恨意中较少的一方
/// 
/// 替换关系：可替换茧笼丝线和泪水丝线
/// 
/// 特殊机制：
/// - 被复制时触发效果（有3次缓冲）
/// - 基础数值 1，上限 6（受 SilkPower 影响）
/// </summary>
public class MindSilkEnchantment : AbstractSilkEnchantment
{
    protected override string? CustomIconPath => "res://Theresa/images/icons/silk_thread3.png";

    /// <summary>
    /// 意志丝线的绝对上限（基础）
    /// </summary>
    public const int AbsoluteTopLimit = 6;

    /// <summary>
    /// 当前上限（受 SilkPower 影响）
    /// </summary>
    public int TotalAmount { get; private set; } = AbsoluteTopLimit;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("TotalAmount", TotalAmount)
    ];

    /// <summary>
    /// 被复制时的缓冲次数（原版 paddingRemains）
    /// </summary>
    public static int PaddingRemains { get; set; } = 3;

    /// <summary>
    /// 是否立即触发（用于特殊场景）
    /// </summary>
    public bool AtOnce { get; set; } = false;

    public MindSilkEnchantment()
    {
        BaseAmount = 1;
        TotalAmount = AbsoluteTopLimit;
    }

    /// <summary>
    /// 更新动态变量，确保描述中的 TotalAmount 正确显示
    /// </summary>
    private void UpdateDynamicVars()
    {
        if (DynamicVars.ContainsKey("TotalAmount"))
        {
            DynamicVars["TotalAmount"].BaseValue = TotalAmount;
        }
    }

    /// <summary>
    /// 应用 Power 影响，更新数值和上限
    /// </summary>
    public override void ApplyPowers()
    {
        if (Card?.Owner?.Creature == null)
        {
            Amount = BaseAmount;
            TotalAmount = AbsoluteTopLimit;
            return;
        }

        var silkPower = Card.Owner.Creature.Powers.FirstOrDefault(p => p is SilkPower) as SilkPower;
        if (silkPower != null)
        {
            Amount = BaseAmount + (int)silkPower.Amount;
            TotalAmount = AbsoluteTopLimit + (int)silkPower.Amount;
        }
        else
        {
            Amount = BaseAmount;
            TotalAmount = AbsoluteTopLimit;
        }
        UpdateDynamicVars();
    }

    /// <summary>
    /// 回合结束效果：获得希望或恨意中较少的一方
    /// </summary>
    public override async Task AtTurnEnd(PlayerChoiceContext choiceContext, PileType pileType)
    {
        if (Card?.Owner?.Creature == null) return;
        if (pileType != PileType.Hand && pileType != PileType.None) return;

        var owner = Card.Owner.Creature;

        // 播放卡牌闪光
        PlayCardFlash();

        // 获取希望和恨意的当前层数
        var hopePower = owner.Powers.FirstOrDefault(p => p is TheresiasHopePower) as TheresiasHopePower;
        var hatePower = owner.Powers.FirstOrDefault(p => p is ZaakathHatePower) as ZaakathHatePower;

        int hopeAmount = hopePower != null ? (int)hopePower.Amount : 0;
        int hateAmount = hatePower != null ? (int)hatePower.Amount : 0;

        bool isHate;
        if (hopeAmount < hateAmount)
        {
            isHate = false; // 希望较少，获得希望
        }
        else if (hateAmount < hopeAmount)
        {
            isHate = true; // 恨意较少，获得恨意
        }
        else
        {
            // 相等时随机
            isHate = Card.Owner.RunState.Rng.CombatTargets.NextInt(2) == 0;
        }

        if (isHate)
        {
            await PowerCmd.Apply<ZaakathHatePower>(choiceContext, owner, Amount, owner, Card);
        }
        else
        {
            await PowerCmd.Apply<TheresiasHopePower>(choiceContext, owner, Amount, owner, Card);
        }

        TriggeredOnce();
    }

    /// <summary>
    /// 被复制时触发效果（有缓冲次数限制）
    /// </summary>
    public override void OnCopied()
    {
        if (PaddingRemains <= 0) return;
        if (Card?.Owner?.Creature == null) return;

        PaddingRemains--;

        // 触发意志效果
        _ = TriggerMindSilkEffect();
    }

    private async Task TriggerMindSilkEffect()
    {
        if (Card?.Owner?.Creature == null) return;

        var owner = Card.Owner.Creature;

        // 确保玩家有 MindPower（用于显示剩余缓冲次数）
        var mindPower = owner.Powers.FirstOrDefault(p => p is MindPower) as MindPower;
        if (mindPower == null)
        {
            await PowerCmd.Apply<MindPower>(new ThrowingPlayerChoiceContext(), owner, 1, owner, Card);
        }

        // 触发回合结束效果
        // 注意：这里使用 AtOnce 控制触发时机
        // 实际效果由 SilkSpreadPower 统一调度
    }

    /// <summary>
    /// 意志可以替换茧笼和泪水
    /// </summary>
    public override bool CanReplace(AbstractSilkEnchantment silkToReplace)
    {
        // 如果有 IreShyWord 遗物，不能替换
        // TODO: 检查 IreShyWord 遗物
        return silkToReplace is CocoonSilkEnchantment or TearSilkEnchantment;
    }

    /// <summary>
    /// 重置缓冲次数
    /// </summary>
    public static void ResetPaddingRemains()
    {
        PaddingRemains = 3;
    }

    private static void PlayCardFlash()
    {
        // 闪光动画由 SilkSpreadPower 统一处理
    }
}
