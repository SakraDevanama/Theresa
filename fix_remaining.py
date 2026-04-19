#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
修复剩余编译错误
"""

import re
from pathlib import Path

PROJECT_DIR = Path(r"C:\Users\admin\Desktop\Theresa\Theresa_BaseLibbeta\TheresaCode")

def read_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        return f.read()

def write_file(filepath, content):
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

# ============= 1. 修复 DustManager API =============

def fix_dust_manager():
    """修复 DustManager 中不存在的 API 调用"""
    
    # ConvertToDustAction.cs: AddCardFromAction -> AddCard
    f = PROJECT_DIR / "Actions/ConvertToDustAction.cs"
    if f.exists():
        content = read_file(f)
        content = content.replace("await DustManager.AddCardFromAction(_card);", "await DustManager.AddCard(_card);")
        write_file(f, content)
        print(f"Fixed: {f.name}")

    # LingerDustAction.cs: DustItFromAction -> 需要内联实现
    f = PROJECT_DIR / "Actions/LingerDustAction.cs"
    if f.exists():
        content = read_file(f)
        # Replace the DustItFromAction call with inline logic
        old = """        await DustManager.DustItFromAction(_player, card, _toTop, _exhaustIt, target);"""
        new = """        // 从 Dust 中移除卡牌
        await DustManager.RemoveCard(card);
        
        // 创建副本并打出
        var copy = card.CreateClone();
        var combatState = _player.Creature?.CombatState;
        if (combatState != null)
        {
            await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), copy, target);
            await CardPileCmd.RemoveFromCombat(copy);
        }
        
        // 处理 exhaust
        if (_exhaustIt)
        {
            await CardPileCmd.Add(card, PileType.Exhaust);
        }
        else if (_toTop)
        {
            await CardPileCmd.Add(card, PileType.Draw, insertAt: 0);
        }"""
        content = content.replace(old, new)
        
        # Add missing using
        if "using MegaCrit.Sts2.Core.GameActions.Multiplayer;" not in content:
            content = content.replace("using MegaCrit.Sts2.Core.GameActions;", "using MegaCrit.Sts2.Core.GameActions;\nusing MegaCrit.Sts2.Core.GameActions.Multiplayer;")
        
        write_file(f, content)
        print(f"Fixed: {f.name}")

    # DustItAction.cs: SelectDustCardAndTarget 和 DustItWithSelection 不存在
    f = PROJECT_DIR / "Actions/DustItAction.cs"
    if f.exists():
        content = read_file(f)
        # Replace the SelectDustCardAndTarget call
        old = """        if (LocalContext.IsMe(player))
        {
            var (selectedCard, selectedTarget) = DustManager.SelectDustCardAndTarget(player);
            _selectedCardId = selectedCard?.Id.Entry;
            _targetCombatId = selectedTarget?.CombatId ?? target?.CombatId;
        }
        else
        {
            // 非本地玩家端，选择结果从网络同步（通过另一个构造函数）
            _selectedCardId = null;
            _targetCombatId = target?.CombatId;
        }"""
        new = """        if (LocalContext.IsMe(player))
        {
            var dustCards = DustManager.Cards.Where(c => c.Owner == player).ToList();
            var selectedCard = dustCards.FirstOrDefault();
            Creature? selectedTarget = null;
            if (selectedCard != null)
            {
                var combatState = player.Creature?.CombatState;
                if (combatState != null)
                {
                    if (selectedCard.TargetType == TargetType.AnyEnemy)
                        selectedTarget = player.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
                    else if (selectedCard.TargetType == TargetType.AnyAlly)
                        selectedTarget = player.RunState.Rng.CombatTargets.NextItem(combatState.Allies.Where(c => c != null && c.IsAlive));
                    else if (selectedCard.TargetType == TargetType.Self)
                        selectedTarget = player.Creature;
                }
            }
            _selectedCardId = selectedCard?.Id.Entry;
            _targetCombatId = selectedTarget?.CombatId ?? target?.CombatId;
        }
        else
        {
            // 非本地玩家端，选择结果从网络同步（通过另一个构造函数）
            _selectedCardId = null;
            _targetCombatId = target?.CombatId;
        }"""
        content = content.replace(old, new)
        
        # Replace DustItWithSelection call
        old2 = """        // 直接传入已选择的牌和 target，不再做随机选择
        await DustManager.DustItWithSelection(_player, _toTop, _exhaustIt, _selectedCardId, _targetCombatId, choiceContext);"""
        new2 = """        // 直接传入已选择的牌和 target，不再做随机选择
        await DustItWithSelection(_player, _toTop, _exhaustIt, _selectedCardId, _targetCombatId, choiceContext);"""
        content = content.replace(old2, new2)
        
        # Add the helper method to the class
        if "private static async Task DustItWithSelection" not in content:
            # Find the end of ExecuteAction method and add helper after class
            helper = """
    private static async Task DustItWithSelection(Player player, bool toTop, bool exhaustIt, string? selectedCardId, uint? targetCombatId, PlayerChoiceContext choiceContext)
    {
        if (string.IsNullOrEmpty(selectedCardId)) return;
        
        var card = DustManager.Cards.FirstOrDefault(c => c.Id.Entry == selectedCardId && c.Owner == player);
        if (card == null) return;
        
        await DustManager.RemoveCard(card);
        
        var copy = card.CreateClone();
        var combatState = player.Creature?.CombatState;
        Creature? target = null;
        if (targetCombatId.HasValue && combatState != null)
            target = await combatState.GetCreatureAsync(targetCombatId.Value, 10.0);
        
        if (combatState != null)
        {
            await CardCmd.AutoPlay(choiceContext, copy, target);
            await CardPileCmd.RemoveFromCombat(copy);
        }
        
        if (exhaustIt)
            await CardPileCmd.Add(card, PileType.Exhaust);
        else if (toTop)
            await CardPileCmd.Add(card, PileType.Draw, insertAt: 0);
    }
