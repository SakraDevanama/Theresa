using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;
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

    #region 已移除卡牌追踪（用于"重现"机制，UnknownRelic/KnownRelic共用）

    /// <summary>
    /// 记录一张从牌组移除的卡牌。
    /// 默认空实现；真正需要持久化的子类（UnknownRelic/KnownRelic）会重写。
    /// </summary>
    public virtual void TrackRemovedCard(SerializableCard card)
    {
    }

    /// <summary>
    /// 获取本遗物记录的已移除卡牌（用于"重现"选牌）。
    /// 默认返回空列表。
    /// </summary>
    public virtual IReadOnlyList<SerializableCard> GetTrackedRemovedCards() => System.Array.Empty<SerializableCard>();

    /// <summary>
    /// 从另一件 Theresa 遗物转移已移除卡牌记录（UnknownRelic 升级成 KnownRelic 时使用）。
    /// </summary>
    public virtual void TransferRemovedCardsFrom(TheresaRelicModel other)
    {
    }

    /// <summary>
    /// 新一局游戏开始时重置遗物上的运行时数据。
    /// 用于防止 ModelDb 复用的 canonical 实例把上一局的 RemovedCards 带到新局。
    /// </summary>
    public virtual void ResetForNewRun()
    {
    }

    #endregion

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

        // 检查是否还有空间添加额外选项（游戏上限2个）
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

        // 防御：如果其他遗物/Mod 也在同一奖励里添加了选项，导致超过2个，
        // 则回退自己的添加，避免 CardRewardAlternative.Generate 抛异常。
        if (alternatives.Count > 2)
        {
            alternatives.RemoveAt(alternatives.Count - 1);
            return false;
        }

        return true;
    }

    /// <summary>
    /// "存续"按钮被点击时的处理：随机记录1张奖励卡牌到已移除列表
    /// 记录完成后在屏幕中央显示浮动提示，告知用户移除了哪张卡
    /// </summary>
    private Task OnRecordCardSelected(CardReward cardReward)
    {
        var rewardCards = cardReward.Cards.ToList();
        if (rewardCards.Count == 0)
            return Task.CompletedTask;

        // 使用游戏原版的确定性 RNG（RunState.Rng.Shuffle），保证 Host/Client 得到相同结果，
        // 避免联机时因各自 new System.Random() 导致记录的卡牌不同，进而引发状态分歧。
        CardModel selectedCard;
        if (rewardCards.Count == 1)
        {
            selectedCard = rewardCards[0];
        }
        else if (Owner?.RunState?.Rng?.Shuffle != null)
        {
            selectedCard = Owner.RunState.Rng.Shuffle.NextItem(rewardCards) ?? rewardCards[0];
        }
        else
        {
            // 兜底：避免空引用，但正常不应走到这里
            selectedCard = rewardCards[0];
        }

        // 记录到已移除卡牌追踪器（等同于"移除"记录）
        var serializableCard = selectedCard.ToSerializable();
        RemovedCardsTracker.AddRemovedCard(serializableCard, Owner);

        // 获取卡牌显示名称（Title已经是格式化好的字符串）
        var cardName = selectedCard.Title;

        MainFile.Logger?.Info($"[{GetType().Name}] 存续：记录了卡牌 {selectedCard.Id.Entry} ({cardName}) 到已移除列表");

        // 延迟触发遗物闪光，避免与选卡界面关闭的渲染冲突
        TaskHelper.RunSafely(DelayedFlash());

        // 显示浮动提示，告知用户移除了哪张卡
        ShowRecordToast(cardName);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 在屏幕中央显示浮动提示，告知用户存续了哪张卡牌
    /// 使用 Godot 原生 Control / Panel / Label 自绘，带淡入/停留/淡出动画
    /// </summary>
    private static void ShowRecordToast(string cardName)
    {
        try
        {
            // 获取当前场景的根节点（CanvasLayer 或 Node）
            var tree = Engine.GetMainLoop() as SceneTree;
            var root = tree?.Root;
            if (root == null) return;

            // 创建提示容器（全屏覆盖，不拦截鼠标）
            var toastContainer = new Control();
            toastContainer.Name = "RecordToast";
            toastContainer.ZIndex = 100; // 确保在最上层
            toastContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
            toastContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);

            // 创建背景面板
            var panel = new Panel();
            panel.Name = "ToastPanel";
            panel.MouseFilter = Control.MouseFilterEnum.Ignore;

            // 样式：深色半透明背景 + 金色边框
            var styleBox = new StyleBoxFlat();
            styleBox.BgColor = new Color(0.1f, 0.08f, 0.05f, 0.95f);
            styleBox.BorderColor = new Color(0.8f, 0.65f, 0.25f, 1.0f);
            styleBox.BorderWidthLeft = 3;
            styleBox.BorderWidthTop = 3;
            styleBox.BorderWidthRight = 3;
            styleBox.BorderWidthBottom = 3;
            styleBox.CornerRadiusTopLeft = 8;
            styleBox.CornerRadiusTopRight = 8;
            styleBox.CornerRadiusBottomLeft = 8;
            styleBox.CornerRadiusBottomRight = 8;
            panel.AddThemeStyleboxOverride("panel", styleBox);

            // 面板尺寸
            float panelWidth = 500f;
            float panelHeight = 120f;
            panel.CustomMinimumSize = new Vector2(panelWidth, panelHeight);
            panel.Size = new Vector2(panelWidth, panelHeight);

            // 居中定位（视口高度 35% 处）
            var viewportSize = root.GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
            panel.Position = new Vector2((viewportSize.X - panelWidth) / 2f, viewportSize.Y * 0.35f);

            // 创建标题标签
            var titleLabel = new Label();
            titleLabel.Name = "TitleLabel";
            titleLabel.Text = "存续成功";
            titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            titleLabel.VerticalAlignment = VerticalAlignment.Center;
            titleLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.85f, 0.55f, 1.0f));
            titleLabel.AddThemeFontSizeOverride("font_size", 28);
            titleLabel.Size = new Vector2(panelWidth, 50f);
            titleLabel.Position = new Vector2(0, 10f);
            titleLabel.MouseFilter = Control.MouseFilterEnum.Ignore;

            // 创建内容标签（显示卡牌名称）
            var contentLabel = new Label();
            contentLabel.Name = "ContentLabel";
            contentLabel.Text = $"已移除卡牌：{cardName}";
            contentLabel.HorizontalAlignment = HorizontalAlignment.Center;
            contentLabel.VerticalAlignment = VerticalAlignment.Center;
            contentLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f, 1.0f));
            contentLabel.AddThemeFontSizeOverride("font_size", 22);
            contentLabel.Size = new Vector2(panelWidth, 50f);
            contentLabel.Position = new Vector2(0, 55f);
            contentLabel.MouseFilter = Control.MouseFilterEnum.Ignore;

            // 组装节点
            panel.AddChild(titleLabel);
            panel.AddChild(contentLabel);
            toastContainer.AddChild(panel);
            root.AddChild(toastContainer);

            // 创建淡入淡出动画
            var tween = toastContainer.CreateTween().SetParallel(false);

            // 初始状态：完全透明
            toastContainer.Modulate = new Color(1, 1, 1, 0);

            // 淡入（0.3秒）
            tween.TweenProperty(toastContainer, "modulate:a", 1.0f, 0.3f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Quad);

            // 保持显示（2秒）
            tween.TweenInterval(2.0f);

            // 淡出（0.5秒）
            tween.TweenProperty(toastContainer, "modulate:a", 0.0f, 0.5f)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Quad);

            // 动画结束后移除节点
            tween.TweenCallback(Callable.From(() =>
            {
                toastContainer.QueueFree();
            }));
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Warn($"[TheresaRelicModel] Failed to show record toast: {ex.Message}");
        }
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