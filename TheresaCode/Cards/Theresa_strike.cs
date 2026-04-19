using BaseLib.Utils;
using Godot; // 引入BaseLib工具类
using MegaCrit.Sts2.Core.Commands; // 引入游戏命令相关类
using MegaCrit.Sts2.Core.Entities.Cards; // 引入卡牌实体相关类
using MegaCrit.Sts2.Core.GameActions.Multiplayer; // 引入多人游戏动作相关类
using MegaCrit.Sts2.Core.Localization.DynamicVars; // 引入动态变量本地化相关类
using MegaCrit.Sts2.Core.ValueProps; // 引入值属性相关类
using Theresa.TheresaCode.Character; // 引入Theresa角色相关类


namespace Theresa.TheresaCode.Cards; // 定义命名空间

[Pool(typeof(TheresaCardPool))]

public  class TheresaStrike()
    : TheresaCardModel(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [new DamageVar(6, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 播放自定义攻击音效
        PlayCustomSfx();
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .WithAttackerFx(sfx: null) // 禁用原版攻击者音效（只保留自定义音效）
            .Execute(choiceContext);
    }
    
    private void PlayCustomSfx()
    {
        // 加载音频流
        var stream = GD.Load<AudioStream>("res://Theresa/audio/TheresaStrike.wav");
        if (stream == null)
        {
            GD.PrintErr("[TheresaStrike] Failed to load audio stream from res://Theresa/audio/TheresaStrike.wav");
            return;
        }
        
        GD.Print($"[TheresaStrike] Loaded audio stream: {stream.GetType().Name}");
        
        var player = new AudioStreamPlayer();
        player.Stream = stream;
        player.VolumeDb = 0; // 确保音量正常
        
        if (Engine.GetMainLoop() is SceneTree sceneTree && sceneTree.Root != null)
        {
            sceneTree.Root.CallDeferred("add_child", player);
            player.Finished += () => player.QueueFree();
            player.CallDeferred("play");
            
        }
        else
        {
            GD.PrintErr("[TheresaStrike] Failed to get scene tree");
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}