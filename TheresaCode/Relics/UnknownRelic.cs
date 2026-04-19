using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
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
        // 创建 KnownRelic 并转移计数
        var knownRelic = ModelDb.Relic<KnownRelic>();
        if (knownRelic is KnownRelic kr)
        {
            // 将 ActionCount 作为初始计数转移过去
            kr.SetInitialActionCount(ActionCount);
        }
        return knownRelic;
    }

    /// <summary>
    /// 修改选卡奖励的额外选项，添加"存续"按钮
    /// </summary>
    public override bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
    {
        return TryAddRecordCardAlternative(player, cardReward, alternatives);
    }
}
