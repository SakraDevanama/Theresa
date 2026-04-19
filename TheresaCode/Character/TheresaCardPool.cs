using BaseLib.Abstracts;
using Theresa.TheresaCode.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Theresa.TheresaCode.Character;

public class TheresaCardPool : CustomCardPoolModel
{
    public override string Title => Theresa.CharacterId;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/energy.png".ImagePath();

    /* HSV值决定卡背颜色，当没有自定义卡框时使用 */
    public override float H => 1f;
    public override float S => 1f;
    public override float V => 1f;

    // 小卡片图标的颜色
    public override Color DeckEntryCardColor => new("ff0000");

    // 指示是否为无色卡牌
    public override bool IsColorless => false;
}
