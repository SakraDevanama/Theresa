import re, os

def find_matching_paren(s, start):
    if s[start] != '(':
        return -1
    depth = 1
    i = start + 1
    while i < len(s) and depth > 0:
        if s[i] == '(':
            depth += 1
        elif s[i] == ')':
            depth -= 1
        elif s[i] == '"':
            i += 1
            while i < len(s) and s[i] != '"':
                if s[i] == '\\':
                    i += 1
                i += 1
        elif s[i] == "'":
            i += 1
            while i < len(s) and s[i] != "'":
                if s[i] == '\\':
                    i += 1
                i += 1
        i += 1
    return i - 1 if depth == 0 else -1

def split_args(args_str):
    args = []
    current = ''
    depth = 0
    i = 0
    while i < len(args_str):
        c = args_str[i]
        if c == '(' or c == '<' or c == '[':
            depth += 1
            current += c
        elif c == ')' or c == '>' or c == ']':
            depth -= 1
            current += c
        elif c == ',' and depth == 0:
            args.append(current.strip())
            current = ''
        else:
            current += c
        i += 1
    if current.strip():
        args.append(current.strip())
    return args

def fix_powercmd_apply(content):
    result = []
    i = 0
    while i < len(content):
        if content[i:i+17] == 'PowerCmd.Apply<' or content[i:i+14] == 'PowerCmd.Apply':
            paren_start = content.find('(', i)
            if paren_start == -1:
                result.append(content[i])
                i += 1
                continue
            paren_end = find_matching_paren(content, paren_start)
            if paren_end == -1:
                result.append(content[i])
                i += 1
                continue
            
            args_str = content[paren_start+1:paren_end]
            args = split_args(args_str)
            
            if len(args) == 4 and not args[0].startswith('new ThrowingPlayerChoiceContext') and not args[0] == 'choiceContext':
                new_args = ['new ThrowingPlayerChoiceContext()'] + args
                new_call = content[i:paren_start+1] + ', '.join(new_args) + content[paren_end]
                result.append(new_call)
                i = paren_end + 1
                continue
            elif len(args) == 5 and not args[0].startswith('new ThrowingPlayerChoiceContext') and not args[0] == 'choiceContext':
                new_args = ['new ThrowingPlayerChoiceContext()'] + args
                new_call = content[i:paren_start+1] + ', '.join(new_args) + content[paren_end]
                result.append(new_call)
                i = paren_end + 1
                continue
        
        result.append(content[i])
        i += 1
    return ''.join(result)

def fix_powercmd_modifyamount(content):
    result = []
    i = 0
    while i < len(content):
        if content[i:i+21] == 'PowerCmd.ModifyAmount':
            paren_start = content.find('(', i)
            if paren_start == -1:
                result.append(content[i])
                i += 1
                continue
            paren_end = find_matching_paren(content, paren_start)
            if paren_end == -1:
                result.append(content[i])
                i += 1
                continue
            
            args_str = content[paren_start+1:paren_end]
            args = split_args(args_str)
            
            if len(args) == 4 and not args[0].startswith('new ThrowingPlayerChoiceContext') and not args[0] == 'choiceContext':
                new_args = ['new ThrowingPlayerChoiceContext()'] + args + ['false']
                new_call = content[i:paren_start+1] + ', '.join(new_args) + content[paren_end]
                result.append(new_call)
                i = paren_end + 1
                continue
            elif len(args) == 4 and (args[0].startswith('new ThrowingPlayerChoiceContext') or args[0] == 'choiceContext'):
                new_args = args + ['null', 'false']
                new_call = content[i:paren_start+1] + ', '.join(new_args) + content[paren_end]
                result.append(new_call)
                i = paren_end + 1
                continue
            elif len(args) == 5 and (args[0].startswith('new ThrowingPlayerChoiceContext') or args[0] == 'choiceContext'):
                new_args = args + ['false']
                new_call = content[i:paren_start+1] + ', '.join(new_args) + content[paren_end]
                result.append(new_call)
                i = paren_end + 1
                continue
        
        result.append(content[i])
        i += 1
    return ''.join(result)

count = 0
for root, dirs, files in os.walk('TheresaCode'):
    for name in files:
        if not name.endswith('.cs'): continue
        path = os.path.join(root, name)
        try:
            with open(path, 'r', encoding='utf-8') as f:
                content = f.read()
        except:
            continue
        
        new_content = fix_powercmd_apply(content)
        new_content = fix_powercmd_modifyamount(new_content)
        
        if new_content != content:
            with open(path, 'w', encoding='utf-8') as f:
                f.write(new_content)
            count += 1
            print(f'Fixed: {path}')

print(f'Done. {count} files fixed.')