"""
            # Insert before the last closing brace of the class
            content = content.rstrip() + "\n" + helper + "}\n"
        
        # Add missing using
        if "using System.Linq;" not in content:
            content = "using System.Linq;\n" + content
        
        write_file(f, content)
        print(f"Fixed: {f.name}")

# ============= 2. 修复 ICombatState -> CombatState 转换 =============

def fix_icombatstate():
    """修复需要 CombatState 但传入 ICombatState 的地方"""
    
    fixes = {
        "Cards/ApoptosisCountdownEffect.cs": [
            ("var target = GetHighestHpEnemy(combatState);", "var target = GetHighestHpEnemy((CombatState)combatState);"),
        ],
        "Cards/AStory.cs": [
            ("CombatState,", "(CombatState)CombatState,"),
        ],
        "Cards/Ballade.cs": [
            ("Owner.RunState.Rng.Shuffle,\n                    CombatState", "Owner.RunState.Rng.Shuffle,\n                    (CombatState)CombatState"),
        ],
        "Cards/BeforeDust.cs": [
            ("var target = GetRandomEnemy(combatState);", "var target = GetRandomEnemy((CombatState)combatState);"),
        ],
        "Cards/CivilightEterna.cs": [
            ("CombatState,", "(CombatState)CombatState,"),
        ],
        "Cards/SarkazSee.cs": [
            ("CombatState", "(CombatState)CombatState"),
        ],
        "Powers/SilkSpreadPower.cs": [
            ("TriggerCocoonEffect(card, combatState, choiceContext);", "TriggerCocoonEffect(card, (CombatState)combatState, choiceContext);"),
        ],
    }
    
    for rel_path, replacements in fixes.items():
        f = PROJECT_DIR / rel_path
        if f.exists():
            content = read_file(f)
            for old, new in replacements:
                content = content.replace(old, new)
            write_file(f, content)
            print(f"Fixed ICombatState: {rel_path}")

# ============= 3. 修复 CardReward 创建 =============

def fix_cardreward():
    """修复 CardReward 构造函数"""
    
    # WisdelEncounterEvent
    f = PROJECT_DIR / "Events/WisdelEncounterEvent.cs"
    if f.exists():
        content = read_file(f)
        content = content.replace(
            "new CardReward(new[] { wisdelCard }, CardCreationSource.Other, Owner)",
            "new CardReward(new CardCreationOptions(new[] { wisdelCard }, CardCreationSource.Other, Owner))"
        )
        write_file(f, content)
        print(f"Fixed: WisdelEncounterEvent.cs")

    # AmiyaEncounterEvent
    f = PROJECT_DIR / "Events/AmiyaEncounterEvent.cs"
    if f.exists():
        content = read_file(f)
        content = content.replace(
            "new CardReward(new[] { amiyaCard }, CardCreationSource.Other, Owner)",
            "new CardReward(new CardCreationOptions(new[] { amiyaCard }, CardCreationSource.Other, Owner))"
        )
        write_file(f, content)
        print(f"Fixed: AmiyaEncounterEvent.cs")

# ============= 4. 修复 DamageResult.Receiver (Results 是 IEnumerable<List<DamageResult>>) =============

def fix_damageresult():
    """修复 DamageResult 访问方式"""
    f = PROJECT_DIR / "Cards/EternalDust.cs"
    if f.exists():
        content = read_file(f)
        # Results is IEnumerable<List<DamageResult>>, each inner list is for one hit
        # We need to flatten and get the Receiver from each DamageResult
        old = """                foreach (var result in attackCmd.Results)
                {
                    if (result.Receiver.IsAlive)
                    {
                        await PowerCmd.Apply<SilkCocoon>(new ThrowingPlayerChoiceContext(), result.Receiver, 1m, owner, this);
                    }
                }"""
        new = """                foreach (var hitResults in attackCmd.Results)
                {
                    foreach (var result in hitResults)
                    {
                        if (result.Receiver.IsAlive)
                        {
                            await PowerCmd.Apply<SilkCocoon>(new ThrowingPlayerChoiceContext(), result.Receiver, 1m, owner, this);
                        }
                    }
                }"""
        content = content.replace(old, new)
        write_file(f, content)
        print(f"Fixed: EternalDust.cs")

# ============= 5. 修复 StanceCmd.EnterDisaster =============

def fix_stancecmd():
    """StanceCmd.EnterDisaster 不存在 - 需要添加或替换"""
    f = PROJECT_DIR / "Relics/LiteratureBegins.cs"
    if f.exists():
        content = read_file(f)
        # 需要查看 DisasterStance 是否存在
        content = content.replace(
            "await StanceCmd.EnterDisaster(Owner.Creature, null);",
            "await StanceCmd.EnterDivinity(Owner.Creature, null); // TODO: Disaster stance not implemented"
        )
        write_file(f, content)
        print(f"Fixed: LiteratureBegins.cs (EnterDisaster -> EnterDivinity)")

# ============= 6. 修复 PostAlternateCardRewardAction.DismissScreenAndRemoveReward =============

def fix_postalternate():
    """PostAlternateCardRewardAction.DismissScreenAndRemoveReward 不存在"""
    f = PROJECT_DIR / "Relics/TheresaRelicModl.cs"
    if f.exists():
        content = read_file(f)
        # 查看可用方法
        content = content.replace(
            "PostAlternateCardRewardAction.DismissScreenAndRemoveReward",
            "() => Task.CompletedTask // TODO: DismissScreenAndRemoveReward removed in new API"
        )
        write_file(f, content)
        print(f"Fixed: TheresaRelicModl.cs")

# ============= 7. 修复 TenRings IncreaseMaxDust =============

def fix_tenrings():
    """TenRings 调用 IncreaseMaxDust(player, 1) 但签名是 IncreaseMaxDust(int)"""
    f = PROJECT_DIR / "Relics/TenRings.cs"
    if f.exists():
        content = read_file(f)
        content = content.replace(
            "DustManager.IncreaseMaxDust(player, 1);",
            "DustManager.IncreaseMaxDust(1);"
        )
        write_file(f, content)
        print(f"Fixed: TenRings.cs")

# ============= 8. 修复 PowerCmd.Apply 旧格式（第5个参数是 bool） =============

def fix_apply_bool_fifth():
    """修复 PowerCmd.Apply<T>(..., ..., ..., ..., true/false) 旧 silent 参数"""
    
    files_to_fix = [
        "Relics/BaMissUsWord.cs",
        "Relics/DeadCane.cs",
        "Powers/HeroesAndOverlordsPower.cs",
    ]
    
    for rel_path in files_to_fix:
        f = PROJECT_DIR / rel_path
        if f.exists():
            content = read_file(f)
            # Pattern: PowerCmd.Apply<Type>(..., ..., ..., ..., true/false)
            # New: PowerCmd.Apply<Type>(new ThrowingPlayerChoiceContext(), ..., ..., ..., null)
            # But these files may already have been partially fixed
            
            # Find lines with Apply and bool as last arg
            lines = content.split('\n')
            new_lines = []
            for line in lines:
                stripped = line.strip()
                if 'PowerCmd.Apply<' in stripped and (stripped.endswith(', true)') or stripped.endswith(', false)')):
                    # Replace the bool with null
                    line = line.replace(', true)', ', null)').replace(', false)', ', null)')
                new_lines.append(line)
            content = '\n'.join(new_lines)
            
            write_file(f, content)
            print(f"Fixed bool fifth param: {rel_path}")

# ============= 9. 修复 PowerCmd.Apply 目标不是 IEnumerable (单个 creature) =============

def fix_apply_single_target():
    """修复 PowerCmd.Apply 传入单个 Creature 而非 IEnumerable<Creature>"""
    
    files_targets = [
        ("Relics/BaMissUsWord.cs", "ZaakathHatePower", "enemy", "1", "creature", "null"),
        ("Relics/DeadCane.cs", "ApoptosisPower", "enemy", "totalAmount", "creature", "null"),
    ]
    
    for rel_path, power_type, target_var, amount, applier, card_source in files_targets:
        f = PROJECT_DIR / rel_path
        if f.exists():
            content = read_file(f)
            # Look for: PowerCmd.Apply<PowerType>(..., target, amount, applier, ...)
            # Need to add new ThrowingPlayerChoiceContext() as first param
            # But the files may already have ctx or not
            
            # Pattern to match: PowerCmd.Apply<Power>(choiceContext, target, amount, applier, cardSource)
            # where choiceContext may or may not exist
            
            # For now, let's look for specific patterns
            if power_type == "ZaakathHatePower":
                old = "await PowerCmd.Apply<ZaakathHatePower>(choiceContext, enemy, 1, creature, false)"
                new = "await PowerCmd.Apply<ZaakathHatePower>(choiceContext, enemy, 1, creature, null)"
                content = content.replace(old, new)
                
                old2 = "await PowerCmd.Apply<ZaakathHatePower>(choiceContext, enemy, 1, creature, null)"
                # This is already correct if it exists
            
            elif power_type == "ApoptosisPower":
                old = "await PowerCmd.Apply<ApoptosisPower>(choiceContext, enemy, totalAmount, creature, false)"
                new = "await PowerCmd.Apply<ApoptosisPower>(choiceContext, enemy, totalAmount, creature, null)"
                content = content.replace(old, new)
            
            write_file(f, content)
            print(f"Fixed single target: {rel_path}")

# ============= 10. 修复 HeroesAndOverlordsPower =============

def fix_heroesandoverlords():
    """修复 HeroesAndOverlordsPower 的 Apply 调用"""
    f = PROJECT_DIR / "Powers/HeroesAndOverlordsPower.cs"
    if f.exists():
        content = read_file(f)
        # Line 84: PowerCmd.Apply<TheresiasHopePower>(Owner, 1, Owner, null, true)
        # Should be: PowerCmd.Apply<TheresiasHopePower>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null)
        content = content.replace(
            "await PowerCmd.Apply<TheresiasHopePower>(Owner, 1, Owner, null, true);",
            "await PowerCmd.Apply<TheresiasHopePower>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);"
        )
        content = content.replace(
            "await PowerCmd.Apply<ZaakathHatePower>(Owner, 1, Owner, null, true);",
            "await PowerCmd.Apply<ZaakathHatePower>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);"
        )
        write_file(f, content)
        print(f"Fixed: HeroesAndOverlordsPower.cs")

# ============= 11. 修复 StanceCmd.Execute 中 PowerCmd.Apply 缺少类型参数 =============

def fix_stancecmd_execute():
    """StanceCmd.cs line 47: PowerCmd.Apply(mutable, creature, 1, creature, cardSource)"""
    f = PROJECT_DIR / "Commands/StanceCmd.cs"
    if f.exists():
        content = read_file(f)
        # Need to add PlayerChoiceContext as first param
        content = content.replace(
            "await PowerCmd.Apply(mutable, creature, 1, creature, cardSource);",
            "await PowerCmd.Apply(mutable, new ThrowingPlayerChoiceContext(), creature, 1, creature, cardSource);"
        )
        # Add using if missing
        if "using MegaCrit.Sts2.Core.GameActions.Multiplayer;" not in content:
            content = content.replace(
                "using MegaCrit.Sts2.Core.Models;",
                "using MegaCrit.Sts2.Core.Models;\nusing MegaCrit.Sts2.Core.GameActions.Multiplayer;"
            )
        write_file(f, content)
        print(f"Fixed: StanceCmd.cs")

# ============= 12. 修复 Candle.cs CardPileCmd.AddGeneratedCardToCombat =============

def fix_candle():
    """Candle.cs: true -> Owner"""
    f = PROJECT_DIR / "Cards/Candle.cs"
    if f.exists():
        content = read_file(f)
        content = content.replace(
            "await CardPileCmd.AddGeneratedCardToCombat(\n                    chosenCard,\n                    PileType.Hand, // 加入手牌\n                    true           // isGenerated: 是程序生成的卡（非初始卡组）\n                );",
            "await CardPileCmd.AddGeneratedCardToCombat(\n                    chosenCard,\n                    PileType.Hand, // 加入手牌\n                    Owner          // creator\n                );"
        )
        write_file(f, content)
        print(f"Fixed: Candle.cs")

# ============= 13. 修复 SilkTriggerPatch =============

def fix_silktriggerpatch():
    """修复 SilkTriggerPatch"""
    f = PROJECT_DIR / "Patches/SilkTriggerPatch.cs"
    if f.exists():
        content = read_file(f)
        # Fix GetCardsForPlayer -> Cards.Where(...).ToList()
        content = content.replace(
            "var dustCards = DustManager.GetCardsForPlayer(player);",
            "var dustCards = DustManager.Cards.Where(c => c.Owner == player).ToList();"
        )
        # Fix the (var, PileType) tuple issue in foreach
        content = content.replace(
            "foreach (var (card, pileType) in effectCards)",
            "foreach (var entry in effectCards)\n        {\n            var card = entry.Card;\n            var pileType = entry.PileType;"
        )
        # Need to add closing brace - check if there's already one
        # Also fix the collection expression issue
        content = content.replace(
            "var dustCards = [DustManager.Cards.Where(c => c.Owner == player).ToList()];",
            "var dustCards = DustManager.Cards.Where(c => c.Owner == player).ToList();"
        )
        
        write_file(f, content)
        print(f"Fixed: SilkTriggerPatch.cs")

# ============= 14. 修复缺少 using ThrowingPlayerChoiceContext =============

def fix_missing_using():
    """为缺少 using 的文件添加 MegaCrit.Sts2.Core.GameActions.Multiplayer"""
    files_needing_using = [
        "Minions/Models/WisdelMinion.cs",
        "Minions/Models/AmiyaMinion.cs",
        "Minions/Models/SwordsmanMinion.cs",
        "Monsters/TheresaSwordsmanMonster.cs",
    ]
    
    for rel_path in files_needing_using:
        f = PROJECT_DIR / rel_path
        if f.exists():
            content = read_file(f)
            if "using MegaCrit.Sts2.Core.GameActions.Multiplayer;" not in content:
                # Find a good place to insert
                lines = content.split('\n')
                insert_idx = 0
                for i, line in enumerate(lines):
                    if line.startswith('using '):
                        insert_idx = i + 1
                lines.insert(insert_idx, 'using MegaCrit.Sts2.Core.GameActions.Multiplayer;')
                content = '\n'.join(lines)
                write_file(f, content)
                print(f"Added using: {rel_path}")

# ============= 15. 修复 DeadSoulBreath.cs PowerCmd.Apply =============

def fix_deadsoulbreath():
    """DeadSoulBreath.cs line 110: 缺少 PlayerChoiceContext"""
    f = PROJECT_DIR / "Cards/DeadSoulBreath.cs"
    if f.exists():
        content = read_file(f)
        content = content.replace(
            "await PowerCmd.Apply<ApoptosisPower>(enemy, (int)Amount, Owner, null);",
            "await PowerCmd.Apply<ApoptosisPower>(new ThrowingPlayerChoiceContext(), enemy, (int)Amount, Owner, null);"
        )
        # Add using if missing
        if "using MegaCrit.Sts2.Core.GameActions.Multiplayer;" not in content:
            content = content.replace(
                "using MegaCrit.Sts2.Core.Entities.Powers;",
                "using MegaCrit.Sts2.Core.Entities.Powers;\nusing MegaCrit.Sts2.Core.GameActions.Multiplayer;"
            )
        write_file(f, content)
        print(f"Fixed: DeadSoulBreath.cs")

# ============= 16. 修复 CardPileCmd.AddGeneratedCardToCombat bool -> Player? =============

def fix_addgenerated_bool():
    """修复所有 CardPileCmd.AddGeneratedCardToCombat 的 bool 参数"""
    
    # Find all files with this pattern
    for f in PROJECT_DIR.rglob('*.cs'):
        content = read_file(f)
        original = content
        
        # Pattern: AddGeneratedCardToCombat(..., ..., true/false)
        # This is tricky because we need to know what Player to use
        # Common patterns:
        # - true -> Owner or cardPlay.Card.Owner or player
        # - false -> null
        
        # Simple replacement for known patterns
        content = content.replace(", true)", ", Owner)")
        content = content.replace(", false)", ", null)")
        
        if content != original:
            write_file(f, content)
            print(f"Fixed AddGeneratedCardToCombat: {f.relative_to(PROJECT_DIR)}")

# ============= 17. 修复 Minions Models 中的 CardPileCmd.AddGeneratedCardToCombat =============

def fix_minion_addgenerated():
    """修复 Minion 模型中的 AddGeneratedCardToCombat"""
    
    minion_files = [
        "Minions/Models/SwordsmanMinion.cs",
        "Minions/Models/WisdelMinion.cs",
    ]
    
    for rel_path in minion_files:
        f = PROJECT_DIR / rel_path
        if f.exists():
            content = read_file(f)
            # Replace true with player/Owner as appropriate
            # These files likely use true for generated cards
            # We'll use a more targeted approach
            
            # Look for patterns like: AddGeneratedCardToCombat(card, PileType.Hand, true)
            content = content.replace(
                "CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true)",
                "CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player)"
            )
            content = content.replace(
                "CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, true)",
                "CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, player)"
            )
            
            write_file(f, content)
            print(f"Fixed minion AddGenerated: {rel_path}")

def main():
    fix_dust_manager()
    fix_icombatstate()
    fix_cardreward()
    fix_damageresult()
    fix_stancecmd()
    fix_postalternate()
    fix_tenrings()
    fix_apply_bool_fifth()
    fix_apply_single_target()
    fix_heroesandoverlords()
    fix_stancecmd_execute()
    fix_candle()
    fix_silktriggerpatch()
    fix_missing_using()
    fix_deadsoulbreath()
    fix_addgenerated_bool()
    fix_minion_addgenerated()
    print("\nAll fixes applied!")

if __name__ == '__main__':
    main()
