#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复 STS2 API 签名变更 - 保留 UTF-8 编码
"""

import os
from pathlib import Path

PROJECT_DIR = Path(r"C:\Users\admin\Desktop\Theresa\Theresa_BaseLibbeta\TheresaCode")

# 定义需要修复的文件和对应的替换
FIXES = {
    # IsInstanced -> InstanceType
    "Powers/CardsDrawnCounterPower.cs": [
        ("public override bool IsInstanced => true;", 
         "public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;")
    ],
    "Powers/EnergyReboundPower.cs": [
        ("public override bool IsInstanced => true;", 
         "public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;")
    ],
    "Powers/HeroesAndOverlordsPower.cs": [
        ("public override bool IsInstanced => false;", 
         "public override PowerInstanceType InstanceType => PowerInstanceType.None;")
    ],
    "Powers/ThousandsWishPower.cs": [
        ("public override bool IsInstanced => true;", 
         "public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;")
    ],
    
    # GetResultPileType -> GetResultPileTypeForCardPlay
    "Cards/Cure.cs": [
        ("override PileType GetResultPileType()", 
         "override PileType GetResultPileTypeForCardPlay()")
    ],
    
    # BeforeSideTurnStart: CombatState -> ICombatState
    "Stances/DivinityStance.cs": [
        ("BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,\n        CombatState combatState)", 
         "BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,\n        ICombatState combatState)")
    ],
    "Minions/Powers/WisdelAutoAttackPower.cs": [
        ("BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)", 
         "BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)")
    ],
    "Minions/Powers/WisdelDawnChargePower.cs": [
        ("BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)", 
         "BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)")
    ],
    "Minions/Powers/WisdelDawnChargeGiverPower.cs": [
        ("BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, CombatState combatState)", 
         "BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)")
    ],
    
    # AfterPowerAmountChanged: 添加 PlayerChoiceContext
    "Cards/DustOfThePast.cs": [
        ("AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)", 
         "AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)")
    ],
    "Cards/PainfulConnection.cs": [
        ("AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)", 
         "AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)")
    ],
    "Powers/ApoptosisBurstPower.cs": [
        ("AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)", 
         "AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)")
    ],
    "Powers/Broken.cs": [
        ("AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)", 
         "AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)")
    ],
    "Powers/HeroesAndOverlordsPower.cs": [
        ("AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)", 
         "AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)")
    ],
    "Powers/MantraPower.cs": [
        ("AfterPowerAmountChanged(\n        PowerModel power,\n        decimal amount,\n        Creature? applier,\n        CardModel? cardSource)", 
         "AfterPowerAmountChanged(PlayerChoiceContext choiceContext,\n        PowerModel power,\n        decimal amount,\n        Creature? applier,\n        CardModel? cardSource)")
    ],
    "Powers/OblivionPower.cs": [
        ("AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)", 
         "AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)")
    ],
    "Powers/SilkCocoonBinding.cs": [
        ("AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)", 
         "AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)")
    ],
    "Powers/TheresiasHopePower.cs": [
        ("AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)", 
         "AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)")
    ],
    "Powers/WeaveTomorrowEffect.cs": [
        ("AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)", 
         "AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)")
    ],
    "Powers/ZaakathHatePower.cs": [
        ("AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)", 
         "AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)")
    ],
    "Powers/test.cs": [
        ("AfterPowerAmountChanged(\n        PowerModel power,           // 发生变化的能力实例\n        decimal amount,             // 变化后的总层数\n        Creature? applier,          // 施加该能力的来源生物（通常是玩家自己）\n        CardModel? cardSource)      // 触发该变化的卡牌（可能为 null）", 
         "AfterPowerAmountChanged(PlayerChoiceContext choiceContext,\n        PowerModel power,           // 发生变化的能力实例\n        decimal amount,             // 变化后的总层数\n        Creature? applier,          // 施加该能力的来源生物（通常是玩家自己）\n        CardModel? cardSource)      // 触发该变化的卡牌（可能为 null）")
    ],
    
    # OnTurnEndInHand public -> protected
    "Cards/WhereIsHome.cs": [
        ("public override async Task OnTurnEndInHand(", 
         "protected override async Task OnTurnEndInHand(")
    ],
}

def fix_file(filepath, replacements):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
    except Exception as e:
        print(f"Error reading {filepath}: {e}")
        return False
    
    original = content
    for old, new in replacements:
        if old in content:
            content = content.replace(old, new)
    
    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Fixed: {filepath}")
        return True
    return False

def main():
    count = 0
    for rel_path, replacements in FIXES.items():
        filepath = PROJECT_DIR / rel_path
        if filepath.exists():
            if fix_file(filepath, replacements):
                count += 1
        else:
            print(f"Not found: {filepath}")
    print(f"\nTotal files modified: {count}")

if __name__ == '__main__':
    main()
