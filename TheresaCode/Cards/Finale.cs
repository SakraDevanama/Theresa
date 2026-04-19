using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Extensions;
using Theresa.TheresaCode.Keywords;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 终结
/// X费攻击牌
/// 保留。造成{damage}点伤害X次。自动打出手中{finale_play_count}张牌。
/// 升级后：造成{damage}点伤害X次。自动打出手中{finale_play_count}张牌。
/// </summary>
[Pool(typeof(TheresaCardPool))]
public sealed class Finale() : TheresaCardModel(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    // 基础伤害
    private const decimal BaseDamage = 7m;
    // 升级后额外打出牌数
    private const int UpgradePlayCountBonus = 1;

    protected override bool HasEnergyCostX => true;

    // 添加保留关键词
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, DimKeyword.Dim];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new FinaleDamageVar(BaseDamage),
        new FinalePlayCountVar(UpgradePlayCountBonus)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        int xValue = ResolveEnergyXValue();
        MainFile.Logger?.Info($"Finale: X value = {xValue}");
        if (xValue < 0) xValue = 0;

        // 造成7点伤害X次
        if (xValue > 0)
        {
            MainFile.Logger?.Info($"Finale: Dealing damage {DynamicVars.Damage.BaseValue} x {xValue}");
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(xValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
            MainFile.Logger?.Info($"Finale: Damage dealt");
        }

        // 自动打出手中X张牌（升级后X+1）
        int playCount = xValue;
        if (IsUpgraded)
        {
            playCount += UpgradePlayCountBonus;
        }
        MainFile.Logger?.Info($"Finale: Will auto-play {playCount} cards");

        if (Owner != null && playCount > 0)
        {
            // 获取手中所有牌（排除当前这张牌）- 现在可以打出任意类型
            var handCards = PileType.Hand.GetPile(Owner).Cards
                .Where(c => c != this)
                .Take(playCount)
                .ToList();
            
            MainFile.Logger?.Info($"Finale: Found {handCards.Count} cards in hand to auto-play");

            foreach (var card in handCards)
            {
                var cardName = card.GetType().Name;
                MainFile.Logger?.Info($"Finale: Processing card {cardName}, Type={card.Type}, TargetType={card.TargetType}");
                
                try
                {
                    // 设置卡牌为免费打出
                    card.SetToFreeThisTurn();
                    
                    // 根据卡牌目标类型选择合适的目标
                    var target = GetTargetForCard(card);
                    
                    MainFile.Logger?.Info($"Finale: Auto-playing card {cardName} using CardCmd.AutoPlay");
                    // 使用 CardCmd.AutoPlay 自动打出卡牌（支持任意类型）
                    await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipXCapture: false, skipCardPileVisuals: false);
                    
                    MainFile.Logger?.Info($"Finale: Card {cardName} auto-play completed");
                }
                catch (Exception ex)
                {
                    MainFile.Logger?.Error($"Finale: FAILED to auto-play card {cardName}");
                    MainFile.Logger?.Error($"Finale: Exception message: {ex.Message}");
                    MainFile.Logger?.Error($"Finale: Exception type: {ex.GetType().FullName}");
                }
            }
            
            MainFile.Logger?.Info($"Finale: Finished auto-playing cards");
        }
        else
        {
            MainFile.Logger?.Info($"Finale: No cards to auto-play (Owner={Owner != null}, playCount={playCount})");
        }
    }

    /// <summary>
    /// 根据卡牌目标类型选择合适的目标
    /// </summary>
    private Creature? GetTargetForCard(CardModel card)
    {
        if (CombatState == null) 
        {
            MainFile.Logger?.Warn($"Finale: CombatState is null when getting target");
            return null;
        }

        var cardOwner = card.Owner;
        
        var target = card.TargetType switch
        {
            TargetType.AnyEnemy => CombatState.HittableEnemies.FirstOrDefault(),
            TargetType.AllEnemies => CombatState.HittableEnemies.FirstOrDefault(),
            TargetType.RandomEnemy => CombatState.HittableEnemies.FirstOrDefault(),
            TargetType.AnyAlly => cardOwner?.Creature,
            TargetType.AllAllies => cardOwner?.Creature,
            TargetType.Self => cardOwner?.Creature,
            TargetType.AnyPlayer => cardOwner?.Creature,
            _ => null
        };
        
        MainFile.Logger?.Info($"Finale: GetTargetForCard({card.GetType().Name}, {card.TargetType}) => {target?.GetType().Name ?? "null"}");
        return target;
    }

    protected override void OnUpgrade()
    {
        // 升级增加自动打出的牌数
        DynamicVars[Theresa.TheresaCode.Extensions.FinalePlayCountVar.Key].UpgradeValueBy(UpgradePlayCountBonus);
    }
}
