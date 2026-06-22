using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 未解析的代码
/// 记录战斗中执行的 Action 数量。
/// 当通过先古之民事件升级时，会替换为 KnownRelic（文明的存续）。
/// 
/// 选卡奖励额外功能：
/// 在选卡奖励界面添加"存续"按钮，允许玩家将1张随机奖励卡牌记录到已移除卡牌列表
/// （而非加入牌组）。被记录的卡牌可用于重现等机制。
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class UnknownRelic : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    // 显示计数器
    public override bool ShowCounter => true;

    // 显示 Action 计数
    public override int DisplayAmount => ActionCount;

    // DynamicVar 键名常量
    private const string ActionCountKey = "ActionCount";

    // 保存属性：Action 计数（核心数据，会被保存到存档）
    [SavedProperty]
    private int ActionCount { get; set; }

    // 保存属性：已移除卡牌列表（用于"重现"机制，会随 RunState 同步到 Client）
    [SavedProperty]
    private List<SerializableCard> RemovedCards { get; set; } = new();

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
    /// 获得遗物时初始化
    /// </summary>
    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        SyncActionCountToDynamicVar();
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
    /// 获取当前 Action 计数
    /// </summary>
    public int GetActionCount() => ActionCount;

    /// <summary>
    /// 升级替换为 KnownRelic
    /// </summary>
    public override RelicModel? GetUpgradeReplacement()
    {
        // 创建 KnownRelic 并转移计数和已移除卡牌记录
        var knownRelic = ModelDb.Relic<KnownRelic>();
        if (knownRelic is KnownRelic kr)
        {
            // 将 ActionCount 作为初始计数转移过去
            kr.SetInitialActionCount(ActionCount);

            // 转移已移除卡牌记录，保证升级后"重现"机制仍能访问之前的数据
            kr.TransferRemovedCardsFrom(this);
        }
        return knownRelic;
    }

    /// <summary>
    /// 联机模式：玩家回合开始时固定 +20 计数（近似替代 Action 级计数）
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);

        if (RunManager.Instance?.NetService?.Type.IsMultiplayer() != true)
            return;
        if (player != Owner)
            return;

        ActionCount += 20;
        SyncActionCountToDynamicVar();
    }

    /// <summary>
    /// 联机模式：玩家回合结束前固定 +20 计数（近似替代 Action 级计数）
    /// </summary>
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.BeforeSideTurnEnd(choiceContext, side, participants);

        if (RunManager.Instance?.NetService?.Type.IsMultiplayer() != true)
            return;
        if (Owner == null || side != CombatSide.Player)
            return;

        ActionCount += 20;
        SyncActionCountToDynamicVar();
    }

    /// <summary>
    /// 修改选卡奖励的额外选项，添加"存续"按钮
    /// </summary>
    public override bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
    {
        return TryAddRecordCardAlternative(player, cardReward, alternatives);
    }

    #region 已移除卡牌追踪（用于"重现"机制）

    public override void TrackRemovedCard(SerializableCard card)
    {
        if (card?.Id == null) return;

        var key = GetCardKey(card);
        if (!RemovedCards.Any(c => GetCardKey(c) == key))
        {
            RemovedCards.Add(card);
            MainFile.Logger?.Info($"[{GetType().Name}] Tracked removed card: {card.Id.Entry} (upgrade {card.CurrentUpgradeLevel})");
        }
    }

    public override IReadOnlyList<SerializableCard> GetTrackedRemovedCards() => RemovedCards.AsReadOnly();

    public override void TransferRemovedCardsFrom(TheresaRelicModel other)
    {
        if (other == null) return;
        foreach (var card in other.GetTrackedRemovedCards())
        {
            TrackRemovedCard(card);
        }
    }

    public override void ResetForNewRun()
    {
        RemovedCards.Clear();
        ActionCount = 0;
        SyncActionCountToDynamicVar();
    }

    private static string GetCardKey(SerializableCard card) => $"{card.Id?.Entry}_{card.CurrentUpgradeLevel}";

    #endregion
}
