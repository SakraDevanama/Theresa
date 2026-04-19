// 引入 Godot 引擎核心命名空间

using Godot;

// 命名空间：守望者角色的姿态视觉特效模块
namespace Theresa.TheresaCode.Stances.vfx;

// 标记为 [GlobalClass]，使其在 Godot 编辑器中可被识别为自定义节点类型
[GlobalClass]
// 继承自 Node2D，表示这是一个 2D 空间中的节点，用于管理多个“眼睛”粒子
public partial class DivinityEyeSpawner : Node2D
{
    // 每隔 0.2 秒生成一个新地漂浮眼睛
    private const float SpawnInterval = 0.2f;

    // 存储当前所有活跃眼睛的数据（位置、颜色、生命周期等）
    private readonly List<EyeData> _eyes = new();

    // 动画帧数组：从精灵图集（spritesheet）中切出的 7 帧眼睛动画
    private AtlasTexture[] _frames = null!;

    // 混合材质：使用“相加”混合模式（Additive），使眼睛发光、半透明叠加更自然
    private CanvasItemMaterial _mat = null!;

    // 随机数生成器，用于随机位置、颜色、大小等
    private RandomNumberGenerator _rng = null!;

    // 全局特效缩放系数（来自 StanceVfx.VfxScale），适配不同分辨率或 UI 缩放
    private float _s;

    // 计时器：累计时间，用于控制生成间隔
    private float _spawnTimer;

    // 标志位：当设为 true 时，停止生成新眼睛，并在现有眼睛消失后自动销毁本节点
    private bool _stopping;

    // 外部调用此方法可立即停止生成新眼睛（由 StancePower.RemoveAura 触发）
    public void StopSpawning()
    {
        _stopping = true;
    }

    // Godot 生命周期方法：节点首次进入场景树时调用（初始化）
    public override void _Ready()
    {
        // 获取全局特效缩放比例
        _s = StanceVfx.VfxScale;
        // 同步调整本节点的初始位置（适配缩放）
        Position *= _s;

        // 初始化随机数生成器并种子化（确保每次效果不同）
        _rng = new RandomNumberGenerator();
        _rng.Randomize();

        // 创建相加混合材质（Additive Blend），让眼睛看起来像发光体
        _mat = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };

        // 加载眼睛动画的精灵图集（1 行 7 列，每格 64x64）
        var strip = GD.Load<Texture2D>("res://Theresa/images/vfx/eye_anim.png");

        // 初始化 7 帧 AtlasTexture
        _frames = new AtlasTexture[7];
        for (var i = 0; i < 7; i++)
        {
            _frames[i] = new AtlasTexture();
            _frames[i].Atlas = strip;                  // 指向整张图集
            _frames[i].Region = new Rect2(i * 64, 0, 64, 64); // 切出第 i 帧
        }

