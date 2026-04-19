using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Theresa.TheresaCode.Minions.Cards;

namespace Theresa.TheresaCode.Events;

/// <summary>
/// 维什戴尔的邂逅
/// 参考阿米娅的邂逅事件
/// 在Act2必定出现一次
/// 选项1：获得一张"约定：维什戴尔"牌
/// 选项2：离开
/// </summary>
public sealed class WisdelEncounterEvent : CustomEventModel
{
    /// <summary>
    /// 标记维什戴尔事件是否已被触发（每局游戏）
    /// </summary>
    public static bool HasBeenTriggered { get; private set; }

    /// <summary>
    /// 重置触发状态（新游戏开始时调用）
    /// </summary>
    public static void ResetTriggerState()
    {
        HasBeenTriggered = false;
    }

    public WisdelEncounterEvent() : base(autoAdd: true)
    {
    }

    public override void OnRoomEnter()
    {
        base.OnRoomEnter();
        // 标记事件已被触发
        HasBeenTriggered = true;
        MainFile.Logger?.Info("[WisdelEncounterEvent] 维什戴尔的邂逅事件已被触发");
    }

    /// <summary>
    /// 只在Act2出现 (CurrentActIndex == 1)
    /// </summary>
    public override ActModel[] Acts => [];

    /// <summary>
    /// 使用默认布局，配合自定义背景场景
    /// </summary>
    public override EventLayoutType LayoutType => EventLayoutType.Default;

    /// <summary>
    /// 使用Wellspring的事件图片作为肖像
    /// </summary>
    public override string? CustomInitialPortraitPath => "res://images/events/wellspring.png";

    /// <summary>
    /// 自定义背景场景路径
    /// 场景路径: res://Theresa/room/wisdel_room/wisdel_event_room.tscn
    /// </summary>
    public override string? CustomBackgroundScenePath => "res://Theresa/room/wisdel_room/wisdel_event_room.tscn";

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    /// <summary>
    /// 只在Act2出现
    /// </summary>
    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex == 1;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, SummonWisdel, "THERESA-WISDEL_ENCOUNTER_EVENT.pages.INITIAL.options.SUMMON"),
            new EventOption(this, Leave, "THERESA-WISDEL_ENCOUNTER_EVENT.pages.INITIAL.options.LEAVE")
        ];
    }

    /// <summary>
    /// 召唤维什戴尔 - 给予玩家一张"约定：维什戴尔"牌
    /// </summary>
    private async Task SummonWisdel()
    {
        if (Owner == null) return;

        // 创建一张"约定：维什戴尔"牌
        var wisdelCard = Owner.RunState.CreateCard(ModelDb.Card<TheWisdel>(), Owner);
        if (wisdelCard != null)
        {
            await RewardsCmd.OfferCustom(Owner, new List<Reward>(1)
            {
                new CardReward(new[] { wisdelCard }, CardCreationSource.Other, Owner)
            });
        }

        SetEventFinished(L10NLookup("THERESA-WISDEL_ENCOUNTER_EVENT.pages.SUMMON.description"));
    }

    /// <summary>
    /// 离开
    /// </summary>
    private async Task Leave()
    {
        SetEventFinished(L10NLookup("THERESA-WISDEL_ENCOUNTER_EVENT.pages.LEAVE.description"));
    }
}
