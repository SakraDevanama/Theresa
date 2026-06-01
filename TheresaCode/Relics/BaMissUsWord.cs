using BaseLib.Utils;



using MegaCrit.Sts2.Core.Combat;



using MegaCrit.Sts2.Core.Commands;



using MegaCrit.Sts2.Core.Entities.Creatures;



using MegaCrit.Sts2.Core.Entities.Players;



using MegaCrit.Sts2.Core.Entities.Relics;



using MegaCrit.Sts2.Core.GameActions.Multiplayer;



using MegaCrit.Sts2.Core.Localization;



using MegaCrit.Sts2.Core.Localization.DynamicVars;



using MegaCrit.Sts2.Core.Models;



using Theresa.TheresaCode.Character;



using Theresa.TheresaCode.Powers;







namespace Theresa.TheresaCode.Relics;







/// <summary>



/// 霸迩萨狂言 (BaMissUsWord)



/// Uncommon 遗物



/// 



/// 效果??


/// 1. 敌方获得恨意时，改为使所有敌人受到伤害??


/// 2. 每场战斗获得6层恨意??


/// 3. 回合结束时若你的恨意多于希望，给予所有敌??层恨意??


/// </summary>



[Pool(typeof(TheresaRelicPool))]



public sealed class BaMissUsWord : TheresaRelicModel



{



    public override RelicRarity Rarity => RelicRarity.Shop;







    /// <summary>



    /// 战斗开始时：获??层恨??


    /// </summary>



    public override async Task BeforeCombatStart()



    {



        if (Owner?.Creature == null) return;







        Flash();



        await PowerCmd.Apply<ZaakathHatePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 6, Owner.Creature, null);



    }







    /// <summary>



    /// 回合结束时：若恨意多于希望，给予所有敌??层恨??


    /// </summary>



    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)



    {



        if (Owner == null || side != CombatSide.Player)



            return;







        var creature = Owner.Creature;



        if (creature == null) return;







        var hatePower = creature.Powers.FirstOrDefault(p => p is ZaakathHatePower) as ZaakathHatePower;



        var hopePower = creature.Powers.FirstOrDefault(p => p is TheresiasHopePower) as TheresiasHopePower;







        int hateAmount = hatePower?.GetEffectiveAmount() ?? 0;



        int hopeAmount = hopePower?.GetEffectiveAmount() ?? 0;







        // 如果恨意 > 希望，给予所有敌??层恨??


        if (hateAmount > hopeAmount)



        {



            Flash();







            var enemies = creature.CombatState?.Enemies.Where(e => e.IsAlive).ToList();



            if (enemies != null)



            {



                foreach (var enemy in enemies)



                {



                    await PowerCmd.Apply<ZaakathHatePower>(choiceContext, enemy, 1, creature, null);



                }



            }



        }



    }



}



