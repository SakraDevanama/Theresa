#!/usr/bin/env python3
"""
批量修复 Godot/STS2 mod 项目的编译错误
"""

import os
import re
import sys

BASE_DIR = r"C:\Users\admin\Desktop\Theresa\Theresa_BaseLibbeta\TheresaCode"

def read_file(path):
    with open(path, 'r', encoding='utf-8') as f:
        return f.read()

def write_file(path, content):
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

def has_choice_context_param(content):
    """检查文件是否有 choiceContext 参数可用"""
    return 'PlayerChoiceContext choiceContext' in content

def fix_power_cmd_apply(content, file_path):
    """修复 PowerCmd.Apply 调用"""
    original = content
    has_choice = has_choice_context_param(content)
    
    # Pattern 1: await PowerCmd.Apply<SomePower>(target, amount, applier, this);
    # -> await PowerCmd.Apply<SomePower>(choiceContext, target, amount, applier, this);
    # 或 -> await PowerCmd.Apply<SomePower>(new ThrowingPlayerChoiceContext(), target, amount, applier, this);
    
    # 先处理有泛型类型参数的 Apply
    def replace_apply_generic(match):
        power_type = match.group(1)
        args = match.group(2)
        # 解析参数
        parts = [p.strip() for p in args.split(',')]
        if len(parts) == 4:
            target, amount, applier, card_source = parts
            if has_choice:
                # 尝试找到最近的 choiceContext 变量名
                return f"PowerCmd.Apply<{power_type}>(choiceContext, {target}, {amount}, {applier}, {card_source})"
            else:
                return f"PowerCmd.Apply<{power_type}>(new ThrowingPlayerChoiceContext(), {target}, {amount}, {applier}, {card_source})"
        return match.group(0)
    
    # Pattern: PowerCmd.Apply<Type>(..., ..., ..., ...)
    content = re.sub(
        r'PowerCmd\.Apply<([^>]+)>\(([^)]+)\)',
        replace_apply_generic,
        content
    )
    
    # Pattern 2: await PowerCmd.Apply(target, amount, applier, cardSource) - 无泛型
    def replace_apply_nongeneric(match):
        args = match.group(1)
        parts = [p.strip() for p in args.split(',')]
        if len(parts) == 4:
            target, amount, applier, card_source = parts
            if has_choice:
                return f"PowerCmd.Apply(choiceContext, {target}, {amount}, {applier}, {card_source})"
            else:
                return f"PowerCmd.Apply(new ThrowingPlayerChoiceContext(), {target}, {amount}, {applier}, {card_source})"
        return match.group(0)
    
    content = re.sub(
        r'PowerCmd\.Apply\(([^)]+)\)',
        replace_apply_nongeneric,
        content
    )
    
    return content

def fix_power_cmd_modify_amount(content, file_path):
    """修复 PowerCmd.ModifyAmount 调用"""
    original = content
    has_choice = has_choice_context_param(content)
    
    def replace_modify(match):
        args = match.group(1)
        parts = [p.strip() for p in args.split(',')]
        if len(parts) == 4:
            power, amount, applier, card_source = parts
            if has_choice:
                return f"PowerCmd.ModifyAmount(choiceContext, {power}, {amount}, {applier}, {card_source})"
            else:
                return f"PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), {power}, {amount}, {applier}, {card_source})"
        elif len(parts) == 3:
            # 3 args: power, amount, applier (missing cardSource)
            power, amount, applier = parts
            if has_choice:
                return f"PowerCmd.ModifyAmount(choiceContext, {power}, {amount}, {applier}, null)"
            else:
                return f"PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), {power}, {amount}, {applier}, null)"
        return match.group(0)
    
    content = re.sub(
        r'PowerCmd\.ModifyAmount\(([^)]+)\)',
        replace_modify,
        content
    )
    
    return content

def fix_creature_cmd_gain_block(content, file_path):
    """修复 CreatureCmd.GainBlock 调用"""
    # Pattern: CreatureCmd.GainBlock(creature, amount, props, cardPlay)
    # -> CreatureCmd.GainBlock(creature, amount, props, cardPlay, false)
    def replace_gain_block(match):
        args = match.group(1)
        parts = [p.strip() for p in args.split(',')]
        if len(parts) == 4:
            return f"CreatureCmd.GainBlock({args}, false)"
        return match.group(0)
    
    content = re.sub(
        r'CreatureCmd\.GainBlock\(([^)]+)\)',
        replace_gain_block,
        content
    )
    return content

def fix_post_alternate_card_reward(content, file_path):
    """修复 PostAlternateCardRewardAction"""
    content = content.replace(
        'PostAlternateCardRewardAction.DismissScreenAndRemoveReward',
        'PostAlternateCardRewardAction.EndSelectionAndCompleteReward'
    )
    return content

def fix_card_reward_constructor(content, file_path):
    """修复 CardReward 构造函数"""
    # Pattern: new CardReward(new[] { card }, CardCreationSource.Other, Owner)
    # -> new CardReward(new[] { card }, CardCreationSource.Other, Owner, null, null)
    # 实际上需要 CardCreationOptions 和 PlayerChoiceSynchronizer
    
    # 先查找 CardReward 的构造
    def replace_card_reward(match):
        args = match.group(1)
        parts = [p.strip() for p in args.split(',')]
        if len(parts) == 3:
            cards, source, player = parts
            # 添加 null for rerollOptions and synchronizer
            return f"new CardReward({cards}, {source}, {player}, null, null)"
        return match.group(0)
    
    content = re.sub(
        r'new CardReward\(([^)]+)\)',
        replace_card_reward,
        content
    )
    return content

def fix_combat_state_param(content, file_path):
    """修复 CombatState 参数类型为 ICombatState"""
    # Pattern: private Creature? GetRandomEnemy(CombatState combatState)
    # -> private Creature? GetRandomEnemy(ICombatState combatState)
    content = re.sub(
        r'\(CombatState (\w+)\)',
        r'(ICombatState \1)',
        content
    )
    return content

def fix_damage_result_receiver(content, file_path):
    """修复 DamageResult.Receiver 访问 - AttackCommand.Results 现在是 IEnumerable<List<DamageResult>>"""
    # 这个需要手动处理，因为涉及嵌套循环的修改
    # 先标记出来
    return content

def process_file(file_path):
    """处理单个文件"""
    content = read_file(file_path)
    original = content
    
    content = fix_power_cmd_apply(content, file_path)
    content = fix_power_cmd_modify_amount(content, file_path)
    content = fix_creature_cmd_gain_block(content, file_path)
    content = fix_post_alternate_card_reward(content, file_path)
    content = fix_card_reward_constructor(content, file_path)
    content = fix_combat_state_param(content, file_path)
    
    if content != original:
        write_file(file_path, content)
        print(f"Fixed: {file_path}")
        return True
    return False

def main():
    fixed_count = 0
    
    for root, dirs, files in os.walk(BASE_DIR):
        # Skip .godot and other generated folders
        dirs[:] = [d for d in dirs if d not in ['.godot', 'temp_check']]
        
        for file in files:
            if file.endswith('.cs'):
                file_path = os.path.join(root, file)
                if process_file(file_path):
                    fixed_count += 1
    
    print(f"\nTotal files modified: {fixed_count}")

if __name__ == '__main__':
    main()
