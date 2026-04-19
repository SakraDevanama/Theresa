shader_type canvas_item;

// 反预乘Alpha着色器
// 用于修正已经预乘过Alpha的Spine纹理
// 原理: 将RGB除以Alpha，恢复原始颜色值

void fragment() {
    vec4 col = texture(TEXTURE, UV);
    
    // 如果Alpha大于0，将RGB除以Alpha来反预乘
    if (col.a > 0.001) {
        col.rgb = col.rgb / col.a;
    }
    
    COLOR = col;
}
