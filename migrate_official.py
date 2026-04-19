#!/usr/bin/env python3
"""
根据官方 0.103-0.105 迁移指南修复编译错误
"""

import os
import re
from pathlib import Path

PROJECT_DIR = Path(r"C:\Users\admin\Desktop\Theresa\Theresa_BaseLibbeta\TheresaCode")

def fix_file(filepath):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
    except:
        return False
    
    original = content
    
    # 1. IsInstanced -> InstanceType (如果还没修复)
    if 'bool IsInstanced => true' in content:
        content = content.replace('public override bool IsInstanced => true;', 
                                   'public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;')
    if 'bool IsInstanced => false' in content:
        content = content.replace('public override bool IsInstanced => false;', 
                                   'public override PowerInstanceType InstanceType => PowerInstanceType.None;')
    
    # 2. GetResultPileType -> GetResultPileTypeForCardPlay
    content = content.replace('protected override PileType GetResultPileType()', 
                               'protected override PileType GetResultPileTypeForCardPlay()')
    
    # 3. OnTurnEndInHand public -> protected
    content = re.sub(r'public override (async )?Task OnTurnEndInHand\(', 
                     r'protected override \1Task OnTurnEndInHand(', content)
    
    # 4. BeforePlayPhaseStart -> AfterAutoPrePlayPhaseEntered (如果存在)
    content = content.replace('BeforePlayPhaseStart(PlayerChoiceContext choiceContext, Player player)',
                               'AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)')
    content = content.replace('BeforePlayPhaseStartLate(PlayerChoiceContext choiceContext, Player player)',
                               'AfterAutoPrePlayPhaseEnteredLate(PlayerChoiceContext choiceContext, Player player)')
    
    # 5. CardPileCmd.AddGeneratedCardToCombat: false -> null, true -> Owner
    # 这个需要具体上下文，暂时不自动处理
    
    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        return True
    return False

def main():
    count = 0
    for root, dirs, files in os.walk(PROJECT_DIR):
        dirs[:] = [d for d in dirs if d not in ['.godot', 'addons']]
        for f in files:
            if f.endswith('.cs'):
                if fix_file(Path(root) / f):
                    count += 1
    print(f"Fixed {count} files")

if __name__ == '__main__':
    main()
