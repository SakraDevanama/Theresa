#!/usr/bin/env python3
"""
STS2 API 迁移脚本 - 从旧版 API 迁移到新版 beta API
参考: STS2_WineFox 项目的代码模式
"""

import os
import re
from pathlib import Path

PROJECT_DIR = Path(r"C:\Users\admin\Desktop\Theresa\Theresa_BaseLibbeta\TheresaCode")

def find_method_context(lines, line_idx):
    """查找当前行所在的方法签名，返回是否有 PlayerChoiceContext 参数"""
    for i in range(line_idx, max(0, line_idx - 30), -1):
        line = lines[i]
        # 匹配方法签名
        if re.search(r'(public|protected|private|internal)\s+(override\s+)?(async\s+)?Task\s+\w+\s*\(', line):
            if 'PlayerChoiceContext choiceContext' in line:
                return 'choiceContext'
            if 'PlayerChoiceContext context' in line:
                return 'context'
            return None
    return None

def fix_file(filepath):
    """修复单个文件"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
    except Exception as e:
        print(f"Error reading {filepath}: {e}")
        return False
    
    original = content
    lines = content.split('\n')
    modified = False
    
    # 1. 修复 IsInstanced -> InstanceType
    if 'bool IsInstanced' in content:
        content = content.replace('public override bool IsInstanced => true;', 
                                   'public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;')
        content = content.replace('public override bool IsInstanced => false;', 
                                   'public override PowerInstanceType InstanceType => PowerInstanceType.None;')
        modified = True
    
    # 2. 修复方法签名 - 使用精确替换
    # BeforeSideTurnStart(CombatSide side, CombatState combatState)
    # -> BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)
    if 'BeforeSideTurnStart(CombatSide side, CombatState combatState)' in content:
        content = content.replace(
            'BeforeSideTurnStart(CombatSide side, CombatState combatState)',
            'BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)'
        )
        modified = True
    
    # AfterSideTurnStart(CombatSide side, CombatState combatState)
    # -> AfterSideTurnStart(CombatSide side, ICombatState combatState)
    if 'AfterSideTurnStart(CombatSide side, CombatState combatState)' in content:
        content = content.replace(
            'AfterSideTurnStart(CombatSide side, CombatState combatState)',
            'AfterSideTurnStart(CombatSide side, ICombatState combatState)'
        )
        modified = True
    
    # 3. 修复 PowerCmd.Apply 调用
    # 模式: PowerCmd.Apply<Type>(target, amount, applier, cardSource)
    # 新:   PowerCmd.Apply<Type>(choiceContext, target, amount, applier, cardSource)
    
    # 使用正则匹配单行调用
    apply_pattern = r'(await\s+)?PowerCmd\.Apply<([^>]+)>\(([^,)]+),\s*([^,)]+),\s*([^,)]+),\s*([^,)]+)\)'
    
    def replace_apply(match):
        prefix = match.group(1) or ''
        type_name = match.group(2)
        args = [match.group(i).strip() for i in range(3, 7)]
        
        # 检查参数中是否已经包含 choiceContext
        if any('choiceContext' in a or 'ThrowingPlayerChoiceContext' in a for a in args):
            return match.group(0)  # 已经修复过了
        
        # 查找上下文中的 choiceContext 变量
        # 简单启发式：如果这行代码在方法中，检查方法参数
        return f"{prefix}PowerCmd.Apply<{type_name}>(new ThrowingPlayerChoiceContext(), {', '.join(args)})"
    
    new_content = re.sub(apply_pattern, replace_apply, content)
    if new_content != content:
        content = new_content
        modified = True
    
    # 4. 修复带 silent 参数的 PowerCmd.Apply
    # 模式: PowerCmd.Apply<Type>(target, amount, applier, cardSource, silent)
    apply_silent_pattern = r'(await\s+)?PowerCmd\.Apply<([^>]+)>\(([^,)]+),\s*([^,)]+),\s*([^,)]+),\s*([^,)]+),\s*([^,)]+)\)'
    
    def replace_apply_silent(match):
        prefix = match.group(1) or ''
        type_name = match.group(2)
        args = [match.group(i).strip() for i in range(3, 8)]
        
        if any('choiceContext' in a or 'ThrowingPlayerChoiceContext' in a for a in args):
            return match.group(0)
        
        return f"{prefix}PowerCmd.Apply<{type_name}>(new ThrowingPlayerChoiceContext(), {', '.join(args)})"
    
    new_content = re.sub(apply_silent_pattern, replace_apply_silent, content)
    if new_content != content:
        content = new_content
        modified = True
    
    # 5. 修复 PowerCmd.ModifyAmount
    # 旧: PowerCmd.ModifyAmount(choiceContext, power, amount, applier)
    # 新: PowerCmd.ModifyAmount(choiceContext, power, amount, applier, cardSource)
    modify_pattern = r'PowerCmd\.ModifyAmount\(([^,]+),\s*([^,]+),\s*([^,]+),\s*([^)]+)\)'
    
    def replace_modify(match):
        args = [match.group(i).strip() for i in range(1, 5)]
        
        # 检查是否已经有5个参数
        if len([a for a in args if a]) == 5:
            return match.group(0)
        
        return f"PowerCmd.ModifyAmount({args[0]}, {args[1]}, {args[2]}, {args[3]}, null)"
    
    new_content = re.sub(modify_pattern, replace_modify, content)
    if new_content != content:
        content = new_content
        modified = True
    
    # 6. 修复 GetResultPileType -> GetResultPileTypeForCardPlay
    if 'override PileType GetResultPileType()' in content:
        content = content.replace(
            'override PileType GetResultPileType()',
            'override PileType GetResultPileTypeForCardPlay()'
        )
        modified = True
    
    if modified:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Fixed: {filepath}")
        return True
    return False

def main():
    modified_count = 0
    
    for root, dirs, files in os.walk(PROJECT_DIR):
        dirs[:] = [d for d in dirs if d not in ['.godot', 'addons']]
        
        for filename in files:
            if filename.endswith('.cs'):
                filepath = Path(root) / filename
                if fix_file(filepath):
                    modified_count += 1
    
    print(f"\nTotal files modified: {modified_count}")

if __name__ == '__main__':
    main()
