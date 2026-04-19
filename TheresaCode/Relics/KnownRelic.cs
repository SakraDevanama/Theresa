using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 文明的存续
/// Boss 遗物（由 UnknownRelic 升级而来）
/// 
/// Java 原版逻辑：
/// - 计数器记录战斗中 GameAction 的数量
/// - 2k: 战斗开始时 +1力量 +1敏捷
/// - 4k: 额外 +1能量 +1抽牌
/// - 6k: 额外随机能力牌到手牌，耗能为0
/// - 8k: 额外 +5最大生命（战斗结束后恢复）
/// - 10k: 受到致命伤害时满血复活（仅一次）
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class KnownRelic : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    // 显示计数器
    public override bool ShowCounter => true;

    // 显示 Action 计数
    public override int DisplayAmount => ActionCount;

    // DynamicVar 键名常量
    private const string ActionCountKey = "ActionCount";

    // 保存属性：Action 计数（核心数据，会被保存到存档）
    [SavedProperty]
    private int ActionCount { get; set; }

    // 保存属性：是否已触发过复活效果
    [SavedProperty]
    private bool HasTriggeredRevive { get; set; }

    // 当前战斗是否已增加过最大生命
    private bool AddedMaxHpThisCombat { get; set; }

    // 当前战斗是否已触发过能量抽牌效果
    private bool HasTriggeredEnergyDrawThisCombat { get; set; }

    // 当前战斗是否已触发过免费能力牌效果
    private bool HasTriggeredFreePowerThisCombat { get; set; }

    /// <summary>
    /// 定义动态变量，用于本地化显示 Action 计数
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar(ActionCountKey, 0m)
    };

    /// <summary>
    /// 同步计数到 DynamicVar（用于本地化显示）
    /// </summary>
    private void SyncActionCountToDynamicVar()
    {
        base.DynamicVars[ActionCountKey].BaseValue = ActionCount;
        InvokeDisplayAmountChanged();
    }

    /// <summary>
    /// 设置初始 Action 计数（用于从 UnknownRelic 升级时转移计数）
    /// </summary>
    public void SetInitialActionCount(int count)
    {
        ActionCount = count;
        SyncActionCountToDynamicVar();
        MainFile.Logger?.Info($"KnownRelic: Initial action count set to {count} (transferred from UnknownRelic)");
    }

    /// <summary>
    /// 增加 Action 计数（由补丁调用）
    /// </summary>
    public void IncrementActionCount()
    {
        ActionCount++;
        SyncActionCountToDynamicVar();
    }

    /// <summary>
    /// 获得遗物时初始化
    /// </summary>
    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        SyncActionCountToDynamicVar();
        MainFile.Logger?.Info($"KnownRelic: Obtained! Loaded count = {ActionCount}");
    }

    /// <summary>
    /// 进入房间时同步（用于存档加载后）
    /// </summary>
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        SyncActionCountToDynamicVar();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 战斗开始时调用 - 根据 Action 计数触发效果
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        if (Owner == null)
            return;

        // 重置战斗状态
        AddedMaxHpThisCombat = false;
        HasTriggeredEnergyDrawThisCombat = false;
        HasTriggeredFreePowerThisCombat = false;

        var count = ActionCount;

        // 2k: 获得1点力量和敏捷
        if (count >= 2000)
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, null);
            await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, null);
            MainFile.Logger?.Info($"KnownRelic: Applied 2k effect (Strength + Dexterity), count={count}");

            // 4k: 额外获得1点能量并抽1张牌
            if (count >= 4000 && !HasTriggeredEnergyDrawThisCombat)
            {
                HasTriggeredEnergyDrawThisCombat = true;
                await PlayerCmd.GainEnergy(1, Owner);
                await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1, Owner);
                Flash();
                MainFile.Logger?.Info($"KnownRelic: Applied 4k effect (Energy + Draw), count={count}");
            }

            // 6k: 额外随机能力牌到手牌，耗能为0
            if (count >= 6000 && !HasTriggeredFreePowerThisCombat)
            {
                HasTriggeredFreePowerThisCombat = true;
                var powerCards = Owner.Deck.Cards
                    .Where(c => c.Type == CardType.Power)
                    .Select(c => c.CanonicalInstance)
                    .Distinct()
                    .ToList();

                if (powerCards.Count > 0)
                {
                    var random = new Random();
                    var selectedCard = powerCards[random.Next(powerCards.Count)];
                    var combatState = Owner.Creature.CombatState;
                    if (combatState != null)
                    {
                        var cardInCombat = combatState.CreateCard(selectedCard, Owner);
                        cardInCombat.SetToFreeThisTurn();
                        await CardPileCmd.Add(cardInCombat, PileType.Hand);
                        Flash();
                        MainFile.Logger?.Info($"KnownRelic: Applied 6k effect (Free Power card: {cardInCombat.Title}), count={count}");
                    }
                }
            }

            // 8k: 获得5点最大生命
            if (count >= 8000 && !AddedMaxHpThisCombat)
            {
                AddedMaxHpThisCombat = true;
                await CreatureCmd.GainMaxHp(Owner.Creature, 5);
                Flash();
                MainFile.Logger?.Info($"KnownRelic: Applied 8k effect (+5 Max HP), count={count}");
            }
        }

        await base.BeforeCombatStart();
    }

    /// <summary>
    /// 战斗胜利后重置状态
    /// </summary>
    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);

        // 8k 效果：战斗结束后恢复最大生命
        if (AddedMaxHpThisCombat)
        {
            AddedMaxHpThisCombat = false;
            // 减少5点最大生命（恢复原有上限）
            // 注意：如果当前生命大于新的最大生命，需要调整
            await CreatureCmd.LoseMaxHp(new BlockingPlayerChoiceContext(), Owner!.Creature, 5, false);
            MainFile.Logger?.Info($"KnownRelic: Reverted 8k effect (-5 Max HP)");
        }

        HasTriggeredEnergyDrawThisCombat = false;
        HasTriggeredFreePowerThisCombat = false;
    }

    /// <summary>
    /// 生命值变化时调用 - 处理10层效果（致命伤害后回复至100%生命）
    /// </summary>
    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        var count = ActionCount;

        // 10k 效果：仅一次地可以在致命伤害后回复至100%生命
        if (count < 10000 || HasTriggeredRevive)
            return;
        if (creature.Player != Owner)
            return;

        // 检查是否受到致命伤害（生命值降到0或以下）
        if (creature.CurrentHp <= 0)
        {
            HasTriggeredRevive = true;
            Flash();

            // 回复至100%生命
            var healAmount = creature.MaxHp - creature.CurrentHp;
            await CreatureCmd.Heal(creature, healAmount);

            MainFile.Logger?.Info($"KnownRelic: Triggered 10k revive effect, healed to full HP");
        }
    }

    /// <summary>
    /// 获取当前 Action 计数
    /// </summary>
    public int GetActionCount() => ActionCount;

    /// <summary>
    /// 修改选卡奖励的额外选项，添加"存续"按钮（继承自UnknownRelic的功能）
    /// </summary>
    public override bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
    {
        return TryAddRecordCardAlternative(player, cardReward, alternatives);
    }
}
