using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using Theresa.TheresaCode.Minions.Cards;

namespace Theresa.TheresaCode.Events;

/// <summary>
/// 阿米娅的邂逅
/// 参考Wellspring事件，场景和对话风格相同
/// 在Act1必定出现一次
/// 选项1：获得一张"约定：阿米娅"牌
/// 选项2：获得随机药水（与Wellspring相同）
/// </summary>
public sealed class AmiyaEncounterEvent : CustomEventModel
{
    /// <summary>
    /// 标记阿米娅事件是否已被触发（每局游戏）
    /// </summary>
    public static bool HasBeenTriggered { get; private set; }

    /// <summary>
    /// 重置触发状态（新游戏开始时调用）
    /// </summary>
    public static void ResetTriggerState()
    {
        HasBeenTriggered = false;
    }

    public AmiyaEncounterEvent() : base(autoAdd: true)
    {
    }

    public override void OnRoomEnter()
    {
        base.OnRoomEnter();
        // 标记事件已被触发
        HasBeenTriggered = true;
        MainFile.Logger?.Info("[AmiyaEncounterEvent] 阿米娅的邂逅事件已被触发");
    }

    /// <summary>
    /// 只在Act1出现 (CurrentActIndex == 0)
    /// 游戏会在Act1的事件池中随机选择，由于IsAllowed限制，此事件只会在Act1出现
    /// 且游戏机制保证每个事件每局游戏只会出现一次
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
    /// 场景路径: res://Theresa/room/amiya3_room/Amiya_event_room.tscn
    /// </summary>
    public override string? CustomBackgroundScenePath => "res://Theresa/room/amiya3_room/Amiya_event_room.tscn";

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    /// <summary>
    /// 只在Act1出现
    /// 游戏的事件系统会确保每个事件每局只出现一次
    /// </summary>
    public override bool IsAllowed(IRunState runState)
    {
        // 只在Act1出现 (CurrentActIndex == 0)
        return runState.CurrentActIndex == 0;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, SummonAmiya, "THERESA-AMIYA_ENCOUNTER_EVENT.pages.INITIAL.options.SUMMON"),
            new EventOption(this, Bottle, "THERESA-AMIYA_ENCOUNTER_EVENT.pages.INITIAL.options.BOTTLE")
        ];
    }

    /// <summary>
    /// 召唤阿米娅 - 给予玩家一张"约定：阿米娅"牌
    /// </summary>
    private async Task SummonAmiya()
    {
        if (Owner == null) return;

        // 创建一张"约定：阿米娅"牌
        var amiyaCard = Owner.RunState.CreateCard(ModelDb.Card<TheAmiya>(), Owner);
        if (amiyaCard != null)
        {
            await RewardsCmd.OfferCustom(Owner, new List<Reward>(1)
            {
                new CardReward(new[] { amiyaCard }, CardCreationSource.Other, Owner)
            });
        }

        SetEventFinished(L10NLookup("THERESA-AMIYA_ENCOUNTER_EVENT.pages.SUMMON.description"));
    }

    /// <summary>
    /// 装瓶 - 与Wellspring相同，获得随机药水
    /// </summary>
    private async Task Bottle()
    {
        if (Owner == null) return;

        IEnumerable<PotionModel> items = Owner.Character.PotionPool.GetUnlockedPotions(Owner.UnlockState)
            .Concat(ModelDb.PotionPool<SharedPotionPool>().GetUnlockedPotions(Owner.UnlockState));
        
        PotionModel potionModel = Owner.PlayerRng.Rewards.NextItem(items);
        if (potionModel != null)
        {
            await RewardsCmd.OfferCustom(Owner, new List<Reward>(1)
            {
                new PotionReward(potionModel.ToMutable(), Owner)
            });
        }

        SetEventFinished(L10NLookup("THERESA-AMIYA_ENCOUNTER_EVENT.pages.BATHE.description"));
    }
}
