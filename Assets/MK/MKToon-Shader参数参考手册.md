# MKToon Shader 参数完全参考手册

> **逐参数解释 MKToon 典型 Shader 的每个属性含义、效果、取值范围与渲染原理**  
> 配合网络资源与图形学背景知识 | 面向已理解基本操作的开发者

---

## 目录

1. [参数体系架构总览](#1-参数体系架构总览)
2. [Options 类参数](#2-options-类参数)
3. [Input - 基础纹理参数](#3-input---基础纹理参数)
4. [Input - PBR 物理参数](#4-input---pbr-物理参数)
5. [Input - 透光/散射参数](#5-input--透光散射参数)
6. [Input - Detail 细节纹理参数](#6-input--detail-细节纹理参数)
7. [Stylize - 光照风格化参数（核心）](#7-stylize---光照风格化参数核心)
8. [Stylize - 艺术效果参数](#8-stylize---艺术效果参数)
9. [Stylize - 动态特效参数](#9-stylize---动态特效参数)
10. [Advanced 高级渲染参数](#10-advanced-高级渲染参数)
11. [Outline 描边参数](#11-outline-描边参数)
12. [附录：图形学原理解释](#12-附录图形学原理解释)

---

## 1. 参数体系架构总览

MKToon 的 Shader Properties 在源码中按以下层级组织（对应 Inspector 的 Tab 排列）：

```
Properties (StandardPBS.shader 第11~201行)
├── Options (选项)          ← _Workflow, _Surface, _Blend, _RenderFace, _AlphaClipping
├── Input (输入)            ← Albedo, Maps, PBS props, Normal, Height
│   └── Detail (细节)      ← Detail Map, Detail Normal, Blend mode
├── Stylize (风格化)        ← Light style, Thresholds, Ramps, Rim, Artistic...
│   ├── Lighting           ← Style, Threshold, Smoothness, Bands, Ramp maps
│   ├── Gooch              ← Gooch shading params
│   ├── Rim                ← Fresnel rim lighting
│   ├── Iridescence         ← Rainbow/iridescent effect
│   ├── Color Grading       ← Contrast/Hue/Saturation/Brightness
│   ├── Vertex Animation    ← Sine/Pulse/Noise animation
│   ├── Dissolve            ← Dissolve effect
│   └── Artistic            ← Drawn/Hatching/Sketch
├── Advanced (高级)          ← Diffuse model, Specular, Reflections, Stencil
└── Outline (描边)           ← Outline mode, Width, Color, Noise (仅+Outline版)
```

**参数命名规范**: 所有 Shader 属性以 `_` 开头，对应 C# 中的 `Uniform` 类定义。Editor 通过 `Properties.cs` 中注册的 `Property<T>` 对象来读写这些值。

---

## 2. Options 类参数

### `_Workflow` — 工作流模式

| 属性 | 类型 | 默认值 | 枚举定义 |
|------|------|--------|---------|
| `_Workflow` | Enum(int) | 0 | `Workflow.Metallic=0 / Specular=1 / Roughness=2` |

**决定材质使用哪套 PBR 输入参数：**

```
Metallic (0):  使用 _Metallic + _Smoothness (+ 对应Map)
Specular (1):  使用 _SpecularColor/_SpecularMap(A通道) + _Smoothness  
Roughness (2): 使用 _Metallic + _Roughness (+ 对应Map)
```

**为什么重要？** 这决定了你从美术管线导出贴图时的工作流程。Substance Painter / Painter 默认导出 Metalic + Roughness 工作流的贴图。

> **参考**: Unity URP Lit Shader 同样提供 Metallic 和 Specular 两种工作流切换。

---

### `_Surface` — 表面类型

| 属性 | 类型 | 默认值 | 枚举 |
|------|------|--------|------|
| `_Surface` | Enum(int) | 0 | `Surface.Opaque=0 / Transparent=1` |

**控制渲染状态的关键开关**：
- `Opaque` → ZWrite=On, RenderType="Opaque", RenderQueue=2000, 启用 ShadowCaster Pass
- `Transparent` → ZWrite=Off, RenderType="Transparent", RenderQueue=3000, 禁用 ShadowCaster Pass

> 注意: `+Outline` 版本的 Surface 强制为 Opaque（`SurfaceOutline` enum），这是为了避免透明物体的 z-fighting 问题。

---

### `_Blend` — 混合模式

| 属性 | 类型 | 默认值 | 枚举 |
|------|------|--------|------|
| `_Blend` | Enum(int) | 0 | `Blend.Alpha=0 / Premultiply=1 / Additive=2 / Multiply=3 / Custom=4` |

**各模式的 Blend Factor 设置** (来自 `Property.cs` → `BlendProperty.SetValue()`):

| 模式 | SrcRGB | DstRGB | SrcA | DstA | 适用场景 |
|------|-------|--------|------|------|---------|
| **Alpha** | SrcAlpha | OneMinusSrcAlpha | One | OneMinusSrcAlpha | 标准透明（玻璃、布料）|
| **Premultiply** | **One** | OneMinusSrcAlpha | One | OneMinusSrcAlpha | **保留高光和反射的透明**（推荐用于带反射的透明物体）|
| **Additive** | SrcAlpha | **One** | One | One | 叠加发光（火焰、全息投影）|
| **Multiply** | **DstColor** | **Zero** | Zero | One | 正片叠底（阴影、染色玻璃）|

> **Premultiply vs Alpha**: 当材质有环境反射时，Premultiply 能正确保留高光和反射。Alpha 模式下高光区域可能因为 alpha 预乘而变暗。参考: [Unity Manual - Standard Transparent Rendering](https://docs.unity3d.com/Manual/transparent-rendering.html)

---

### `_RenderFace` — 渲染面剔除

| 属性 | 类型 | 默认值 | Cull 指令 |
|------|------|--------|----------|
| `_RenderFace` | Enum(int) | 2 (Front) | Front→Cull Back; Back→Cull Front; Double→Cull Off |

- **Front** (=2): 正常渲染，剔除背面（标准）
- **Back** (=1): 剔除正面，看到内部（用于建筑内部视角等）
- **Double Sided** (=0): 双面渲染（性能代价较高）

---

### `_AlphaClipping` — Alpha 裁切

开启后:
- 启用 `clip()` 操作在 Fragment Shader 中
- RenderQueue 切换到 AlphaTest (2450)
- RenderType = "TransparentCutout"
- 关键字 `_MK_ALPHA_CLIPPING` 激活

**裁切公式**: `if(albedo.a - _AlphaCutoff < 0) discard;`

---

## 3. Input - 基础纹理参数

### `_AlbedoColor` / `_AlbedoMap` / `_AlbedoMapIntensity`

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_AlbedoColor` | Color(RGBA) | `(1,1,1,1)` | 主颜色 |
| `_AlbedoMap` | Texture2D | `"white" {}` | 漫反射贴图 (sRGB) |
| `_AlbedoMapIntensity` | Range(0,1) | 1.0 | 贴图对最终颜色的混合强度 |

**最终 Albedo 计算**: 
```glsl
float3 albedo = _AlbedoColor.rgb * _AlbedoMapIntensity + tex2D(_AlbedoMap, uv).rgb * (1.0 - _AlbedoMapIntensity);
// 实际实现可能是: lerp(color, sample, intensity) 形式
```

> 对于卡通渲染，建议 Albedo 贴图**尽量平坦**（避免 baked-in 光照），让 MKToon 的光照系统接管所有光影计算。

### `_NormalMap` / `_NormalMapIntensity`

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_NormalMap` | Texture2D | `"bump" {}` | 切线空间法线贴图 |
| `_NormalMapIntensity` | Float | 1.0 | 法线强度缩放 |

法线贴图在卡通渲染中仍然非常重要——它影响:
- 光照方向计算 → 影响明暗边界形状
- Rim Light 的分布
- Outline 的挤出方向
- Specular 高光的位置

**强度 > 1.0** 会增强凹凸感（类似 Blender 的 Bump Strength），但可能导致视觉伪影。

---

## 4. Input - PBR 物理参数

### Specular 工作流 (`_Workflow == 1`)

| 属性 | 类型 | 默认值 | 通道含义 |
|------|------|--------|---------|
| `_SpecularColor` | Color(RGB) | `(0.203, 0.203, 0.203, 1)` | F0 反射率（默认 ~4% 灰色，非金属标准值）|
| `_SpecularMap` | Texture2D | `"white" {}` | RGB=F0颜色, A=Smoothness |
| `_Smoothness` | Range(0,1) | 0.5 | 表面光滑度 |

**默认 F0 = 0.203125 的来源**: 这是非电介质（dielectric）在可见光波段的平均 Fresnel 反射率。参见: [Fresnel Equations - Wikipedia](https://en.wikipedia.org/wiki/Fresnel_equations) 和 [PBR Theory - LearnOpenGL](https://learnopengl.com/PBR/Theory)

### Metallic 工作流 (`_Workflow == 0`)

| 属性 | 类型 | 默认值 | 通道含义 |
|------|------|--------|---------|
| `_Metallic` | Range(0,1) | 0 | 金属度: 0=介质, 1=纯金属 |
| `_MetallicMap` | Texture2D | `"white" {}` | R=Metallic, A=Smoothness |
| `_Smoothness` | Range(0,1) | 0.5 | 光滑度 |

** metallic = 0 时**: 材质为电介质（塑料、木头、布料），F0 使用 `_SpecularColor` 的默认值。
** metallic = 1 时**: 材质为金属，F0 由 Albedo 颜色计算（有色高光）。

### Roughness 工作流 (`_Workflow == 2`)

| 属性 | 类型 | 默认值 | 通道含义 |
|------|------|--------|---------|
| `_Roughness` | Range(0,1) | 0.5 | 粗糙度: 0=极光滑, 1=极粗糙 |
| `_RoughnessMap` | Texture2D | `"white" {}` | R=Roughness |

> Roughness 工作流更接近现代引擎（Unreal Engine、Unity HDRP）的标准做法——直接用 Roughness 而非 Smoothness (1-Roughness)。

### `_Parallax` / `_HeightMap` — 视差映射

| 属性 | 类型 | 范围 | 说明 |
|------|------|------|------|
| `_HeightMap` | Texture2D | — | 高度图，R通道=高度信息 |
| `_Parallax` | Range | 0~0.1 | 视差偏移强度 |

**原理**: 根据 View Direction 和 Height Map 在采样 UV 时进行偏移，模拟几何深度感。仅在高度图赋值且有 Parallax > 0 时激活关键字 `_MK_PARALLAX`。

> 参考: [Parallax Mapping - LearnOpenGL](https://learnopengl.com/Advanced-Lighting/Parallax-Mapping)

---

## 5. Input - 透光/散射参数

### `_LightTransmission` — 透光模式

| 枚举值 | 名称 | 效果 | 关键字 |
|--------|------|------|--------|
| 0 | Off | 无透光 | — |
| 1 | Translucent | **背光透过**（简单近似）| `_MK_LIGHT_TRANSMISSION_TRANSLUCENT` |
| 2 | Subsurface Scattering | **次表面散射**（光线内部反弹）| `_MK_LIGHT_TRANSMISSION_SUB_SURFACE_SCATTERING` |

### 各参数详解

#### `_ThicknessMap` / `_LightTransmissionDistortion` / `_LightTransmissionColor`

| 属性 | 说明 |
|------|------|
| `_ThicknessMap` | 厚度贴图: **R=0 → 厚(不透)**, **R=1 → 薄(全透)** |
| `_LightTransmissionDistortion` | 光沿法线方向的扩散程度 0~1。值越大散射范围越大 |
| `_LightTransmissionColor` | 透光的颜色 tint。默认橙黄色 `(1, 0.65, 0)` 模拟皮肤/叶肉的透光色 |
| `_LightTransmissionIntensity` | (Advanced中) 整体强度倍数 |

**Translucent 近似算法**:
```
// 简化描述: 用 dot(lightDir, -normal) 的负值部分（背光面）
// 结合 thickness 和 distortion 来计算透光贡献
transmission = saturate(dot(-L, N)) * thickness * distortion * color;
```

**SSS 近似算法** (Subsurface Scattering):
```
// 使用一个预计算的 wrap 值来模拟光线进入表面后的散射
// 比 Translucent 更真实但更昂贵
sss = WrapDiffusion(L, N, distortion) * thickness * transmissionColor;
```

> **典型用途**: 角色耳朵/头发受光透红、纸张/树叶背光、蜡烛/玉石效果。参考: [SSS in Unity URP - Alan Zucconi](https://www.alanzucconi.com/unity/s-s-s-subsurface-scattering/)

---

## 6. Input - Detail 细节纹理参数

### Detail 系统允许在不增加分辨率的情况下叠加一层细节纹理

| 属性 | 类型 | 说明 |
|------|------|------|
| `_DetailMap` | Texture2D (RGBA) | 细节贴图: RGB=颜色细节, A=强度遮罩 |
| `_DetailColor` | Color(RGB) | 细节的染色 (白色时不改变色相) |
| `_DetailBlend` | Enum | Mix(0)/Add(1)/Multiply(2) — 与 Albedo 的混合方式 |
| `_DetailMix` | Range(0,1) | 混合比例 |
| `_DetailNormalMap` | Texture2D | 细节法线 |
| `_DetailNormalMapIntensity` | Float | 细节法线强度 |
| `_DetailUVSet` | Enum(UV0/UV7) | Detail 使用的 UV 通道（需全局 Feature 开启才可选 UV7）|

**三种混合模式的效果**:

```
Mix:     finalAlbedo = lerp(albedo, detail * detailColor, mix * detail.a)
Add:     finalAlbedo = albedo + detail * detailColor * mix * detail.a
Multiply: finalAlbedo = albedo * lerp(white, detail * detailColor, mix * detail.a)
```

> **性能提示**: Detail Map 和 Detail Normal Map 都会触发额外的 texture sample，移动端注意。

---

## 7. Stylize - 光照风格化参数（核心）

这是 MKToon 最有价值的部分。所有参数都围绕一个核心问题:**如何将连续的 PBR 光照离散化为风格化的明暗层次**。

### `_Light` — 光照风格选择

| 枚举 | 名称 | 关键字 | 效果描述 |
|------|------|--------|---------|
| 0 | Builtin (内置) | (无) | 标准 PBR 光照，无任何卡通化处理。平滑渐变的明暗过渡 |
| 1 | Cel (赛璐珞) | `_MK_LIGHT_CEL` | 单一 cutoff 阈值，将光照二值化（或接近二值化）。经典的"二次元明暗分界" |
| 2 | Banded (多层色阶) | `_MK_LIGHT_BANDED` | 将光照分为 N 层（2~12），每层之间可配置平滑度。类似"三色调"/"四色调" |
| 3 | Ramp (渐变贴图) | `_MK_LIGHT_RAMP` | 使用 1D 渐变贴图（存储在 2D 贴图的 R 通道）精确映射光照到任意曲线 |

**四种风格的视觉效果对比**:

```
Builtin:  ████████████░░░░░░░░░  (平滑过渡)
Cel:      ██████████████████░░░░  (一次硬切)
Banded:   ██████████░░░████░░░░  (多层阶梯)
Ramp:     ███████░▄░▀░▄░▀░▄░▀░░░  (自定义曲线)
```

> **选型建议**: 
> - 角色 → Cel（干净利落）
> - 场景物件 → Ramp 或 Banded（更多控制）
> - 需要 pixel-perfect 控制 → Ramp（自定义渐变贴图）
> - 快速原型/不关心 → Builtin

---

### `_LightThreshold` / `_AdditionalLightsThreshold` — 光照阈值

| 属性 | 范围 | 默认值 | 说明 |
|------|------|--------|------|
| `_LightThreshold` | 0~1 | 0.5 | **主光源**(Directional Light)的明暗分界点 |
| `_AdditionalLightsThreshold` | 0~1 | 0.5 | **附加光源**(Point/Spot)的分界点 |

**阈值的作用机制**:

对于 Cel/Banded 模式，光照计算后得到一个 0~1 的亮度值 `NdotL`（法线与光线的点积），然后：

```glsl
// Cel 模式的简化逻辑
float lighting = dot(normal, lightDir);       // -1 ~ 1
lighting = lighting * 0.5 + 0.5;               // remap to 0 ~ 1
lighting = step(_LightThreshold, lighting);      // 大于阈值=1(亮), 否则=0(暗)

// 加上 Wrapped Lighting 后:
lighting = lighting * wrappedFactor + (1.0 - wrappedFactor) * 0.5;
lighting = smoothstep(threshold - smooth, threshold + smooth, lighting);
```

**实际效果**:
- **Threshold = 0.5** (默认): 约 50% 区域亮，50% 暗。标准平衡
- **Threshold = 0.3**: 更亮的材质，更大面积被照亮
- **Threshold = 0.7**: 更暗的材质，仅小面积高光

> **调试技巧**: 先设 Main Light Threshold 让主光源正确，再用 Additional Lights Threshold 微调补光（点光源容易过曝）。

---

### `_DiffuseSmoothness` / `_DiffuseThresholdOffset` — 漫反射平滑度

| 属性 | 范围 | 默认值 | 说明 |
|------|------|--------|------|
| `_DiffuseSmoothness` | 0~1 | 0.0 | 漫反射明暗边界的柔和程度 |
| `_DiffuseThresholdOffset` | 0~1 | 0.25 | 叠加到主阈值的额外偏移量 |

**Smoothness 的作用**: 使用 `smoothstep` 替代硬 `step`:

```glsl
// 硬边缘 (smoothness = 0):
float cel = step(threshold, ndotl);

// 柔边缘 (smoothness = 0.1):
float cel = smoothstep(threshold - 0.1, threshold + 0.1, ndotl);

// ThresholdOffset 的作用:
float finalThreshold = _LightThreshold + _DiffuseThresholdOffset;
float cel = smoothstep(finalThreshold - smooth, finalThreshold + smooth, ndotl);
```

**推荐的 Smoothness 值**:
- **0.0 ~ 0.05**: 几乎硬边（经典日式 RPG）
- **0.1 ~ 0.2**: 轻微抗锯齿（推荐用于角色）
- **0.3+**: 明显柔和（更像水彩/手绘风）

> **Threshold Offset 的意义**: 它允许漫反射和高光使用**不同的阈值中心点**。例如漫反射 threshold=0.55 而高光 threshold=0.7，这样高光区域会比漫反射亮区域更"聚焦"。

---

### `_SpecularSmoothness` / `_SpecularThresholdOffset` — 高光平滑度

与 Diffuse 同理，但作用于 **Specular (镜面反射)** 分量：

| 属性 | 范围 | 默认值 | 说明 |
|------|------|--------|------|
| `_SpecularSmoothness` | 0~1 | 0.0 | 高光边缘柔和度 |
| `_SpecularThresholdOffset` | 0~1 | 0.25 | 高光阈值偏移 |

> **典型设置**: 高光的 Smooth 通常比 Diffuse 稍大一些（0.1~0.3），让高光斑看起来更自然而不是生硬的圆点。如果 Specular 完全硬边，在高光边界处会产生难看的锯齿。

---

### `_LightBands` / `_LightBandsScale` — 多层色阶（仅 Banded 模式）

| 属性 | 范围 | 默认值 | 说明 |
|------|------|--------|------|
| `_LightBands` | 2~12 | 4 | 色阶层数 |
| `_LightBandsScale` | 0~1 | 0.5 | 色阶推挤系数 |

**Bands 的数学含义**:

```glsl
// Banded 模式将 0~1 的光照均匀分为 N 个区间
float bands = _LightBands;  // 例如 4
float lighting = dot(n, l) * 0.5 + 0.5;  // 0~1

// 每个区间宽度 = 1.0 / bands
// quantize 到最近的层
float bandStep = 1.0 / bands;
float quantized = floor(lighting / bandStep + 0.5) * bandStep;

// Scale 控制层的分布
// scale=0.5 表示层往暗部"推挤"
quantized = quantized * _LightBandsScale + (1.0 - _LightBandsScale) * lighting;

// 最后应用 smoothstep 平滑
quantized = smoothstep(quantized - smooth, quantized + smooth, lighting);
```

**不同 Bands 数量的视觉效果**:
- **Bands=2**: 两色调（纯亮/纯暗），极端对比
- **Bands=3**: 三色调（亮/中/暗），常见于简约 Low Poly
- **Bands=4**: 四色调（经典二次元层次）
- **Bands=6~8**: 接近连续但有微妙层次（水彩/插画风）
- **Bands=10~12**: 几乎连续，仅在有渐变的地方体现

---

### `_*Ramp` — 渐变贴图（仅 Ramp 模式）

| 属性 | 默认值 | 读取方式 |
|------|--------|---------|
| `_DiffuseRamp` | `"grey" {}` | 采样 R 通道，X轴=光照强度(左暗右亮), Y轴=衰减(仅非定向光) |
| `_SpecularRamp` | `"grey" {}` | 同上，应用于高光 |
| `_RimRamp` | `"grey" {}` | 应用于边缘光 |
| `_LightTransmissionRamp` | `"grey" {}` | 应用于透光 |

**Ramp 贴图的工作方式**:

```glsl
// 采样 ramp: uv.x = 光照强度(0~1), uv.y = 衰减/角度因子(0~1)
float2 rampUV = float2(lighting, falloffOrAngle);
float rampValue = tex2D(_DiffuseRamp, rampUV).r;

// 直接用 ramp 值替代原来的光照值
finalLighting = rampValue;
```

**制作自定义 Ramp 贴图**:
1. 创建 **256x1** 或 **512x2** 的 PNG（宽度是关键，高度可以是1或2）
2. 从**左到右**绘制: 黑色(0,0,0) → 白色(255,255,255) 存储在 **R 通道**
3. 你可以在中间画**阶梯**（Cel 效果）、**曲线**（柔边）或任意形状
4. 导入 Unity，设置 **Filter Mode = Clamp**, **sRGB = False**

> **在线工具**: [Gradient Ramp Editor by Rory Driscolls](https://www.rorydriscolls.com/tools/gradientrampeditor/) —— 可视化创建 Ramp 贴图并下载
>
> **高级技巧**: 利用 Y 轴（falloff）可以让**非定向光**（点光源、聚光灯）使用不同的渐变曲线。例如让点光源的衰减区更暗。

---

### `_ThresholdMap` / `_ThresholdMapScale` — 阈值贴图

| 属性 | 类型 | 默认值 |
|------|------|--------|
| `_ThresholdMap` | Texture2D | `"gray" {}` |
| `_ThresholdMapScale` | Float | 1.0 |

**作用**: 用一张贴图的 R 通道在每个像素位置**修改局部的光照阈值**。

```glsl
float threshold = _LightThreshold + tex2D(_ThresholdMap, uv).r * _ThresholdMapScale;
float cel = smoothstep(threshold - smooth, threshold + smooth, ndotl);
```

**用途示例**:
- 角色面部：额头/鼻子/颧骨处阈值略不同（模拟复杂几何的近似）
- 衣物褶皱：凹陷处自动更暗
- 边缘强调：轮廓线位置通过阈值变化产生"伪描边"效果

---

### `_AdditionalLightsFalloff` — 附加光源衰减形状

| 属性 | 范围 | 默认值 | 说明 |
|------|------|--------|------|
| `_AdditionalLightsFalloff` | 0~1 | 1.0 | 点光源/聚光灯的衰减形状 |

- **0 (或近0)**: 光照范围呈**球形/均匀**衰减，与编辑器中的 Range Gizmo 形状一致。多个相同强度的光源衰减一致。
- **1**: 使用**物理准确的平方反比衰减**（intensity/distance^2），近距离非常亮，远处快速衰减。更真实但更难手动调参。

> **建议**: 卡通渲染通常希望光照可控性强，设为 0~0.3 范围更容易调整。

---

## 8. Stylize - 艺术效果参数

### Gooch 着色参数

Gooch Shading 是一种**艺术风格的着色技术**，由 A. Gooch 和 B. Gooch 于 1998 年提出。它用暖色替代亮部、冷色替代暗部，产生类似水彩画或插画的效果。

> **论文原文**: [A Non-Photorealistic Lighting Model for Automatic Technical Illustration (Gooch & Gooch, 1998)](https://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.15.3374&rep=rep1&type=pdf)

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_GoochRamp` | Texture2D | `"white" {}` | Gooch 色彩渐变（X=亮度映射）|
| `_GoochRampIntensity` | Range(0,1) | 0.5 | 整体 Gooch 效果混合强度 |
| `_GoochBrightColor` | Color | `(1,1,1,1)` | 亮部颜色 |
| `_GoochBrightMap` | Texture2D | `"white" {}` | 亮部颜色贴图（与 Albedo 插值）|
| `_GoochDarkColor` | Color | `(0,0,0,1)` | 暗部颜色 |
| `_GoochDarkMap` | Texture2D | `"white" {}` | 暗部颜色贴图（与 Albedo 插值）|
| `_GoochDarkRemapFadeWithIndirect` | Toggle | Off | 暗部是否向 GI/环境光渐隐 |
| `_GoochDarkRemapMin/Max` | Range | 0~1 | 暗部重映射范围 |

**Gooch 的简化算法**:

```glsl
// 标准光照值 lighting 在 0~1 之间
float lighting = dot(n, l); // -1 ~ 1, remapped to 0 ~ 1

// 亮部: 在 albedo 和 goochBright 之间插值
float3 brightColor = lerp(albedo, _GoochBrightColor, lighting);

// 暗部: 在 albedo 和 goochDark 之间插值  
float darkFactor = 1.0 - lighting;
float3 darkColor = lerp(albedo, _GoochDarkColor, darkFactor);

// 最终: 根据 lighting 选择 bright 或 dark
float3 finalColor = lerp(darkColor, brightColor, lighting);
finalColor = lerp(originalFinal, finalColor, _GoochRampIntensity);
```

**Fade With Indirect** (开启后):
```glsl
// 暗部的 Gooch 贡献随 indirect lighting 衰减
// 这样在强环境光区域可以看到环境色而非死板的 goochDark
float indirectOcclusion = 1.0 - indirectLighting * fadeFactor;
darkContribution *= indirectOcclusion;
```

**推荐配色方案**:
| 风格 | Bright | Dark | Intensity |
|------|-------|------|-----------|
| 水彩画 | 淡黄 `(1, 0.95, 0.8)` | 淡蓝灰 `(0.6, 0.65, 0.75)` | 0.4~0.6 |
| 温暖插画 | 暖白 `(1, 0.9, 0.85)` | 深棕 `(0.3, 0.2, 0.15)` | 0.5~0.7 |
| 冷色调 | 冷白 `(0.85, 0.9, 1)` | 深蓝 `(0.1, 0.15, 0.25)` | 0.3~0.5 |

---

### Rim（菲涅尔边缘光）参数

基于 **Fresnel Effect**（菲涅尔效应）：视线与表面法线夹角越大，边缘光越强。

> **理论基础**: [Fresnel Equations - Wikipedia](https://en.wikipedia.org/wiki/Fresnel_equations)

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_Rim` | Enum | 0 (Off) | Off / Default / Split |
| `_RimSize` | Range(0,5) | 1.0 | 边缘光扩散范围 |
| `_RimColor` | Color(RGBA) | `(1,1,1,1)` | Default 模式的颜色 + Alpha=透明度 |
| `_RimBrightColor` | Color | `(1,1,1,1)` | Split 模式: 亮部边缘光 |
| `_RimDarkColor` | Color | `(0,0,0,1)` | Split 模式: 暗部边缘光 |
| `_RimSmoothness` | Range(0,1) | 0.5 | 边缘光柔和度 |
| `_RimThresholdOffset` | Range(0,1) | 0.25 | 边缘光额外阈值偏移 |

**三种 Rim 模式的区别**:

```glsl
// Fresnel 基础值 (视角与法线的点积)
float fresnel = 1.0 - max(0, dot(viewDir, normal));
float rim = pow(fresnel, _RimSize);  // Size 控制指数衰减速度

// Default 模式: 全局统一 rim
finalRim = _RimColor.rgb * rim * _RimColor.a;

// Split 模式: 亮部和暗部分别计算
float rimBright = pow(fresnelLit, _RimSize) * _RimBrightColor.a;
float rimDark  = pow(fresnelDark, _RimSize) * _RimDarkColor.a;
finalRim = lerp(rimDark * _RimDarkColor.rgb, rimBright * _RimBrightColor.rgb, lighting);
```

**Size 的直观效果**:
- **0.5~1.0**: 很窄的边缘光（仅最外侧轮廓）
- **2.0~3.0**: 宽边缘光（明显的"光晕"效果）
- **4.0~5.0**: 非常宽（几乎覆盖整个侧面）

> **Split Rim 典型用法**: Bright=(1,1,1,0.8) 白色高光 + Dark=(1,0.5,0,0.4) 暖橙色 → 暗部有暖色轮廓，亮部有白色高光

---

### Iridescence（彩虹色/虹彩）参数

基于视角变化的薄膜干涉色彩效应。

> **物理原理**: [Thin-film interference - Wikipedia](https://en.wikipedia.org/wiki/Thin-film_interference)

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_Iridescence` | Toggle | Off | 开启/关闭 |
| `_IridescenceRamp` | Texture2D | `"white" {}` | 彩虹色渐变（X轴=色彩变化）|
| `_IridescenceSize` | Range(0,5) | 1.0 | 效果大小/分布范围 |
| `_IridescenceColor` | Color(RGBA) | `(1,1,1,0.5)` | 额外染色 + Alpha=整体强度 |
| `_IridescenceSmoothness` | Range(0,1) | 0.5 | 过渡平滑度 |
| `_IridescenceThresholdOffset` | Range(0,1) | 0.0 | 额外阈值偏移 |

**算法简述**:
```glsl
// 基于 fresnel 和法线方向的组合产生变化的索引
float iridIndex = dot(viewDir, normal) * dot(normal, lightDir) * _IridescenceSize;
iridIndex += _IridescenceThresholdOffset;
float2 iridUV = float2(iridIndex * 0.5 + 0.5, 0.5); // 映射到 ramp 中部
float3 iridColor = tex2D(_IridescenceRamp, iridUV).rgb;

// 最终与基础颜色叠加
finalColor += iridColor * _IridescenceColor.rgb * _IridescenceColor.a;
```

**典型用法**: 
- 泡泡/肥皂膜效果（Size大 + 全彩 Ramp）
- 金属材料表面的变色高光（光盘油墨、拉丝金属）
- 幻灵/魔法物品（配合低频 Ramp 产生梦幻效果）

---

### Color Grading（后期调色）参数

在 Shader 内置的简单后期处理，避免依赖 Volume 后处理。

| 属性 | 范围 | 说明 |
|------|------|------|
| `_Contrast` | Float | 对比度。>1 增强, <1 降低 |
| `_Hue` | 0~1 | 色相偏移（整个色轮360°）|
| `_Saturation` | 0+ | 饱和度。0=灰度图, 1=原始, >1=过饱和 |
| `_Brightness` | 0+ | 亮度偏移 |
| `_ColorGrading` | Enum | Off / Albedo / FinalOutput |

**两种应用阶段的区别**:
- **Albedo**: 在光照计算之前对 Albedo 贴图做调色。会影响后续所有光照计算的结果。
- **FinalOutput**: 在所有计算完成（光照+发射+边缘光等）之后做调色。只改变最终输出颜色。

**推荐用法**: 
- 想降低贴图饱和度 → Albedo（让光照重新上色）
- 想统一调整最终画面 → FinalOutput
- 想做特殊效果（如黑白+单色突出）→ FinalOutput + Saturation=0 + 自定义 Hue

---

## 9. Stylize - 动态特效参数

### Vertex Animation（顶点动画）

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_VertexAnimation` | Enum | 0 (Off) | Off / Sine / Pulse / Noise |
| `_VertexAnimationIntensity` | Range(0,1) | 0.05 | 动画振幅（世界空间单位）|
| `_VertexAnimationFrequency` | Vector3 | (2.5, 2.5, 2.5) | X/Y/Z 轴各自的频率 |
| `_VertexAnimationMap` | Texture2D | `"white" {}` | 强度调制贴图（R通道）|
| `_VertexAnimationStutter` | Toggle | Off | 抖动效果 |

**各模式的顶点位移公式**:

```hlsl
// Sine 模式: 正弦波
float sine = sin(_Time.y * frequency.x + position.x * freqMod.x) 
             * sin(_Time.y * frequency.y + position.y * freqMod.y)
             * sin(_Time.z * frequency.z + position.z * freqMod.z);
vertex.position += normal * sine * intensity * map.r;

// Pulse 模式: 脉冲函数 (周期性尖锐峰值)
float pulse = abs(fract(_Time.y * frequency - position * freqMod) * 2.0 - 1.0);
vertex.position += normal * pulse * intensity * map.r;

// Noise 模式: 基于噪声函数
float noise = snoise(position * frequency + _Time.y) * intensity;
vertex.position += normal * noise * map.r;
```

> **性能注意**: 顶点动画在 GPU Vertex Shader 中执行，通常不会成为瓶颈。但极度复杂的网格（百万级三角面）需注意。

---

### Dissolve（溶解）特效

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_Dissolve` | Enum | 0 (Off) | Off / BorderColor / BorderRamp |
| `_DissolveMap` | Texture2D | `"white" {}` | 溶解布局图（R通道决定哪里先溶解）|
| `_DissolveMapScale` | Float | 1.0 | 贴图缩放 |
| `_DissolveAmount` | Range(0,1) | 0.5 | **0=完全显示, 1=完全溶解** |
| `_DissolveBorderSize` | Range(0,1) | 0.25 | 边缘过渡带宽 |
| `_DissolveBorderRamp` | Texture2D | `"white" {}` | 边缘渐变贴图（BorderRamp 模式）|
| `_DissolveBorderColor` | HDR Color | `(1,1,1,1)` | 边缘颜色（BorderColor 模式）|

**溶解的核心算法**:

```glsl
float dissolveValue = tex2D(_DissolveMap, uv).r;
float edge = dissolveValue - _DissolveAmount;  // amount 增大 → edge 从正变负

// Simple 模式: 直接 clip
clip(edge);  // edge < 0 的像素丢弃

// BorderColor 模式: 在边界处显示颜色
float border = smoothstep(-_DissolveBorderSize, 0, edge);
float3 withBorder = lerp(baseColor, _DissolveBorderColor, border);

// BorderRamp 模式: 用渐变贴图映射边界
float2 borderUV = float2(edge * 0.5 + 0.5, 0.5);
float3 borderRampColor = tex2D(_DissolveBorderRamp, borderUV).rgb;
float borderWidth = smoothstep(-_DissolveBorderSize, 0, edge);
float3 withBorder = lerp(baseColor, borderRampColor, borderWidth);
```

**Amount 参数的时间线**:
```
0.0 ──────────────── 0.5 ──────────────── 1.0
███████████████████░░░░░░░░░░░░░░░░░░░░░░
   完全可见                    完全消失
```

> **运行时控制**: 通过代码 `material.SetFloat("_DissolveAmount", progress)` 可以做出死亡消失、传送门出现/消失等效果。

---

### Artistic（艺术风格）参数

| 模式 | 所需贴图 | 效果描述 |
|------|---------|---------|
| **Drawn** | Drawn Map (R) | 手绘感。通过查找纹理将光照映射为手绘笔触风格 |
| **Hatching** | HatchingBright (RGB) + HatchingDark (RGB) | **排线/交叉排线**。亮部/暗部各3个层级共6通道 |
| **Sketch** | Sketch Map (R) | 素描效果。噪声状纹理融入光照 |

公共参数:
| 属性 | 说明 |
|------|------|
| `_ArtisticProjection` | Tangent Space（UV驱动）/ Screen Space（屏幕空间投射，跟随视角）|
| `_ArtisticFrequency` | 重绘频率。1=静态, >1=动态持续重绘 |
| `*MapScale` | 各贴图的 UV 缩放 |
| `_DrawnClampMin/Max` | Drawn 模式的暗/亮阈值 |
| `_ArtisticShadowFilter` | 系统阴影上的覆盖强度（需全局 Feature）|

**Hatching 通道映射**:
```
HatchingDarkMap:  R → G → B   (从最深 → 次深 → 浅暗)
HatchingBrightMap: R → G → B (从最亮 → 次亮 → 中等亮)
```

当光照值落入某个区间时，对应的通道纹理被采样并叠加到最终颜色上。

> **Screen Space Projection 的优势**: 排线/素描纹理固定在屏幕上，不会随模型移动而"滑动"，更像真正的手绘过程。

---

## 10. Advanced 高级渲染参数

### `_Diffuse` — 漫反射模型

| 枚举 | 名称 | 特点 | 适用场景 |
|------|------|------|---------|
| 0 | Lambert | 最简单的 cos(theta) | 通用、性能最好 |
| 1 | Oren-Nayar | 考虑粗糙度和视角的"微表面理论"| 粗糙表面: 石头、混凝土、粗糙塑料 |
| 2 | Minnaert | 视角越倾斜越暗 | 布料、天鹅绒、毛发 |

**Oren-Nayar vs Lambert 对比**:
```
Lambert:   diffuse = max(0, dot(N, L))
Oren-Nayar: diffuse = A + B * sigma * max(0, dot(N,L)) * max(0, dot(V,R))
           // 其中 sigma 是粗糙度, V 是视线方向, R 是反射方向
           // 结果: 粗糙表面在掠射角(看侧面)时会更暗
```

> 参考: [Oren-Nayar Reflectance Model - Scratch a Pixel](https://scratchapixel.com/lessons/surfaces-theory-and-mathematics/optics-fresnel/

### `_Specular` — 高光模式

| 枚举 | 名称 | 说明 |
|------|------|------|
| 0 | **Off** | 完全关闭高光（Simple Shader 只有 Off/Isotropic 两个选项）|
| 1 | **Isotropic** | 标准 PBR 各向同性高光（圆形高光斑）|
| 2 | **Anisotropic** | 各向异性高光（**拉伸的高光斑**）|

**Anisotropic + `_Anisotropy`**:
- **Anisotropy = -1~0**: 沿切线方向拉伸（适合头发、拉丝金属）
- **Anisotropy = 0~1**: 垂直于切线方向拉伸
- **Anisotropy = 0**: 回退到 Isotropic

> **各向异性需要 Tangent 信息**: 你的 Mesh 必须包含切线数据（Unity 默认导入时会生成）。没有 tangent 的网格 Anisotropy 无法正确工作。

### `_EnvironmentReflections` — 环境反射

| 枚举 | 名称 | 说明 |
|------|------|------|
| 0 | **Off** | 无环境反射/无GI |
| 1 | **Ambient** | 仅使用 Ambient 光（Light Probe 的平均颜色）|
| 2 | **Advanced** | 完整的环境反射（Reflection Probes + Skybox + GI + Ambient）|

**Off**: 材质在阴影中完全黑色（除非有 Indirect Light / Emission）
**Ambient**: 阴影区有统一的 ambient 颜色填充，但无镜面反射
**Advanced**: 阴影区有完整的环境反应，包括天空盒反射、GI、Light Probe

> **性能建议**: 角色用 Ambient 即可（节省 Reflection Probe 采样）；金属/光滑表面用 Advanced。

### `_WrappedLighting` — 包裹光照

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `_WrappedLighting` | Toggle | **ON** | 包裹光照开关 |

**包裹光照的效果**:
```glsl
// 普通 Lambert: dot(N,L) 范围 -1~1, remap 后 0~1 时暗部可能全黑
// 包裹光照: 将 -1~1 映射到 0~1 的范围更大（半球积分）
// 公式: (dot(N,L) * wrap + wrap) / (1 + wrap)
// 默认 wrap ≈ 0.5 时: 即使背光面也有 ~0.33 的亮度
```

> **强烈建议保持 ON**。关闭后暗部会变得非常黑（尤其是 Cel/Banded 模式下），除非你故意想要这种"纯黑阴影"的效果。

### `_IndirectFade` — 间接光衰减

| 属性 | 范围 | 默认值 | 说明 |
|------|------|--------|------|
| `_IndirectFade` | 0~1 | 1.0 | 间接光在阴影区的衰减 |

控制 GI/Ambient Light 在多"暗"的区域有多强:
- **1.0**: 间接光在阴影区和亮区同样强（物理正确）
- **0.0**: 阴影区完全没有间接光
- **0.5**: 折中方案

### `_ReceiveShadows` — 接收阴影

| 属性 | 类型 | 默认值 |
|------|------|--------|
| `_ReceiveShadows` | Toggle | **ON** |

关闭后该物体不受其他物体投射阴影的影响（但仍会自己投射阴影给其他物体）。

### `_Stencil` — 模板测试

完整的 Stencil State 配置（Custom 模式下）:

| 属性 | 说明 |
|------|------|
| `_StencilRef` | 参考值 (0~255) |
| `_StencilReadMask` | 读掩码 |
| `_StencilWriteMask` | 写掩码 |
| `_StencilComp` | 比较函数 (Never/Less/LEqual/Equal/Greater/GEqual/NotEqual/Always) |
| `_StencilPass` | 通过时的操作 (Keep/Zero/Replace/IncSat/DecSat/Invert/IncWrap/DecWrap) |
| `_StencilFail` | 失败时的操作 |
| `_StencilZFail` | 深度失败时的操作 |

**Builtin 模式** (默认): Ref=0, ReadMask=255, WriteMask=255, Comp=Always, Pass=Keep, Fail=Keep, ZFail=Keep — 即不做任何模板限制。

> **用途示例**: 用于限制只在特定区域渲染描边（如通过 Stencil Buffer 标记角色区域）、实现镂空效果、或与自定义 Post-Processing 配合。

### `_RenderPriority` — 渲染优先级

| 范围 | 说明 |
|------|------|
| -50 ~ 50 | 偏移 Render Queue。正值越大越先渲染（覆盖在其他物体之上）|

**自动分配的 Render Queue 基准**:
- Opaque + No AlphaClip: 2000 (Geometry)
- Opaque + AlphaClip: 2450 (AlphaTest)
- Transparent: 3000 (Transparent)

最终 `renderQueue = baseQueue - priority`。所以 priority=10 时 opaque 材质的 renderQueue = 1990（稍晚于普通物体）。

---

## 11. Outline 描边参数

> **注意**: 以下参数仅存在于 `Physically Based + Outline` 版本的 Shader

### `_Outline` — 描边模式

| 枚举 | 名称 | 原理 | 缺点 |
|------|------|------|------|
| 1 | **Hull Object** | 将网格沿**自身法线**方向翻转扩展 | 透视缩短时近处描边变粗（foreshortening）|
| 2 | **Hull Origin** | 从**对象空间原点**沿顶点到原点的方向扩展 | 要求模型居中原点；否则描边不均匀 |
| 3 | **Hull Clip** | 在**裁剪空间(Clip Space)**进行扩展优化 | **推荐模式**——像素完美、避免 foreshortening |

**三种模式的数学本质**（都基于 Inverted Hull 技术）:

```glsl
// Hull Object: 沿法线方向移动顶点
float3 outlinePos = pos + normal * _OutlineSize * 0.005;

// Hull Origin: 沿 "从原点到顶点" 方向移动
float3 dir = normalize(pos - objectOrigin);
float3 outlinePos = pos + dir * _OutlineSize * 0.005;

// Hull Clip: 在 clip space (NDC) 中处理，避免透视变形
oPos = mul(UNITY_MATRIX_VP, float4(pos, 1.0)); // 转到裁剪空间
oPos.xy += normalize(oPos.xy) * _OutlineSize * 0.001 * oPos.w; // 与深度关联的偏移
```

> **参考**: [SillyVertexShader - Outlines - Unity Forums](https://forum.unity.com/threads/sillyvertexshader-outlines.164779/) —— Inverted Hull 描边的经典讨论

### `_OutlineData` — 描边数据来源

| 枚举 | 说明 |
|------|------|
| 0 (**Normal**) | 使用 mesh 原始的法线数据。描边形状完全由几何法线决定 |
| 1 (**Baked**) | 使用 **UV7 通道**中烘焙的自定义法线数据。可用 MK Mesh Utility 工具生成平均法线到 UV7 |

**Baked 法线的优势**: 原始法线在硬边缘处变化剧烈（相邻三角形法线差异大），导致描边粗细不均。Baked 法线经过平滑/平均处理后描边更均匀。**仅适用于静态网格**。

### 其余描边参数

| 属性 | 范围/类型 | 默认值 | 说明 |
|------|----------|--------|------|
| `_OutlineWidth` (Inspector: Width) | Float+ | 5.0 | 描边宽度。可用贴图 R 通道控制不同区域的宽度变化 |
| `_OutlineMap` | Texture2D | `"white" {}` | 描边宽度贴图（R通道 = 宽度倍数）|
| `_OutlineColor` | Color(RGBA) | `(0,0,0,1)` | 描边颜色 + Alpha=透明度（Alpha < 1 可做半透明描边！）|
| `_OutlineClipOffset` | Range(0,1) | 0.0 | 在 Clip Space 中将描边推离相机（减少内部穿帮线）|
| `_OutlineConstantSize` | Toggle | Off | 保持描边像素宽度恒定（不受距离影响）|
| `_OutlineNoise` | Range(-1,1) | 0.0 | 描边噪声扰动（负值=反向/收缩效果）|
| `_OutlineFadeMin/Max` | Float | 0.25 / 2.0 | 距离衰减范围（需全局开启 Outline Fading）|

**描边宽度的单位转换**: 
```
实际像素偏移 ≈ _OutlineSize * 0.005 * (screenHeight? or depth related?)
// 在 HullClip 模式中与 w (depth) 相关联，远处自动变细
// Constant Size 开启后使用固定的 screen-space offset
```

> **Noise 参数技巧**: 设为负值（如 -0.5）可以让某些区域的描边"内缩"，产生不规则的手绘描边效果。

---

## 12. 附录：图形学原理解释

### 12.1 什么是 Toon Shading（卡通着色）？

Toon Shading（也称为 Cel-Shading、NPR - Non-Photorealistic Rendering）是一类**非真实感渲染技术**，其核心思想是将传统渲染中**连续的光照梯度**离散化为**有限数量的色阶**，从而产生绘画/动漫/插画般的视觉风格。

> **延伸阅读**: [Toon Shading - Wikipedia](https://en.wikipedia.org/wiki/Cel-shading) | [GPU Gems 2 - Photorealistic to Nonphotorealistic Rendering](https://developer.nvidia.com/gpugems/gpugems2/part-four.html)

### 12.2 PBR + Toon 的结合为何有意义？

传统的 Toon Shader 是"纯手工"的，缺乏物理数据的支持。MKToon 的创新在于：

1. **保留 PBR 输入**: Metal/Roughness/Normal 等物理贴图仍然生效
2. **在光照计算的最后阶段**进行离散化（而非在输入阶段就丢弃物理信息）
3. **结果**: 材质在不同光照环境下都能正确响应（虽然被风格化了），同时保持了 PBR 的灵活性和兼容性

### 12.3 Ramp 贴图的本质

Ramp 贴图本质上是一个**一维查找表 (1D LUT)**，将连续的光照值映射到任意的颜色/强度曲线：

```
光照值 (0~1) -----> [Ramp贴图采样] -----> 输出强度/颜色

线性渐变 Ramp: ████████████░░░░░░░░░  (标准PBR效果)
硬切割 Ramp:   ██████████░░░░░░░░░░░  (Cel效果)
阶梯 Ramp:    ████░░░░░░████░░░░░░░  (Banded效果)
自定义曲线:   ██░▒░▓░█░░▒░█░▓░█░  (自由控制)
```

### 12.4 Inverted Hull 描边技术的原理

Inverted Hull（反转外壳）是目前游戏开发中最常用的**实时描边方案**之一：

```
步骤1: 将模型的三角形全部翻转（Front Face → Back Face）
步骤2: 沿法线（或其他方向）向外扩展顶点
步骤3: 只绘制背面（Cull Front），所以看到的是"向外膨胀了一层"的外壳
步骤4: 这个外壳就是描边
```

**优势**: 实时、无需后处理、完美贴合物体轮廓
**劣势**: 硬开销（几乎翻倍 draw call）、硬边处可能有穿帮、透视变形

> **替代方案对比**: 后处理描边（Sobel/Roberts 算子检测边缘 + dilation）——便宜但不精确；Geometry Shader 描边——精确但有限制。

### 12.5 Wrapped Lighting（包裹光照）的重要性

标准 Lambert 漫反射公式 `max(0, dot(N, L))` 在 N·L < 0 时返回 0（完全黑暗）。这对于卡通渲染来说太暗了——暗部变成纯黑色。

Wrapped Lighting 将 N·L 的 -1~1 范围映射到 0~1 的更大范围：
```
wrapped = (N·L * wrap + wrap) / (2 * (1 + wrap))

wrap=1 时: 范围变为 0~1 (原范围的一半, 但最小值不是0)
wrap=0.5 时: 范围约为 0.25~0.75 (暗部仍有 25% 的基础亮度)
```

这就是为什么 MKToon 默认开启 Wrapped Lighting 且推荐保持开启。

---

> **文档结束**。本参考手册基于 MKToon 源码逆向分析编写，所有参数说明均来自源码中的 Property 定义、Editor Tooltip、枚举值和 HLSL 实现。如发现任何不准确之处，欢迎指正。
