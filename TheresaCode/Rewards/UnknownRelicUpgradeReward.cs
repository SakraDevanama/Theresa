using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using Theresa.TheresaCode.Relics;

namespace Theresa.TheresaCode.Rewards;

/// <summary>
/// UnknownRelic 升级奖励：在 ACT2 Boss 胜利后出现，允许玩家选择是否升级 starter 遗物
/// 
/// 选择后：
/// 1. 将 UnknownRelic 替换为 KnownRelic（继承计数）
/// 2. 获得 KnownRelic
/// </summary>
public sealed class UnknownRelicUpgradeReward : Reward
{
    private readonly RelicModel _knownRelic;
    private readonly UnknownRelic _unknownRelic;
    private bool _wasTaken;

    protected override RewardType RewardType => RewardType.Relic;

    public override int RewardsSetIndex => 3;

    public override LocString Description => _knownRelic.Title;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => _knownRelic.HoverTips;

    public override bool IsPopulated => true;

    public UnknownRelicUpgradeReward(RelicModel knownRelic, Player player, UnknownRelic unknownRelic)
        : base(player)
    {
        knownRelic.AssertMutable();
        _knownRelic = knownRelic;
        _unknownRelic = unknownRelic;
    }

    public override void Populate()
    {
        // 已经预填充了，无需操作
    }

    public override TextureRect CreateIcon()
    {
        TextureRect val = new TextureRect();
        val.Texture = _knownRelic.BigIcon;
        ((CanvasItem)val).Material = (Material)(ShaderMaterial)((Resource)PreloadManager.Cache.GetMaterial("res://materials/ui/relic_mat.tres")).Duplicate(true);
        _knownRelic.UpdateTexture(val);
        ((Control)val).SetAnchorsPreset((Control.LayoutPreset)15);
        val.ExpandMode = (TextureRect.ExpandModeEnum)1;
        return val;
    }

    protected override async Task<bool> OnSelect()
    {
        MainFile.Logger?.Info($"[UnknownRelicUpgradeReward] Player chose to upgrade UnknownRelic to KnownRelic!");

        // 先替换 UnknownRelic 为 KnownRelic
        if (_unknownRelic.Owner != null)
        {
            await RelicCmd.Replace(_unknownRelic, _knownRelic);
        }

        // 同步到奖励系统
        RunManager.Instance?.RewardSynchronizer.SyncLocalObtainedRelic(_knownRelic);
        _wasTaken = true;

        MainFile.Logger?.Info($"[UnknownRelicUpgradeReward] Upgrade complete!");
        return true;
    }

    public override void OnSkipped()
    {
        if (!_wasTaken)
        {
            // 记录玩家跳过了升级
            var entry = base.Player.RunState.CurrentMapPointHistoryEntry?.GetEntry(LocalContext.NetId.Value);
            if (entry != null)
            {
                entry.RelicChoices.Add(new ModelChoiceHistoryEntry(_knownRelic.Id, wasPicked: false));
            }
            RunManager.Instance?.RewardSynchronizer.SyncLocalSkippedRelic(_knownRelic);
            MainFile.Logger?.Info($"[UnknownRelicUpgradeReward] Player skipped the upgrade.");
        }
    }

    public override void MarkContentAsSeen()
    {
        SaveManager.Instance?.MarkRelicAsSeen(_knownRelic);
    }
}
