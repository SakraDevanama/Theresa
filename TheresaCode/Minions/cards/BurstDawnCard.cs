using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Cards;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Minions.Interfaces;
using Theresa.TheresaCode.Minions.Nodes;
using Theresa.TheresaCode.Minions.Powers;
using Theresa.TheresaCode.Powers;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Minions.Cards;

/// <summary>
/// 爆裂黎明 - 维什戴尔专属仆从卡
/// 2费攻击牌
/// 给所有敌人造成20点伤害，并给予玩家一层微尘
/// 触发维什戴尔的Skill_3_Loop动作
/// 需要消耗绑定维什戴尔的3层爆裂黎明充能
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class BurstDawnCard()
    : TheresaCardModel(1, CardType.Skill, CardRarity.Token, TargetType.Self), IMinionBoundCard
{
    
    private const bool shouldShowInCardLibrary = false;
    private const int BaseDamage = 20;
    private const int UpgradedDamage = 18;
    private const int DustGain = 1;
    private const int ChargeCost = 3;
    private const string SkillSoundPath = "res://Theresa/audio/wisdel_skill_1.wav";
    private const string Slash3SoundPath = "res://Theresa/audio/wisdel_slash3.wav";

    // IMinionBoundCard 接口实现
    public uint? BoundMinionCombatId { get; set; }
    public string? BoundMinionNameSnapshot { get; set; }

    // 如果绑定的随从已死亡，框变为红色
    protected override bool ShouldGlowRedInternal => this.ResolveBoundMinion() is not { IsAlive: true };

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ 
        CardKeyword.Retain,
        CardKeyword.Innate,
        LingerKeyword.Linger,
        DimKeyword.Dim,
    ];

    // 提示文本：微尘、魔王残响（微尘后续变化）
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [
        HoverTipFactory.FromPower<MantraPower>(),
        HoverTipFactory.FromPower<DivinityStance>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Damage", IsUpgraded ? UpgradedDamage : BaseDamage),
        new DynamicVar("DustGain", DustGain),
        new DynamicVar("ChargeCost", ChargeCost)
    ];

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        this.AddBoundNameToDescription(description);
    }

    /// <summary>
    /// 检查是否可打出：需要绑定的维什戴尔有至少3层爆裂黎明充能
    /// </summary>
    protected override bool IsPlayable
    {
        get
        {
            var minion = this.ResolveBoundMinion();
            if (minion is not { IsAlive: true }) return false;
            var charge = minion.GetPower<WisdelDawnChargePower>();
            return charge != null && charge.Amount >= ChargeCost;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        MainFile.Logger?.Info("[BurstDawnCard] OnPlay started");
        
        if (CombatState == null || Owner == null) 
        {
            MainFile.Logger?.Info($"[BurstDawnCard] Early return: CombatState={CombatState != null}, Owner={Owner != null}");
            return;
        }

        var ownerCreature = Owner.Creature;
        var damage = IsUpgraded ? UpgradedDamage : BaseDamage;

        // 解析绑定的维什戴尔
        var wisdel = this.ResolveBoundMinion();
        if (wisdel is not { IsAlive: true })
        {
            MainFile.Logger?.Info("[BurstDawnCard] Bound Wisdel not found or dead");
            return;
        }

        // 消耗3层爆裂黎明充能
        var chargePower = wisdel.GetPower<WisdelDawnChargePower>();
        if (chargePower == null || chargePower.Amount < ChargeCost)
        {
            MainFile.Logger?.Info($"[BurstDawnCard] Not enough charge: {chargePower?.Amount ?? 0}");
            return;
        }
        await PowerCmd.Apply<WisdelDawnChargePower>(wisdel, -ChargeCost, ownerCreature, this);

        // 播放技能音效
        PlaySkillSound();
        
        // 触发维什戴尔的Skill_3_Loop动画（在后台运行，不阻塞伤害）
        _ = PlayWisdelSkill3Animation(wisdel);
        
        // 使用维什戴尔作为伤害来源（立即执行，不等待动画）
        await DealDamageFromWisdel(choiceContext, wisdel, damage);

        // 给予玩家一层微尘
        await PowerCmd.Apply<MantraPower>(ownerCreature, DustGain, ownerCreature, this);

        // 打出后自动回到手牌
        await CardPileCmd.Add(this, PileType.Hand);
    }

    /// <summary>
    /// 播放维什戴尔的Skill_3动画，播放完成后切换回Idle
    /// </summary>
    private async Task PlayWisdelSkill3Animation(Creature wisdel)
    {
        try
        {
            MainFile.Logger?.Info($"[BurstDawnCard] Trying to play Skill_3 animation for {wisdel.Name}");
            
            var room = NCombatRoom.Instance;
            if (room == null) 
            {
                MainFile.Logger?.Info("[BurstDawnCard] NCombatRoom.Instance is null");
                return;
            }

            var nCreature = room.GetCreatureNode(wisdel);
            if (nCreature == null) 
            {
                MainFile.Logger?.Info("[BurstDawnCard] GetCreatureNode returned null");
                return;
            }

            MainFile.Logger?.Info($"[BurstDawnCard] Got NCreature, Visuals type: {nCreature.Visuals?.GetType().Name}");

            if (nCreature.Visuals is Wisdel wisdelVisuals)
            {
                MainFile.Logger?.Info("[BurstDawnCard] Playing Skill_3_Loop animation via Wisdel.PlayAnimation");
                wisdelVisuals.PlayAnimation("Skill_3_Loop", false);
                
                await Task.Delay(1200);
                
                MainFile.Logger?.Info("[BurstDawnCard] Switching back to Idle animation");
                wisdelVisuals.PlayAnimation("Idle", true);
            }
            else
            {
                MainFile.Logger?.Info($"[BurstDawnCard] Visuals is not Wisdel, type: {nCreature.Visuals?.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Info($"[BurstDawnCard] Failed to play Skill_3 animation: {ex.Message}");
        }
    }

    /// <summary>
    /// 从维什戴尔造成伤害（但伤害来源设为玩家，避免触发残影和余震）
    /// </summary>
    private async Task DealDamageFromWisdel(PlayerChoiceContext choiceContext, Creature wisdel, int damage)
    {
        var enemies = CombatState?.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies == null) return;

        var player = Owner?.Creature;
        if (player == null) return;

        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive)
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    enemy,
                    damage,
                    ValueProp.Move,
                    player,
                    this
                );
            }
        }
        
        PlaySound(Slash3SoundPath, 1.4f);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Damage"].UpgradeValueBy(4);
    }

    /// <summary>
    /// 播放技能音效（语音，音量0.9倍）
    /// </summary>
    private void PlaySkillSound()
    {
        PlaySound(SkillSoundPath, 0.9f);
    }

    /// <summary>
    /// 播放指定路径的音效
    /// </summary>
    private void PlaySound(string soundPath, float volumeMultiplier = 1.0f)
    {
        try
        {
            var stream = GD.Load<AudioStream>(soundPath);
            if (stream == null) return;

            float volumeDb = 20f * (float)Math.Log10(volumeMultiplier);

            var player = new AudioStreamPlayer
            {
                Stream = stream,
                VolumeDb = volumeDb,
                PitchScale = 1f,
                Autoplay = true
            };

            if (Engine.GetMainLoop() is SceneTree sceneTree)
            {
                sceneTree.Root.AddChild(player);
                player.Finished += () => player.QueueFree();
            }
        }
        catch
        {
            // 忽略音效播放错误
        }
    }
}
