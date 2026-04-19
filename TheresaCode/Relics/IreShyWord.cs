using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Commands;
using Theresa.TheresaCode.Enchantments;

namespace Theresa.TheresaCode.Relics;

/// <summary>
/// 以勒什呓语 (IreShyWord)
/// 普通遗物（Common）
/// 
/// 效果：
/// 1. 每当你打出一张卡牌时：触发该卡牌的所有丝线效果。
/// 2. 若该卡牌没有丝线：为其编织意志丝线。
/// 
/// 对应原版 Java：
/// - onUseCard: SilkPatch.triggerSilk(ALL, targetCard, HAND)
///              若卡牌无丝线则 SetSilkAction(MindSilk)
/// 
/// 注意：原版 Java 中 IreShyWord 是 Boss 遗物（RelicTier.BOSS），
/// 但用户要求作为 Common 搬运。
/// </summary>
[Pool(typeof(TheresaRelicPool))]
public sealed class IreShyWord : TheresaRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    /// <summary>
    /// 卡牌打出后：触发丝线效果，若无丝线则编织意志
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        var card = cardPlay.Card;
        if (card == null) return;

        // 检查卡牌是否有丝线附魔
        if (card.Enchantment is AbstractSilkEnchantment silk)
        {
            // 触发丝线的打出后效果
            Flash();
            await silk.AfterPlayed(choiceContext, cardPlay);
            silk.TriggeredOnce();
        }
        else
        {
            // 卡牌没有丝线：编织意志丝线
            Flash();
            var mindSilk = (MindSilkEnchantment)ModelDb.Enchantment<MindSilkEnchantment>().ToMutable();
            WeaveCmd.Weave(card, mindSilk, mustReplace: false, canReplace: true);
        }
    }
}
