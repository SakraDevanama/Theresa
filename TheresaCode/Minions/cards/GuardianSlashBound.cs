using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Commands;
using MinionLib.Powers;
using Theresa.TheresaCode.Cards;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Keywords;
using Theresa.TheresaCode.Minions.Models;
using Theresa.TheresaCode.Minions.Interfaces;

namespace Theresa.TheresaCode.Minions.Cards;

/// <summary>
/// 卫护 (GuardianSlashBound)
/// 1费Token技能牌 - 绑定特雷西斯的牌
/// 给予绑定的特雷西斯8点格挡和守护者能力（持续）。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class GuardianSlashBound()
    : TheresaCardModel(1, CardType.Skill, CardRarity.Token, TargetType.Self), IMinionBoundCard
{
    
    private const bool shouldShowInCardLibrary = false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [DimKeyword.Dim];

    private const int BaseBlockBonus = 20;

    // IMinionBoundCard 接口实现
    public uint? BoundMinionCombatId { get; set; }
    public string? BoundMinionNameSnapshot { get; set; }

    // 如果绑定的随从已死亡，框变为红色
    protected override bool ShouldGlowRedInternal => this.ResolveBoundMinion() is not { IsAlive: true };

    // 格挡变量
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BaseBlockBonus", BaseBlockBonus)
    ];

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        this.AddBoundNameToDescription(description);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner?.Creature;
        if (owner == null)
        {
            MainFile.Logger?.Info("[GuardianSlashBound] OnPlay: Owner is null");
            return;
        }

        // 解析绑定的随从
        var boundMinion = this.ResolveBoundMinion();
        bool hasLivingSwordsman = boundMinion is { IsAlive: true };

        MainFile.Logger?.Info($"[GuardianSlashBound] OnPlay: Bound minion present = {hasLivingSwordsman}, Name = {BoundMinionNameSnapshot}, CombatId = {BoundMinionCombatId}");

        // 如果绑定的特雷西斯存活，给予格挡和守护者能力
        if (hasLivingSwordsman && boundMinion != null)
        {
            MainFile.Logger?.Info("[GuardianSlashBound] OnPlay: Applying block and Guardian power to bound Swordsman");
            
            // 增加20点最大生命值，然后回复20点生命
            await CreatureCmd.SetMaxHp(boundMinion, boundMinion.MaxHp + BaseBlockBonus);
            await CreatureCmd.Heal(boundMinion, BaseBlockBonus);
            MainFile.Logger?.Info($"[GuardianSlashBound] Increased max HP by {BaseBlockBonus} and healed {BaseBlockBonus} HP for {boundMinion.Name}");
            
            // 给予守护者能力（持续，不自动移除）
            await ApplyGuardianPower(boundMinion, owner);
        }
        else
        {
            MainFile.Logger?.Info($"[GuardianSlashBound] OnPlay: Bound minion not alive or not found, skipping effects");
        }
    }

    /// <summary>
    /// 给予绑定的随从守护者能力（持续，不自动移除）
    /// </summary>
    private async Task ApplyGuardianPower(Creature boundMinion, Creature owner)
    {
        try
        {
            // 检查是否已有守护者能力
            var existingPower = boundMinion.GetPower<MinionGuardianPower>();
            if (existingPower != null)
            {
                MainFile.Logger?.Info($"[GuardianSlashBound] {boundMinion.Name} already has Guardian power, skipping");
                return;
            }
            
            MainFile.Logger?.Info($"[GuardianSlashBound] Applying Guardian power to {boundMinion.Name}");
            
            // 给予守护者能力（持续，不自动移除）
            await PowerCmd.Apply<MinionGuardianPower>(new ThrowingPlayerChoiceContext(), boundMinion, 1m, owner, this);
            
            MainFile.Logger?.Info($"[GuardianSlashBound] Guardian power applied successfully to {boundMinion.Name}");
        }
        catch (Exception ex)
        {
            MainFile.Logger?.Info($"[GuardianSlashBound] Error applying Guardian power: {ex.Message}");
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后：额外格挡增加到30
        DynamicVars["BaseBlockBonus"].UpgradeValueBy(10m);
    }
}
