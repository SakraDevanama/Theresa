using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Extensions;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 愈合
/// 1费能力牌
/// 你每已损失5点生命，回复1点生命。
/// 使用后从这场战斗中移除。
/// 这张牌从卡组移除时，提升3（+2）点最大生命。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class Cure() : TheresaCardModel(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    /// <summary>
    /// 使用后从战斗中彻底移除（不会进入弃牌堆或消耗堆）
    /// </summary>
    protected override PileType GetResultPileType()
    {
        return PileType.None;
    }
    // 每损失多少生命回复1点
    private const int HpLostPerHeal = 5;
    // 基础最大生命提升
    private const int BaseMaxHpGain = 3;
    // 升级后额外提升
    private const int UpgradeMaxHpDelta = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MaxHpGain", BaseMaxHpGain),
        new CureTotalHealVar()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null) return;

        // 计算已损失的生命值
        int maxHp = Owner.Creature.MaxHp;
        int currentHp = Owner.Creature.CurrentHp;
        int hpLost = maxHp - currentHp;

        if (hpLost <= 0) return;

        // 每损失5点生命回复1点
        int healAmount = hpLost / HpLostPerHeal;
        
        if (healAmount > 0)
        {
            await CreatureCmd.Heal(Owner.Creature, healAmount);
        }
    }

    /// <summary>
    /// 卡牌从卡组移除前触发 - 用于检测卡牌被移除（商店、事件等）
    /// </summary>
    public override async Task BeforeCardRemoved(CardModel card)
    {
        // 确保是当前卡牌，并且当前在卡组中
        if (card != this) return;
        if (card.Pile?.Type != PileType.Deck) return;
        
        // 增加最大生命值
        if (Owner?.Creature != null)
        {
            int maxHpGain = (int)DynamicVars["MaxHpGain"].BaseValue;
            await CreatureCmd.GainMaxHp(Owner.Creature, maxHpGain);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后提升的最大生命+2
        DynamicVars["MaxHpGain"].UpgradeValueBy(UpgradeMaxHpDelta);
    }
}
