#!/usr/bin/env python3
"""
批量修复 STS2 mod 编译错误
主要修复 PowerCmd.Apply 和 PowerCmd.ModifyAmount 的签名变更
"""

import os
import re
from pathlib import Path

PROJECT_DIR = Path(r"C:\Users\admin\Desktop\Theresa\Theresa_BaseLibbeta\TheresaCode")

def has_choice_context_var(content: str, line_idx: int) -> str | None:
    """
    检查当前方法上下文中是否有可用的 choiceContext/context 变量
    返回变量名或 None
    """
    # 简单启发式：查找方法参数中是否有 PlayerChoiceContext 类型的参数
    # 向上查找方法定义
    lines = content.split('\n')
    for i in range(line_idx, max(0, line_idx - 50), -1):
        line = lines[i]
        if 'async Task' in line or 'public override' in line or 'private async' in line or 'public async' in line:
            # 找到了方法签名行
            if 'PlayerChoiceContext choiceContext' in line:
                return 'choiceContext'
            if 'PlayerChoiceContext context' in line:
                return 'context'
            break
    return None

def fix_powercmd_apply(content: str) -> str:
    """
    修复 PowerCmd.Apply 调用
    旧: PowerCmd.Apply<T>(target, amount, applier, cardSource)
    新: PowerCmd.Apply<T>(choiceContext, target, amount, applier, cardSource)
    """
    lines = content.split('\n')
    new_lines = []
    
    for idx, line in enumerate(lines):
        original = line
        
        # 匹配 PowerCmd.Apply<T>(...)
        # 需要处理可能跨多行的情况
        if 'PowerCmd.Apply<' in line and 'choiceContext' not in line:
            # 检查这一行和后面几行是否构成完整调用
            call_lines = [line]
            j = idx + 1
            while j < len(lines) and not line.strip().endswith(');') and not line.strip().endswith(')'):
                call_lines.append(lines[j])
                j += 1
            
            full_call = '\n'.join(call_lines)
            
            # 正则匹配: await PowerCmd.Apply<Type>(arg1, arg2, arg3, arg4)
            # 或: await PowerCmd.Apply<Type>(arg1, arg2, arg3, arg4, bool);
            pattern = r'(await\s+)?(PowerCmd\.Apply<[^>]+>)\(([^)]+)\)'
            match = re.search(pattern, full_call, re.DOTALL)
            
            if match:
                prefix = match.group(1) or ''
                method = match.group(2)
                args_str = match.group(3)
                
                # 解析参数（简单按逗号分割，不考虑嵌套括号）
                args = [a.strip() for a in args_str.split(',')]
                
                # 判断参数数量
                if len(args) == 4:
                    # 旧格式: target, amount, applier, cardSource
                    # 需要添加 choiceContext
                    ctx_var = has_choice_context_var(content, idx)
                    if ctx_var:
                        new_args = f"{ctx_var}, {args_str}"
                    else:
                        new_args = f"new ThrowingPlayerChoiceContext(), {args_str}"
                    
                    new_call = f"{prefix}{method}({new_args})"
                    full_call = full_call.replace(match.group(0), new_call)
                    
                    # 更新 lines
                    call_lines_new = full_call.split('\n')
                    for i, cl in enumerate(call_lines_new):
                        if idx + i < len(new_lines):
                            new_lines[idx + i - len(new_lines)] = cl
                        else:
                            new_lines.append(cl)
                    continue
                elif len(args) == 5:
                    # 可能是旧格式带 silent 参数: target, amount, applier, cardSource, silent
                    # 新格式: choiceContext, target, amount, applier, cardSource, silent
                    ctx_var = has_choice_context_var(content, idx)
                    if ctx_var:
                        new_args = f"{ctx_var}, {args_str}"
                    else:
                        new_args = f"new ThrowingPlayerChoiceContext(), {args_str}"
                    
                    new_call = f"{prefix}{method}({new_args})"
                    full_call = full_call.replace(match.group(0), new_call)
                    
                    call_lines_new = full_call.split('\n')
                    for i, cl in enumerate(call_lines_new):
                        if idx + i < len(new_lines):
                            new_lines[idx + i - len(new_lines)] = cl
                        else:
                            new_lines.append(cl)
                    continue
        
        new_lines.append(line)
    
    return '\n'.join(new_lines)

def fix_powercmd_modifyamount(content: str) -> str:
    """
    修复 PowerCmd.ModifyAmount 调用，添加 cardSource 参数
    """
    # 这个比较复杂，因为参数位置不固定，暂时跳过
    return content

def fix_combatstate_to_icombatstate(content: str) -> str:
    """
    修复方法签名中 CombatState 参数改为 ICombatState
    但只在 override 方法中需要改
    """
    # 这个需要更精确的处理，暂时跳过
    return content

def process_file(filepath: Path) -> bool:
    """处理单个文件，返回是否修改"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
    except Exception as e:
        print(f"Error reading {filepath}: {e}")
        return False
    
    original = content
    content = fix_powercmd_apply(content)
    # content = fix_powercmd_modifyamount(content)
    # content = fix_combatstate_to_icombatstate(content)
    
    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Fixed: {filepath}")
        return True
    return False

def main():
    modified_count = 0
    
    for root, dirs, files in os.walk(PROJECT_DIR):
        # 排除某些目录
        dirs[:] = [d for d in dirs if d not in ['.godot', 'addons']]
        
        for filename in files:
            if filename.endswith('.cs'):
                filepath = Path(root) / filename
                if process_file(filepath):
                    modified_count += 1
    
    print(f"\nTotal files modified: {modified_count}")

if __name__ == '__main__':
    main()
