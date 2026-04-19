using BaseLib.Utils;



using MegaCrit.Sts2.Core.Combat;



using MegaCrit.Sts2.Core.Commands;



using MegaCrit.Sts2.Core.Entities.Cards;



using MegaCrit.Sts2.Core.Entities.Creatures;



using MegaCrit.Sts2.Core.Entities.Players;



using MegaCrit.Sts2.Core.Entities.Relics;



using MegaCrit.Sts2.Core.GameActions.Multiplayer;



using MegaCrit.Sts2.Core.Localization;



using MegaCrit.Sts2.Core.Localization.DynamicVars;



using MegaCrit.Sts2.Core.Models;



using MegaCrit.Sts2.Core.Rooms;



using MegaCrit.Sts2.Core.ValueProps;



using Theresa.TheresaCode.Character;



using Theresa.TheresaCode.Powers;







namespace Theresa.TheresaCode.Relics;







/// <summary>



/// 食腐者手??(DeadCane)



/// Boss 遗物



/// 



/// 效果??


/// 1. 在你回合开始时，给予所有敌??3 层凋亡。你在上一回合每造成??次伤害，额外给予 1 层??


/// 2. 每当一名敌人死亡后，你回复 1 点生命??


/// </summary>



[Pool(typeof(TheresaRelicPool))]



public sealed class DeadCane : TheresaRelicModel



{



    public override RelicRarity Rarity => RelicRarity.Shop;







    // 记录上一回合造成的伤害次??


    private int _lastTurnDamageCount = 0;



    // 记录当前回合造成的伤害次??


    private int _currentTurnDamageCount = 0;







    /// <summary>



    /// 战斗开始时：重置计数器



    /// </summary>



    public override Task BeforeCombatStart()



    {



        _lastTurnDamageCount = 0;



        _currentTurnDamageCount = 0;



        return Task.CompletedTask;



    }







    /// <summary>



    /// 玩家回合开始时：给予所有敌人凋亡，然后重置计数??


    /// </summary>



    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)



    {



        if (Owner == null || player == null || player.NetId != Owner.NetId)



            return;







        var creature = player.Creature;



        if (creature == null) return;







        var combatState = creature.CombatState;



        if (combatState == null) return;







        // 基础 3 ??+ 上一回合伤害次数



        int baseAmount = 3;



        int bonusAmount = _lastTurnDamageCount;



        int totalAmount = baseAmount + bonusAmount;







        Flash();







        // 给予所有存活敌人凋??


        var enemies = combatState.Enemies.Where(e => e.IsAlive).ToList();



        foreach (var enemy in enemies)



        {



            await PowerCmd.Apply<ApoptosisPower>(choiceContext, enemy, totalAmount, creature, null);



        }







        // 重置计数器：当前回合变为上一回合



        _lastTurnDamageCount = _currentTurnDamageCount;



        _currentTurnDamageCount = 0;



    }







    /// <summary>



    /// 造成伤害后：记录伤害次数（只对非玩家目标??


    /// </summary>



    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)



    {



        // 只统计玩家造成的伤害，且目标不是玩??


        if (dealer?.Side != CombatSide.Player || target.IsPlayer)



            return Task.CompletedTask;







        // 只统计实际造成伤害的（??伤害??


        if (result.TotalDamage > 0)



        {



            _currentTurnDamageCount++;



        }







        return Task.CompletedTask;



    }







    /// <summary>



    /// 敌人死亡后：回复 1 点生??


    /// </summary>



    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)



    {



        // 只处理敌方死??


        if (target.IsPlayer || Owner?.Creature == null)



            return;







        // 如果死亡被阻止（如不死图腾），不触发



        if (wasRemovalPrevented)



            return;







        Flash();



        await CreatureCmd.Heal(Owner.Creature, 1);



    }







    /// <summary>



    /// 战斗结束时：重置计数??


    /// </summary>



    public override Task AfterCombatEnd(CombatRoom room)



    {



        _lastTurnDamageCount = 0;



        _currentTurnDamageCount = 0;



        return Task.CompletedTask;



    }



}



