using BaseLib.Abstracts;
using Theresa.TheresaCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Theresa.TheresaCode.Cards;
using Theresa.TheresaCode.Relics;
using Theresa.TheresaCode.Stances;

namespace Theresa.TheresaCode.Character;

public class Theresa : PlaceholderCharacterModel
{
    public const string CharacterId = "THERESA-THERESA";

    public static readonly Color Color = StsColors.pink;

    public override Color NameColor => StsColors.pink;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 85;
    
    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_slash", "vfx/vfx_attack_blunt", "vfx/vfx_heavy_blunt", 
            "vfx/vfx_bloody_impact", "vfx/vfx_rock_shatter"
        ];
    }

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<TheresaDefend>(),
        ModelDb.Card<TheresaDefend>(),
        ModelDb.Card<TheresaDefend>(),
        ModelDb.Card<TheresaDefend>(),
        ModelDb.Card<TheresaStrike>(),
        ModelDb.Card<TheresaStrike>(),
        ModelDb.Card<TheresaStrike>(),
        ModelDb.Card<TheresaStrike>(),
        ModelDb.Card<Mote>(),
        ModelDb.Card<Spot>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<MaoCrest>(),
        ModelDb.Relic<UnknownRelic>()
        
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<TheresaCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<TheresaRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<TheresaPotionPool>();

    public override string CustomIconTexturePath => "char_4134_icon.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_4134_cetsyr.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "char_4134_icon.png".CharacterUiPath();
    public override string CustomVisualPath => "res://Theresa/animations/characters/crature_visuals/silent.tscn";
    public override string CustomCharacterSelectBg => "res://Theresa/animations/characters/char_select/char_select_bg_theresa.tscn";
    public override string CustomCharacterSelectTransitionPath => "res://materials/transitions/ironclad_transition_mat.tres";
    public override string CustomMerchantAnimPath => "res://Theresa/animations/characters/merchant/TheresaMerchant.tscn";
    public override string CustomEnergyCounterPath => "res://Theresa/animations/characters/energy_counter/Theresa_energy_counter.tscn";
    public override string CustomRestSiteAnimPath => "res://Theresa/animations/characters/rest_site/characters/theresa_character_camp.tscn";
    public override string CustomIconPath => "res://Theresa/animations/characters/theresa_icon/Theresa_icon.tscn";
    // 多人模式-手指。
    public override string CustomArmPointingTexturePath => "multiplayer_hand_theresa_point.png".CharacterUiPath();
    // 多人模式剪刀石头布-石头。 
    public override string CustomArmRockTexturePath => "multiplayer_hand_theresa_rock.png".CharacterUiPath();
    // 多人模式剪刀石头布-布。 
    public override string CustomArmPaperTexturePath => "multiplayer_hand_theresa_paper.png".CharacterUiPath();
    // 多人模式剪刀石头布-剪刀。
    public override string CustomArmScissorsTexturePath => "multiplayer_hand_theresa_scissors.png".CharacterUiPath();
   
    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        bool IsGuardStance() => ResolveGuardStance(controller);

        AnimState idle = new("Idle", isLooping: true);
        AnimState guardIdle = new("Skill_1_Loop", isLooping: true);
        
        // 入场动画: Start -> Idle
        AnimState start = new("Start");
        start.NextState = idle;

        // Attack: Skill_2_Begin -> Skill_2_End -> Idle
        AnimState attackBegin = new("Skill_2_Begin");
        AnimState attackEnd = new("Skill_2_End");
        attackBegin.NextState = attackEnd;
        attackEnd.NextState = idle;

        // Divinity Attack: Skill_1_Begin -> Skill_2_End -> guardIdle
        AnimState divinityAttackBegin = new("Skill_1_Begin");
        AnimState divinityAttackEnd = new("Skill_2_End");
        divinityAttackBegin.NextState = divinityAttackEnd;
        divinityAttackEnd.NextState = guardIdle;

        // Cast: Skill_3_Begin -> Skill_3_End -> Idle
        AnimState castBegin = new("Skill_3_Begin");
        AnimState castEnd = new("Skill_3_End");
        castBegin.NextState = castEnd;
        castEnd.NextState = idle;
        
        AnimState hit = new("Stun_Begin");
        AnimState guardHit = new("Stun_Begin");
        AnimState dead = new("Die");
        AnimState relaxed = new("Stun", isLooping: true);

        hit.NextState = idle;
        guardHit.NextState = guardIdle;

        CreatureAnimator animator = new(start, controller);
        // 禁用 DivinityStance 动画切换，统一使用普通动画
        // animator.AddAnyState("Idle", guardIdle, IsGuardStance);
        // animator.AddAnyState("Idle", idle, () => !IsGuardStance());
        animator.AddAnyState("Dead", dead);
        // animator.AddAnyState("Hit", guardHit, IsGuardStance);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Attack", attackBegin);
        // animator.AddAnyState("Cast", guardCast, IsGuardStance);
        animator.AddAnyState("Cast", castBegin);
        animator.AddAnyState("Relaxed", relaxed);
        animator.AddAnyState("Revive", idle);
        return animator;
    }

    private static bool ResolveGuardStance(MegaSprite controller)
    {
        if (controller.BoundObject is not Node node)
            return false;

        Node? current = node;
        while (current != null)
        {
            if (current is NCreature nCreature)
                return nCreature.Entity.HasPower<DivinityStance>();

            current = current.GetParent();
        }

        return false;
    }
}