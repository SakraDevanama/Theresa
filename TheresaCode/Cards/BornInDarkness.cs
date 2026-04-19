using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Theresa.TheresaCode.Character;
using Theresa.TheresaCode.Powers;

namespace Theresa.TheresaCode.Cards;

/// <summary>
/// 自定义动态变量：恨意层数
/// </summary>
public class ZaakathHateVar(int baseValue) : DynamicVar(Key, baseValue)
{
    public const string Key = "ZaakathHatePower";
}

/// 生于黑夜
/// 0费
/// 获得1层恨意（+1）
/// 若持有MemorySilk，额外触发：消耗一层MemorySilk，移除3层ZaakathHatePower，获得1层TheresiasHopePower
[Pool(typeof(TheresaCardPool))]
public class BornInDarkness() : TheresaCardModel(0, CardType.Skill, CardRarity.Common, TargetType.None)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ZaakathHatePower>(),
        HoverTipFactory.FromPower<TheresiasHopePower>(),
    ];
    
    // 注册动态变量
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ZaakathHateVar(IsUpgraded ? 2 : 1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner.Creature;
        
        // 播放施法动画
        await CreatureCmd.TriggerAnim(owner, "Cast", Owner.Character.CastAnimDelay);
        
        // 基础效果：获得1层恨意
        int hateAmount = IsUpgraded ? 2 : 1;
        await PowerCmd.Apply<ZaakathHatePower>(owner, hateAmount, owner, this);

        // 检查是否持有MemorySilk
        var memorySilk = owner.Powers.FirstOrDefault(p => p is MemorySilk) as MemorySilk;
        if (memorySilk != null)
        {
            // 额外触发：消耗一层MemorySilk
            await PowerCmd.ModifyAmount(memorySilk, -1, owner, this);

            // 移除3层ZaakathHatePower
            var hatePower = owner.Powers.FirstOrDefault(p => p is ZaakathHatePower) as ZaakathHatePower;
            if (hatePower != null)
            {
                int removeAmount = Math.Min(hatePower.Amount, 3);
                if (removeAmount > 0)
                {
                    await PowerCmd.ModifyAmount(hatePower, -removeAmount, owner, this);
                }
            }

            // 获得1层TheresiasHopePower
            await PowerCmd.Apply<TheresiasHopePower>(owner, 1, owner, this);
        }
    }
    
    protected override void OnUpgrade()
    {
        // 升级 "ZaakathHatePower" 关联的动态变量的值，基础+1
        // 根据卡牌描述，升级后从1层变为2层
        DynamicVars[ZaakathHateVar.Key].UpgradeValueBy(1);
    }
}