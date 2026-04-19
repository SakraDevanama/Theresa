#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
全面修复 STS2 API 变更
"""

import os
import re
from pathlib import Path

PROJECT_DIR = Path(r"C:\Users\admin\Desktop\Theresa\Theresa_BaseLibbeta\TheresaCode")

def read_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        return f.read()

def write_file(filepath, content):
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

def fix_powercmd_apply(content):
    """
    修复 PowerCmd.Apply<T>(target, amount, applier, cardSource) 
    → PowerCmd.Apply<T>(new ThrowingPlayerChoiceContext(), target, amount, applier, cardSource)
    
    但需要注意：
    1. 已经有 5+ 个参数的不要改
    2. 第5个参数是 bool 的（旧 silent 参数）要改成 cardSource=null
    """
    # 模式1: PowerCmd.Apply<Type>(target, amount, applier, null) 或类似 - 4个参数
    # 匹配: PowerCmd.Apply<...>(..., ..., ..., ...)
    # 但要确保不是已经有5+个参数的
    
    def replace_apply(match):
        full = match.group(0)
        # 检查是否已经有 new ThrowingPlayerChoiceContext 或 choiceContext
        if 'new ThrowingPlayerChoiceContext()' in full or 'choiceContext' in full:
            return full
        # 数参数个数
        args = match.group(2)
        arg_list = [a.strip() for a in args.split(',')]
        if len(arg_list) >= 5:
            # 已经有5+参数，可能已修复或是其他形式
            return full
        if len(arg_list) == 4:
            # 旧格式: target, amount, applier, cardSource
            return f"PowerCmd.Apply{match.group(1)}(new ThrowingPlayerChoiceContext(), {args})"
        return full
    
    # 匹配 PowerCmd.Apply<Type>(arg1, arg2, arg3, arg4) - 4个参数
    pattern = r'PowerCmd\.Apply(<[^>]+>)?\(([^)]+)\)'
    content = re.sub(pattern, replace_apply, content)
    return content

def fix_powercmd_apply_bool_fifth(content):
    """
    修复 PowerCmd.Apply<T>(..., ..., ..., ..., true/false) 
    旧API中第5个参数是 bool (silent)，新API中第5个是 CardModel?
    
    需要识别：Apply<T>(ctx/target, target/amount, amount/applier, applier/cardSource, bool)
    这种比较复杂，手动处理几个已知文件
    """
    return content

def fix_modifyamount(content):
    """
    修复 PowerCmd.ModifyAmount(power, amount, applier, cardSource)
    → PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), power, amount, applier, cardSource)
    
    以及缺少 cardSource 的：
    PowerCmd.ModifyAmount(ctx, power, amount, applier) 
    → PowerCmd.ModifyAmount(ctx, power, amount, applier, null)
    """
    # 先修复缺少 PlayerChoiceContext 的（4参数，第一个不是ctx）
    def replace_modify(match):
        full = match.group(0)
        args = match.group(1)
        arg_list = [a.strip() for a in args.split(',')]
        
        # 如果第一个参数是 new ThrowingPlayerChoiceContext 或 choiceContext，说明已有ctx
        first = arg_list[0].strip()
        if first in ('new ThrowingPlayerChoiceContext()', 'choiceContext', 'new GameActionPlayerChoiceContext(this)'):
            # 已有ctx，检查是否有5个参数
            if len(arg_list) == 4:
                # 缺少 cardSource
                return f"PowerCmd.ModifyAmount({args}, null)"
            return full
        
        # 没有ctx，4个参数
        if len(arg_list) == 4:
            return f"PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), {args})"
        
        return full
    
    pattern = r'PowerCmd\.ModifyAmount\(([^)]+)\)'
    content = re.sub(pattern, replace_modify, content)
    return content

def fix_cardpile_addgenerated(content):
    """
    CardPileCmd.AddGeneratedCardToCombat(card, pile, true/false) 
    → CardPileCmd.AddGeneratedCardToCombat(card, pile, player)
    
    CardPileCmd.AddGeneratedCardToCombat(card, pile) - 保持不变（可能已正确）
    
    需要看上下文判断 player 是谁。常见模式：
    - true → Owner
    - false → null
    """
    def replace_add(match):
        full = match.group(0)
        args = match.group(1)
        arg_list = [a.strip() for a in args.split(',')]
        
        if len(arg_list) == 3:
            third = arg_list[2].strip()
            if third == 'true':
                # 需要替换为 player，通常是 Owner
                return f"CardPileCmd.AddGeneratedCardToCombat({arg_list[0].strip()}, {arg_list[1].strip()}, Owner)"
            elif third == 'false':
                return f"CardPileCmd.AddGeneratedCardToCombat({arg_list[0].strip()}, {arg_list[1].strip()}, null)"
        return full
    
    pattern = r'CardPileCmd\.AddGeneratedCardToCombat\(([^)]+)\)'
    content = re.sub(pattern, replace_add, content)
    return content

def fix_icombatstate_cast(content):
    """
    某些方法需要 CombatState 而不是 ICombatState
    常见模式：将 ICombatState 变量强制转换为 CombatState
    """
    # 这个需要具体分析，暂时不自动处理
    return content

def fix_dustmanager_api(content):
    """
    DustManager API 变更：
    - IsDustCard(card) → DustManager.ContainsCard(card) 或检查 card.Enchantment
    - AddCardFromAction → 需要看上下文
    - DustItFromAction → 需要看上下文  
    - SelectDustCardAndTarget → 需要看上下文
    - DustItWithSelection → 需要看上下文
    - GetCardsForPlayer → DustManager.Cards
    """
    # GetCardsForPlayer(player) → Cards.ToList() (但需要过滤玩家)
    content = content.replace('DustManager.GetCardsForPlayer(player)', 
                               '[DustManager.Cards.Where(c => c.Owner == player).ToList()]')
    
    # 简单替换 IsDustCard
    content = content.replace('DustManager.IsDustCard(card)', 'DustManager.ContainsCard(card)')
    content = content.replace('DustManager.IsDustCard(', 'DustManager.ContainsCard(')
    
    return content

def process_file(filepath):
    content = read_file(filepath)
    original = content
    
    content = fix_powercmd_apply(content)
    content = fix_modifyamount(content)
    content = fix_cardpile_addgenerated(content)
    content = fix_dustmanager_api(content)
    
    if content != original:
        write_file(filepath, content)
        return True
    return False

def main():
    cs_files = list(PROJECT_DIR.rglob('*.cs'))
    count = 0
    for filepath in cs_files:
        if process_file(filepath):
            print(f"Fixed: {filepath.relative_to(PROJECT_DIR)}")
            count += 1
    print(f"\nTotal files modified: {count}")

if __name__ == '__main__':
    main()
