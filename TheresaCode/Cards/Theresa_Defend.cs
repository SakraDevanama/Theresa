using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Theresa.TheresaCode.Character; 


namespace Theresa.TheresaCode.Cards;


[Pool(typeof(TheresaCardPool))]


public class TheresaDefend()
    : TheresaCardModel(1, CardType.Skill, CardRarity.Basic, TargetType.Self) // 定义TheresaDefend类，继承自TheresaCardModel
{
    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];
    
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [new BlockVar(5m, ValueProp.Move)];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) // 打出时的效果逻辑
    {
        // 播放自定义防御音效
        PlayCustomSfx();
        
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay); // 让玩家获得10点护甲
    }
    
    private void PlayCustomSfx()
    {
        var stream = GD.Load<AudioStream>("res://Theresa/audio/p_atk_drgndfmrtel.wav");
        if (stream != null)
        {
            var player = new AudioStreamPlayer();
            player.Stream = stream;
            if (Engine.GetMainLoop() is SceneTree sceneTree && sceneTree.Root != null)
            {
                sceneTree.Root.CallDeferred("add_child", player);
                player.Finished += () => player.QueueFree();
                player.CallDeferred("play");
            }
        }
    }

    protected override void OnUpgrade() // 重写OnUpgrade方法，处理卡牌升级
    {
        DynamicVars.Block.UpgradeValueBy(2m); 
    }
    
    
}