        // 预生成 3 个眼睛（带随机“已存在时间”），让特效一出现就有内容，避免空窗期
        for (var i = 0; i < 3; i++)
        {
            var preAge = _rng.RandfRange(0f, 2.0f); // 随机年龄（0~2秒）
            SpawnEye(preAge);
        }
    }

    // Godot 生命周期方法：每帧调用，用于更新眼睛动画和生成逻辑
    public override void _Process(double delta)
    {
        var dt = (float)delta; // 将帧时间转为 float

        // 如果未停止生成，则尝试按间隔生成新眼睛
        if (!_stopping)
        {
            _spawnTimer += dt;
            // 使用 while 循环处理帧率波动（可能一帧内需要生成多个）
            while (_spawnTimer >= SpawnInterval)
            {
                _spawnTimer -= SpawnInterval;
                SpawnEye(0f); // 新眼睛从 0 岁开始
            }
        }
        // 如果已停止生成，且所有眼睛都消失了，就销毁自己
        else if (_eyes.Count == 0)
        {
            QueueFree();
            return;
        }

        // 从后往前遍历眼睛列表（避免删除时索引错乱）
        for (var i = _eyes.Count - 1; i >= 0; i--)
        {
            var e = _eyes[i];
            e.Age += dt; // 增加年龄

            // 如果寿命到了，销毁 Sprite 并从列表移除
            if (e.Age >= e.Lifetime)
            {
                e.Sprite.QueueFree();
                _eyes.RemoveAt(i);
                continue;
            }

            // 计算当前动画进度（0.0 ~ 1.0）
            var progress = e.Age / e.Lifetime;

            // 根据进度选择当前应显示的帧
            var frame = GetEyeFrame(progress);
            if (frame >= 0 && frame < _frames.Length)
                e.Sprite.Texture = _frames[frame];

            // 计算垂直方向的“浮动”偏移（模拟漂浮感）
            var bob = GetVerticalBob(progress, e.Scale);
            e.Sprite.Position = new Vector2(e.Sprite.Position.X, e.StartY + bob);

            // 计算透明度（Alpha）：
            // - 前半段：淡入（0 → 1）
            // - 后半段：淡出（1 → 0）
            float alpha;
            if (progress < 0.5f)
                alpha = progress * 2f;
            else
                alpha = (1f - progress) * 2f;

            // 使用 smoothstep 函数让淡入淡出更平滑（S 形曲线）
            alpha = alpha * alpha * (3f - 2f * alpha);

            // 应用带透明度的颜色（保留原始 RGB，只改 Alpha）
            e.Sprite.Modulate = new Color(e.BaseColor.R, e.BaseColor.G, e.BaseColor.B, alpha);

            // 更新列表中的数据（结构体是值类型，需重新赋值）
            _eyes[i] = e;
        }
    }

    // 根据动画进度返回对应的帧索引（实现眼睛“眨动”效果）
    private static int GetEyeFrame(float progress)
    {
        // 前进阶段：睁眼
        if (progress < 0.15f) return 0; // 完全闭合
        if (progress < 0.20f) return 1;
        if (progress < 0.25f) return 2;
        if (progress < 0.30f) return 3;
        if (progress < 0.35f) return 4;
        if (progress < 0.40f) return 5;
        if (progress < 0.55f) return 6; // 完全睁开

        // 后退阶段：闭眼（对称）
        if (progress < 0.62f) return 5;
        if (progress < 0.70f) return 4;
        if (progress < 0.75f) return 3;
        if (progress < 0.80f) return 2;
        if (progress < 0.85f) return 1;
        return 0; // 回到闭合
    }

    // 根据当前帧计算眼睛在 Y 轴上的浮动偏移量（增强“活物”感）
    private float GetVerticalBob(float progress, float scale)
    {
        var frame = GetEyeFrame(progress);
        // 不同帧对应不同的浮动高度（闭眼时浮得高，睁眼时下沉）
        var bobAmount = frame switch
        {
            0 => 12f, // 闭眼：最高
            1 => 8f,
            2 => 4f,
            3 => 3f, // 半睁：略低
            _ => 0f   // 睁开：不浮动
        };
        // 应用缩放和全局特效缩放
        return bobAmount * scale * _s;
    }

    // 生成一个新地漂浮的眼睛
    private void SpawnEye(float initialAge)
    {
        // 创建 Sprite2D 节点
        var sprite = new Sprite2D();
        sprite.Texture = _frames[0]; // 初始为闭眼帧
        sprite.Material = _mat;      // 使用相加混合材质

        // 随机缩放（1.25x ~ 1.88x），并应用全局缩放
        var scale = _rng.RandfRange(1.25f, 1.88f) * _s;
        sprite.Scale = new Vector2(scale, scale);

        // 随机位置（X: -150~150, Y: -175~75），单位为像素，并缩放
        var px = _rng.RandfRange(-150f, 150f) * _s;
        var py = _rng.RandfRange(-175f, 75f) * _s;
        sprite.Position = new Vector2(px, py);

        // 随机旋转角度（6°~12°），右侧的眼睛向左歪，左侧的向右歪（增加自然感）
        var rot = _rng.RandfRange(6f, 12f);
        if (px > 0) rot = -rot;
        sprite.Rotation = Mathf.DegToRad(rot);

        // 随机生成偏紫发光的颜色（R 和 B 高，G 中等）
        var r = _rng.RandfRange(0.8f, 1.0f);
        var g = _rng.RandfRange(0.5f, 0.7f);
        var b = _rng.RandfRange(0.8f, 1.0f);
        var baseColor = new Color(r, g, b, 0f);
        sprite.Modulate = baseColor;

        // 随机设置 ZIndex（-1 或 1），让部分眼睛在角色前、部分在后，增加层次感
        sprite.ZIndex = _rng.Randf() < 0.5f ? -1 : 1;

        // 将 Sprite 添加为子节点（显示在场景中）
        AddChild(sprite);

        // 寿命 = 基础时间 + 缩放比例（大的眼睛活得久一点）
        var lifetime = scale / _s + 0.8f;

        // 将眼睛数据存入列表，供后续更新使用
        _eyes.Add(new EyeData
        {
            Sprite = sprite,
            Age = initialAge,       // 初始年龄（预生成时用）
            Lifetime = lifetime,    // 总寿命
            Scale = scale,          // 缩放
            BaseColor = new Color(r, g, b), // 无 Alpha 的基础色
            BobSpeed = 1f,          // （未使用，但保留扩展性）
            StartY = py             // 初始 Y 位置（用于浮动计算）
        });
    }

    // 结构体：存储单个眼睛的所有动态数据
    private struct EyeData
    {
        public Sprite2D Sprite;     // 对应的 Sprite 节点
        public float Age;           // 当前已存活时间（秒）
        public float Lifetime;      // 总寿命（秒）
        public float Scale;         // 缩放比例
        public Color BaseColor;     // 基础 RGB 颜色（不含透明度）
        public float BobSpeed;      // 浮动速度（当前未使用）
        public float StartY;        // 初始 Y 坐标（用于计算浮动偏移）
    }
}