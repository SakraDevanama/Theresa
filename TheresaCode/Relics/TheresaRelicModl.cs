using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rewards;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Utils;
using System.Text.RegularExpressions;

namespace Theresa.TheresaCode.Relics;

[Pool(typeof(TheresaRelicPool))]
public abstract partial class TheresaRelicModel : CustomRelicModel
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string RelicId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string IconBasePath => $"res://Theresa/images/relics/{RelicId}";

    public override string PackedIconPath => $"{IconBasePath}.png";
    protected override string BigIconPath => $"{IconBasePath}.png";
    protected override string PackedIconOutlinePath => $"{IconBasePath}_outline.png";

    protected TheresaRelicModel() : base()
    {
    }

    protected TheresaRelicModel(bool autoAdd) : base(autoAdd)
    {
    }

    #region 选卡奖励"存续"功能（UnknownRelic/KnownRelic共用）

    /// <summary>
    /// 修改选卡奖励的额外选项，添加"存续"按钮
    /// 允许玩家将1张随机奖励卡牌记录到已移除卡牌列表（而非加入牌组）
    /// </summary>
    protected bool TryAddRecordCardAlternative(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
    {
        // 只影响拥有此遗物的玩家
        if (Owner != player)
            return false;

        // 检查是否还有空间添加额外选项（最多2个）
        if (alternatives.Count >= 2)
            return false;

        // 检查是否有可记录的卡牌
        var rewardCards = cardReward.Cards.ToList();
        if (rewardCards.Count == 0)
            return false;

        // 添加"存续"选项
        alternatives.Add(new CardRewardAlternative(
            "RECORD_CARD",
            () => OnRecordCardSelected(cardReward),
            PostAlternateCardRewardAction.EndSelectionAndCompleteReward
        ));

        return true;
    }

    /// <summary>
    /// "存续"按钮被点击时的处理：随机记录1张奖励卡牌到已移除列表
    /// </summary>
    private Task OnRecordCardSelected(CardReward cardReward)
    {
        var rewardCards = cardReward.Cards.ToList();
        if (rewardCards.Count == 0)
            return Task.CompletedTask;

        // 随机选择1张奖励卡牌
        var random = new System.Random();
        var selectedCard = rewardCards[random.Next(rewardCards.Count)];

        // 记录到已移除卡牌追踪器（等同于"移除"记录）
        var serializableCard = selectedCard.ToSerializable();
        RemovedCardsTracker.AddRemovedCard(serializableCard);

        // 获取卡牌显示名称（Title已经是格式化好的字符串）
        var cardName = selectedCard.Title;

        MainFile.Logger?.Info($"[{GetType().Name}] 存续：记录了卡牌 {selectedCard.Id.Entry} ({cardName}) 到已移除列表");

        // 延迟触发遗物闪光，避免与选卡界面关闭的渲染冲突
        TaskHelper.RunSafely(DelayedFlash());

        return Task.CompletedTask;
    }

    /// <summary>
    /// 延迟触发遗物闪光，避免渲染冲突
    /// </summary>
    private async Task DelayedFlash()
    {
        await Cmd.Wait(0.5f);
        Flash();
    }

    #endregion

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}