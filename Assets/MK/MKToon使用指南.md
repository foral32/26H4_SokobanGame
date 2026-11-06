# MKToon 完整使用指南

> **面向独游开发者的 MKToon Shader 插件使用文档**  
> 基于 `MK/Toon` 插件源码深度分析编写 | 适用于 URP (Universal Render Pipeline)  
> 作者: Michael Kremmel | [www.michaelkremmel.de](https://www.michaelkremmel.de)  
> Asset Store: [MK Toon - Toon Shading](https://assetstore.unity.com/packages/tools/particles-effects/mk-toon-164524)

---

## 目录

1. [插件概述](#1-插件概述)
2. [安装与配置](#2-安装与配置)
3. [Shader 变体总览](#3-shader-变体总览)
4. [Inspector 面板详解](#4-inspector-面板详解)
5. [核心工作流程：从零到出图](#5-核心工作流程从零到出图)
6. [典型材质调参范例](#6-典型材质调参范例)
7. [进阶技巧与性能优化](#7-进阶技巧与性能优化)
8. [常见问题 FAQ](#8-常见问题-faq)
9. [附录：全局功能开关](#9-附录全局功能开关)

---

## 1. 插件概述

### 1.1 什么是 MKToon？

MKToon 是一个**基于物理的卡通渲染（PBR + Toon/NPR）Shader 系统**，专为 Unity URP（Universal Render Pipeline）设计。它将传统 PBR 的金属度/粗糙度工作流与卡通渲染的色阶化（Cel-Shading）、描边（Outline）、艺术风格（Artistic）等特性深度融合。

### 1.2 核心特性一览

| 特性类别 | 具体能力 |
|---------|---------|
| **PBR 工作流** | Metallic / Specular / Roughness 三种工作流 |
| **光照风格** | Builtin（内置） / Cel（单 cutoff） / Banded（多 band） / Ramp（渐变贴图控制） |
| **描边系统** | Hull Object / Hull Origin / Hull Clip 三种模式，支持距离衰减、噪声、贴图控制宽度 |
| **透光/散射** | Translucent（半透明透光） + Subsurface Scattering（次表面散射） |
| **艺术效果** | Drawn（手绘感）/ Hatching（排线）/ Sketch（素描）三种艺术化模式 |
| **顶点动画** | Sine（正弦波）/ Pulse（脉冲）/ Noise（噪声） |
| **溶解特效** | 基础溶解 + 边缘颜色 / 边缘渐变贴图 |
| **菲涅尔边缘光** | Default（全局） / Split（亮暗分离）两种 Rim 模式 |
| **彩虹色（Iridescence）** | 基于视角的彩虹色效果，支持自定义渐变贴图 |
| **Gooch 着色** | 经典 Gooch 艺术着色算法实现 |
| **后期调色** | Contrast / Hue / Saturation / Brightness 可作用于 Albedo 或 Final Output |
| **细节纹理** | Detail Map + Detail Normal Map，支持 Mix/Add/Multiply 混合 |
| **视差映射** | Height Map + Parallax 视差效果 |

### 1.3 适用场景

- 日系/韩系二次元风格游戏（角色、场景）
- 低多边形（Low Poly）风格独立游戏
- 手绘/水彩风格游戏
- 需要 PBR 物理准确性但又要求风格化表现的项目
- 需要**程序化描边**（非后处理描边）的项目

---

## 2. 安装与配置

### 2.1 导入插件

通过 Unity Package Manager 或 `.unitypackage` 导入。导入后会自动弹出 **MK Install Wizard** 配置窗口。

### 2.2 Install Wizard 配置

首次导入时需要完成以下设置：

1. **选择渲染管线**: 选择 `Universal` (URP) 或 `Built-in`
2. **导入基础 Shader 包**: 点击 Import 对应管线的 Shader 包
3. **导入示例包**（可选）: 包含示例场景和预设材质
4. **全局功能开关**（Global Shader Features）:

重要全局功能说明：

| 功能 | 推荐设置 | 说明 |
|------|---------|------|
| **Outline Distance Fading** | Linear 或 Exponential | 描边距离衰减，避免近处描边过粗 |
| **Force Point Filtering** | Off | 强制点过滤（像素风时开启）|
| **Enable Local Antialiasing** | On | 局部抗锯齿，推荐开启 |
| **Detail UV Sets** | Off | 开启后可使用 UV7 作为 Detail 独立 UV |
| **Stylize System Shadows** | Off | 让系统阴影也受卡通化影响（慎用）|

### 2.3 URP Renderer 设置（重要！）

如果你使用 **带 Outline 的 Shader**（如 `Physically Based + Outline`），**必须**在 URP Renderer 中添加 **MK Toon Per Object Outlines** Renderer Feature：

1. 选择你的 URP Renderer Asset
2. 在 Renderer Features 列表中点击 `Add Renderer Feature`
3. 添加 `MK Toon Per Object Outlines`
4. 否则 Outline Pass 不会被渲染

> 来源: `Configuration.cs` → `ShowURPOutlineWarning()` 方法中的检测逻辑

---

## 3. Shader 变体总览

MKToon 在 URP 下提供以下主要 Shader（位于 `MK/Toon/URP/Standard/` 目录）：

| Shader 名称 | 路径 | 特点 | 适用场景 |
|------------|------|------|---------|
| **Physically Based** | `StandardPBS` | 完整 PBR + 卡通化，无描边 | 不需要描边的角色/物体 |
| **Physically Based + Outline** | `StandardPBSOutline` | 完整 PBR + 卡通化 + **描边** | 角色、需要描边轮廓的物体 |
| **Simple** | `StandardSimple` | 简化版，无 Metal/Roughness 工作流切换 | 性能敏感场景 |
| **Unlit** | `StandardUnlit` | 无光照计算 | UI、全自发光物体 |

### 渲染Pass结构（以 PBS+Outline 为例）

每个 Shader 包含多个 SubShader 以支持不同 Shader Model（SM 4.5 / SM 3.5 / SM 2.5），每个 SubShader 包含：

```
ForwardBase (UniversalForward / UniversalForwardOnly)
├── ShadowCaster
├── Meta (Lightmap Baking)
├── Outline (MKToonOutline)          ← 仅 PBS+Outline 版本
├── DepthOnly
├── DepthNormals / DepthNormalsOnly
├── MotionVectors                    ← SM 4.5/3.5 only
├── XRMotionVectors                 ← URP 17+ only
└── Universal2D
```

> **Shader Target 要求**: SM 4.5 (推荐) / SM 3.5 (GL Core/GLES3) / SM 2.5 (移动端 GLES)

---

## 4. Inspector 面板详解

MKToon 的 Material Inspector 分为 **5 个主标签页**（Tab）：

```
┌─────────────────────────────────────────────┐
│  Options │ Input │ Stylize │ Advanced │ Outline │
│  (选项)   │(输入)│(风格化)  │(高级)   │(描边)   │
└─────────────────────────────────────────────┘
```

---

### 4.1 Options（选项）

基础渲染设置，定义材质的"身份"。

#### Workflow（工作流）
- **Metallic** (=0): 金属度工作流 — 使用 `_Metallic` + `_Smoothness`
- **Specular** (=1): 高光工作流 — 使用 `_SpecularColor`(或SpecularMap A通道) + `_Smoothness`
- **Roughness** (=2): 粗糙度工作流 — 使用 `_Metallic` + `_Roughness`（或对应Map）

> **建议**: 角色用 Specular 或 Metallic，环境物用 Roughness

#### Surface（表面类型）
- **Opaque** (=0): 不透明 — 写入深度缓冲，投射/接收阴影
- **Transparent** (=1): 透明 — 不写入深度，不投射阴影

#### Blending（混合模式）
仅在 Surface=Transparent 时生效：
- **Alpha** (=0): 标准 Alpha 混合 (`SrcAlpha, OneMinusSrcAlpha`)
- **Premultiply** (=1): 预乘 Alpha，保留高光和反射
- **Additive** (=2): 叠加混合 (`SrcAlpha, One`)
- **Multiply** (=3): 正片叠底 (`DstColor, Zero`)
- **Custom** (=4): 自定义 Blend Factor

#### Render Face（渲染面）
- **Front** (=2): 剔除背面（默认）
- **Back** (=1): 剔除正面（用于内部视角）
- **Double Sided** (=0): 双面渲染

#### Alpha Clipping（Alpha裁切）
开启后材质变为 Cutout 模式，Render Queue 切换到 `AlphaTest`(2450)。像素 Alpha 低于 Cutoff 值将被丢弃。

#### ZWrite / ZTest
- **ZWrite**: 控制是否写入深度缓冲（Transparent 时通常关闭）
- **ZTest**: 深度测试方式（LessEqual / Less / Equal / Greater 等）

---

### 4.2 Input（输入）

材质的基础属性输入。

#### 主纹理区域
| 属性 | Uniform | 说明 |
|------|---------|------|
| **Color** | `_AlbedoColor` | 基础色调 (RGBA) |
| **Albedo** | `_AlbedoMap` | 漫反射贴图 (RGBA)，A通道影响透明度 |
| **Albedo Intensity** | `_AlbedoMapIntensity` | Albedo 贴图的影响强度 0~1 |
| **Alpha Cutoff** | `_AlphaCutoff` | Alpha 裁切阈值 0~1 |

#### PBS 属性（根据 Workflow 变化）
| Workflow | 显示属性 | 说明 |
|----------|---------|------|
| **Specular** | Specular(Map) + Smoothness | 高光颜色贴图(A=光滑度) + 光滑度 |
| **Metallic** | Metallic(Map) + Smoothness | 金属度贴图(A=光滑度) + 光滑度 |
| **Roughness** | Metallic(Map) + Roughness(Map) | 金属度 + 粗糙度贴图 |

> **默认值参考**: SpecularColor = `(0.203125, 0.203125, 0.203125, 1)` （约 20% 灰色的标准 PBR 默认高光色）

#### 法线与高度
| 属性 | Uniform | 说明 |
|------|---------|------|
| **Normals** | `_NormalMap` | 切线空间法线贴图 |
| **Normal Map Scale** | `_NormalMapIntensity` | 法线强度 |
| **Height** | `_HeightMap` | 高度图（用于视差映射），R通道控制高度强度 |
| **Height Scale** | `_Parallax` | 视差映射强度 0~0.1 |

#### 透光/散射（Light Transmission）
| 属性 | Uniform | 说明 |
|------|---------|------|
| **Mode** | `_LightTransmission` | Off / Translucent / SSS |
| **Thickness** | `_ThicknessMap` | 厚度贴图，R通道: 0=厚(不透光), 1=薄(全透光) |
| **Color** | `_LightTransmissionColor` | 透光颜色（默认橙黄色 `(1, 0.65, 0)` ）|
| **Distortion** | `_LightTransmissionDistortion` | 光沿法线扩散程度 0~1 |
| **Intensity** | `_LightTransmissionIntensity` | 透光强度（Advanced 标签页中）|

> **Translucent vs SSS**: Translucent 是简单的背光透过效果；SSS 是模拟光线在材质内部散射的效果（更真实但更贵）

#### 遮蔽与发射
| 属性 | Uniform | 说明 |
|------|---------|------|
| **Occlusion** | `_OcclusionMap` | 遮蔽贴图: R=间接光, G=直射光 |
| **Occlusion Intensity** | `_OcclusionMapIntensity` | 遮蔽强度 0~1 |
| **Emission** | `_EmissionMap` | 自发光贴图 (RGB) |
| **Emission Color** | `_EmissionColor` | 自发光颜色 (HDR) |

#### Detail（细节纹理）
| 属性 | Uniform | 说明 |
|------|---------|------|
| **Detail** | `_DetailMap` | 细节贴图 (RGBA)，与 Albedo 叠加 |
| **Blend Mode** | `_DetailBlend` | Mix(插值) / Add(叠加) / Multiply(乘法) |
| **Color** | `_DetailColor` | 细节颜色 |
| **Mix** | `_DetailMix` | 混合比例 0~1 |
| **Detail Normals** | `_DetailNormalMap` | 细节法线贴图（自动与主法线合并）|
| **Normal Scale** | `_DetailNormalMapIntensity` | 细节法线强度 |
| **UV Set** | `_DetailUVSet` | UV0 或 UV7（需开启全局 Feature）|
| **Tiling / Offset** | — | Detail 贴图的 Tiling 和 Offset |

---

### 4.3 Stylize（风格化）

MKToon 最核心的标签页——所有卡通/风格化渲染参数都在这里。

#### 4.3.1 光照风格（Style）
| 属性 | Uniform | 说明 |
|------|---------|------|
| **Style** | `_Light` | Builtin / Cel / Banded / Ramp |

四种光照风格的含义：
- **Builtin** (=0): 标准 PBR 光照，不做任何卡通化处理
- **Cel** (=1): 经典 Cel-Shading，单一切割阈值
- **Banded** (=2): 多层色阶（Bands），可控制层数 2~12
- **Ramp** (=3): 使用 1D/2D 渐变贴图精确控制光影分布

#### 4.3.2 光照阈值与平滑度（Threshold & Smoothness）

这些参数在 Style 非 Builtin 时生效：

**Diffuse（漫反射）:**
| 属性 | 范围 | 说明 |
|------|------|------|
| `_DiffuseSmoothness` | 0~1 | 漫反射明暗交界平滑度。0=硬边，1=柔边 |
| `_DiffuseThresholdOffset` | 0~1 | 漫反射额外阈值偏移（叠加在主阈值之上）|

**Specular（镜面反射）:**
| 属性 | 范围 | 说明 |
|------|------|------|
| `_SpecularSmoothness` | 0~1 | 高光的平滑度 |
| `_SpecularThresholdOffset` | 0~1 | 高光的额外阈值偏移 |

**Rim（边缘光）:**
| 属性 | 范围 | 说明 |
|------|------|------|
| `_RimSmoothness` | 0~1 | 边缘光的平滑度 |
| `_RimThresholdOffset` | 0~1 | 边缘光的额外阈值偏移 |

**Light Transmission（透光/散射）:**
| 属性 | 范围 | 说明 |
|------|------|------|
| `_LightTransmissionSmoothness` | 0~1 | 透光效果的平滑度 |
| `_LightTransmissionThresholdOffset` | 0~1 | 透光的额外阈值偏移 |

#### 4.3.3 主光源阈值（Main Light Threshold）
| 属性 | Uniform | 范围 | 说明 |
|------|---------|------|------|
| **Main Light Threshold** | `_LightThreshold` | 0~1 | 主光源的明暗分界点。0=整体更亮，1=整体更暗 |
| **Additional Light Threshold** | `_AdditionalLightsThreshold` | 0~1 | 附加光源的阈值（点光源、聚光灯等）|
| **Additional Lights Falloff** | `_AdditionalLightsFalloff` | 0~1 | 附加光源衰减形状。0=范围一致，1=物理准确 |

> **调试技巧**: 先调整 Main Light Threshold 让主体光照正确，再用 Additional Lights Threshold 微调补光

#### 4.3.4 多层色阶（Bands）— 仅 Banded 模式
| 属性 | 范围 | 说明 |
|------|------|------|
| **Bands** | `_LightBands` | 2~12 | 色阶数量（层次数）|
| **Bands Scale** | `_LightBandsScale` | 0~1 | 色阶插值/推挤程度。值越大越向阴影区推进 |

> Bands=4 时呈现典型的"三色调"（亮-灰-暗）效果

#### 4.3.5 渐变贴图（Ramps）— 仅 Ramp 模式
| 属性 | Uniform | 说明 |
|------|---------|------|
| **Diffuse Ramp** | `_DiffuseRamp` | 漫反射渐变贴图。X轴=亮度（左暗右亮），Y轴=衰减（仅非定向光）|
| **Specular Ramp** | `_SpecularRamp` | 高光渐变贴图 |
| **Rim Ramp** | `_RimRamp` | 边缘光渐变贴图 |
| **Light Transmission Ramp** | `_LightTransmissionRamp` | 透光渐变贴图 |

> **Ramp 贴图格式**: 默认为灰色（线性）。你可以绘制自定义的 1D 渐变（存储在任意 2D 贴图的 R 通道中）来精确控制光影过渡。例如：在 R 通道中间画一条硬切线就能得到硬边 Cel 效果。

#### 4.3.6 阈值贴图（Threshold Map）
| 属性 | Uniform | 说明 |
|------|---------|------|
| **Threshold** | `_ThresholdMap` | 阈值贴图（R通道），叠加到全局光照阈值上 |
| **Scale** | `_ThresholdMapScale` | 蘸图缩放系数 |

> 用途：可以用一张贴图在不同区域实现不同的阴影边界位置（例如面部阴影更柔和、衣服阴影更硬）

#### 4.3.7 Gooch 着色
Gooch Shading 是一种经典的**艺术风格着色技术**，用自定义的亮色和暗色替代传统的明暗计算。

| 属性 | Uniform | 说明 |
|------|---------|------|
| **Ramp** | `_GoochRamp` | Gooch 色彩渐变（RGB），X轴亮度，不影响光强 |
| **Intensity** | `_GoochRampIntensity` | Gooch 效果混合强度 0~1 |
| **Bright** | `_GoochBrightMap` / `_GoochBrightColor` | 亮部颜色（贴图或纯色）|
| **Dark** | `_GoochDarkMap` / `_GoochDarkColor` | 暗部颜色（贴图或纯色）|
| **Fade With Indirect** | `_GoochDarkRemapFadeWithIndirect` | 暗部是否随环境光/GI 渐隐 |
| **Remap Dark** | Min/Max | 重映射暗部范围（仅 Fade With Indirect 开启时显示）|

> **典型用法**: Bright 设为暖色（淡黄）、Dark 设为冷色（蓝灰），营造类似水彩画的冷暖对比

#### 4.3.8 边缘光（Rim Lighting）
基于 Fresnel（菲涅尔）效应的边缘高光。

| 属性 | Mode | 说明 |
|------|------|------|
| **Rim** | `_Rim` | Off / Default / Split |
| **Size** | `_RimSize` | 边缘光范围 0~5 |
| **Color** | `_RimColor` | 颜色(RGB)+透明度(A)（Default 模式）|
| **Bright Color** | `_RimBrightColor` | 亮部边缘光颜色（Split 模式）|
| **Dark Color** | `_RimDarkColor` | 暗部边缘光颜色（Split 模式）|

> **Split 模式**: 亮部和暗部的边缘光可以分别设不同颜色——例如亮部用白色高光，暗部用暖色轮廓光

#### 4.3.9 彩虹色（Iridescence）
基于观察角度变化的色彩效果（类似肥皂泡/油膜）。

| 属性 | 范围 | 说明 |
|------|------|------|
| **Iridescence** | On/Off | 开启/关闭 |
| **Ramp** | — | 彩虹色渐变贴图（RGB），X轴=色彩变化 |
| **Size** | 0~5 | 效果大小/扩散范围 |
| **Color** | RGBA | 额外染色 + 透明度(A) |
| **Smoothness** | 0~1 | 过渡平滑度 |
| **Threshold Offset** | 0~1 | 额外阈值偏移 |

#### 4.3.10 后期调色（Color Grading）
| 属性 | 范围 | 应用阶段 |
|------|------|---------|
| **Contrast** | Float | 对比度 |
| **Hue** | 0~1 | 色相偏移 |
| **Saturation** | 0+ | 饱和度 |
| **Brightness** | 0+ | 亮度 |
| **Mode** | Off/Albedo/FinalOutput | Off=关闭; Albedo=对输入纹理调色; FinalOutput=对最终输出调色 |

#### 4.3.11 艺术效果（Artistic）
三种手绘风格化模式：

| 模式 | 说明 | 所需贴图 |
|------|------|---------|
| **Drawn** | 通过查找纹理(R) + 双阈值控制明暗，产生手绘感 | Drawn Map |
| **Hatching** | 排线效果，亮/暗各 3 个层级共 6 个通道 | Hatching Bright Map + Dark Map |
| **Sketch** | 素描效果，通过查找纹理(R)融入光照 | Sketch Map |

公共参数:
| 属性 | 说明 |
|------|------|
| **Projection** | Tangent Space（原始UV）/ Screen Space（屏幕空间投影）|
| **Frequency** | 动画频率（>1时有持续重绘动画效果）|
| **Scale** | 各贴图的缩放 |
| **Clamp Min/Max** | Drawn 模式的阴影/高光阈值控制 |
| **Shadow Filter** | 系统阴影上的覆盖强度（需全局开启 Stylize System Shadows）|

> **Hatching 通道顺序**: Dark Map: R→G→B (暗→较暗→最暗); Bright Map: R→G→B (亮→较亮→最亮)

#### 4.3.12 顶点动画（Vertex Animation）
| 模式 | 说明 |
|------|------|
| **Off** | 关闭 |
| **Sine** | 正弦波动画（对象空间）|
| **Pulse** | 脉冲式动画 |
| **Noise** | 噪声扰动动画 |

参数:
| 属性 | 范围 | 说明 |
|------|------|------|
| **Intensity** | 0~1 | 动画幅度 |
| **Frequency** | Vector3(X,Y,Z) | 各轴频率 |
| **Map** | — | 强度贴图（R通道微调）|
| **Stutter** | Toggle | 开启抖动效果 |

#### 4.3.13 溶解特效（Dissolve）
| 模式 | 说明 |
|------|------|
| **Off** | 关闭 |
| **Simple** | 基础溶解（贴图 + Amount 控制）|
| **Border Color** | 溶解 + 单色边缘线 |
| **Border Ramp** | 溶解 + 渐变贴图边缘（支持彩色过渡）|

参数:
| 属性 | 范围 | 说明 |
|------|------|------|
| **Dissolve Map** | — | 溶解布局贴图（R通道）|
| **Scale** | Float | 贴图缩放 |
| **Amount** | 0~1 | 0=完全可见, 1=完全溶解 |
| **Border Size** | 0~1 | 边缘宽度 |
| **Border Ramp** | — | 边缘渐变贴图（颜色从左到右读取）|
| **Border Color** | HDR | 边缘颜色（Border Color 模式）|

> **典型用途**: 死亡消失效果、传送门、魔法消散

#### 4.3.14 基础光照选项（折叠区域）
| 属性 | 默认值 | 说明 |
|------|--------|------|
| **Wrapped Lighting** | On | 开启包裹光照（漫反射更柔和，暗部不会全黑）|
| **Receive Shadows** | On | 是否接收阴影 |

---

### 4.4 Advanced（高级）

#### 4.4.1 光照高级设置
| 属性 | 选项 | 说明 |
|------|------|------|
| **Receive Shadows** | Toggle | 接收其他物体的阴影 |
| **Wrapped Lighting** | Toggle | 包裹光照（让暗部也有一定亮度）|
| **Diffuse** | Lambert/OrenNayar/Minnaert | 漫反射模型选择 |
| **Specular** | Off/Isotropic/Anisotropic | 高光模式 |
| **Anisotropy** | -1~1 | 各向异性拉伸方向和强度（仅 Anisotropic 模式）|
| **Light Transmission** | Off/Translucent/SSS | 透光模式选择 |
| **Light Transmission Intensity** | 0+ | 透光强度 |
| **Environment Reflections** | Off/Ambient/Advanced | 环境反射 |
| **Fresnel Highlights** | Toggle | PBR 菲涅尔高光 |

**漫反射模型对比**:
- **Lambert**: 最简单经典
- **Oren-Nayar**: 考虑粗糙度和视角（适合粗糙表面：石头、布料）
- **Minnaert**: 视角相关暗化（适合布料、织物类材质）

#### 4.4.2 渲染管线设置
| 属性 | 说明 |
|------|------|
| **Pipeline** | 当前使用的渲染管线信息（只读）|

#### 4.4.3 Stencil（模板测试）
| 属性 | 说明 |
|------|------|
| **Mode** | Builtin（Unity默认）/ Custom（自定义）|
| **Ref** | 参考值 0~255 |
| **Read Mask** | 读取掩码 |
| **Write Mask** | 写入掩码 |
| **Comp** | 比较函数（Always/Less/LEqual/Equal 等）|
| **Pass / Fail / ZFail** | 通过/失败/深度失败时的操作 |

> **用途**: 用于特殊效果（如遮罩、镂空、限制渲染区域）

#### 4.4.4 Indirect Fade（间接光衰减）
| 属性 | 范围 | 说明 |
|------|------|------|
| **Indirect Fade** | 0~1 | 间接光在阴影区的衰减。0=阴影区无间接光，1=完全影响 |

#### 4.4.5 Render Priority（渲染优先级）
| 范围 | 说明 |
|------|------|
| -50 ~ 50 | 调整 Render Queue 偏移量。值越大越先渲染 |

---

### 4.5 Outline（描边）

**仅 `+ Outline` 版本的 Shader 才有此标签！**

#### 描边模式（Outline Mode）
| 模式 | 原理 | 特点 | 注意事项 |
|------|------|------|---------|
| **Hull Object** | 沿法线翻转扩展网格 | 最常用、稳定 | 标准做法 |
| **Hull Origin** | 从对象空间原点扩展 | 几何体需居中 | 需要模型中心在原点 |
| **Hull Clip** | 在裁剪空间优化 | 像素完美、避免透视变形 | 推荐 |

#### 描边参数
| 属性 | 范围 | 说明 |
|------|------|------|
| **Data** | Normal/Baked | 使用原始法线 / UV7 中烘焙的法线数据 |
| **Width** | 0+ | 描边宽度（可用贴图 R 通道控制不同区域的宽度）|
| **Color** | RGBA | 描边颜色（RGB）+ 透明度（A）|
| **Offset** | 0~1 | 在裁剪空间中将描边推离相机（减少内部穿帮线）|
| **Constant Size** | Toggle | 保持描边宽度不随距离变化 |
| **Noise** | -1~1 | 描边噪声扰动 |
| **Fade Min/Max** | Float | 距离衰减范围（需全局开启 Outline Fading）|

> **Baked 法线**: 可以用 MK Mesh Utility 将平均法线烘焙到 UV7 通道，使描边更均匀（适合静态网格）

---

## 5. 核心工作流程：从零到出图

### 5.1 创建第一个 MKToon 材质

1. 在 Project 窗口右键 → **Create → Material**
2. 在 Inspector 中点击 Shader 下拉框
3. 选择 `MK/Toon/URP/Standard/Physically Based`（或 `+ Outline`）
4. 材质自动初始化为默认参数

### 5.2 角色材质标准流程

```
Step 1: Options
  └─→ Workflow = Specular, Surface = Opaque, Render Face = Front

Step 2: Input  
  └─→ 拖入 Albedo 贴图
  └─→ 拖入 Normal Map
  └─→ Workflow=Specular → 拖入 Specular Map, 调整 Smoothness
  └─→ 如需要 → 开启 Light Transmission (角色耳朵/头发)

Step 3: Stylize
  ├─→ Style = Cel (经典二次元)
  ├─→ 调整 Main Light Threshold (约 0.5)
  ├─→ 调整 Diffuse Smoothness (约 0.05~0.2, 微柔边)
  ├─→ 开启 Rim = Split, 调整颜色
  └─→ 开启 Outline (如果用 +Outline Shader)

Step 4: Advanced
  └─→ 开启 Wrapped Lighting (让暗部不至于太黑)
  └─→ Environment Reflections = Ambient (或 Advanced)
```

### 5.3 场景物件材质流程

```
Step 1-2: 同上（但可能选 Metallic/Roughness 工作流）

Step 3: Stylize
  ├─→ Style = Ramp (更多控制)
  ├─→ 绘制 Diffuse Ramp 贴图定制光影
  ├─→ 尝试 Gooch 着色获得艺术感
  └─→ 可能开启 Artistic → Hatching (排线风格)

Step 4: Advanced
  └─→ Diffuse = Oren-Nayar (粗糙表面)
```

---

## 6. 典型材质调参范例

### 6.1 日系二次元角色

| 参数组 | 推荐值 | 说明 |
|--------|--------|------|
| **Options** | Specular / Opaque / Front | 标准角色配置 |
| **Input: Albedo** | 角色贴图 | 确保贴图无过多光影 baked-in |
| **Input: Normal** | 法线贴图 | 保留服装褶皱纹理 |
| **Input: LT** | Translucent / 橙黄色 | 头发/耳朵透光 |
| **Stylize: Style** | **Cel** | 经典 cel-shading |
| **Stylize: Threshold** | **0.5~0.55** | 调整到阴影比例美观 |
| **Stylize: Diffuse Smooth** | **0.05~0.15** | 几乎硬边但有微量抗锯齿 |
| **Stylize: Specular Smooth** | **0.1~0.3** | 高光稍柔和 |
| **Stylize: Rim** | **Split** | 暗部边缘暖光 |
| **Stylize: Rim Dark Color** | `(1, 0.7, 0.3, 0.5)` | 暖橙色暗部轮廓 |
| **Outline: Mode** | **HullClip** | 像素完美的描边 |
| **Outline: Width** | **3~8** | 根据屏幕分辨率调整 |
| **Outline: Color** | `(0.05, 0.05, 0.08, 1)` | 深蓝黑色描边 |
| **Advanced: Wrapped Light** | **ON** | 暗部不完全黑 |
| **Advanced: Env Reflections** | **Ambient** | 有基本环境反应 |

### 6.2 手绘/水彩风格

| 参数组 | 推荐值 | 说明 |
|--------|--------|------|
| **Style** | **Banded** 或 **Ramp** | 更多色阶层次 |
| **Bands** | **3~4** | 三色或四色调 |
| **Artistic** | **Hatching** | 排线效果 |
| **Projection** | **Screen Space** | 排线跟随视角 |
| **Gooch** | **ON** | 艺彩冷暖对比 |
| **Gooch Bright** | 淡暖黄 | 亮部偏暖 |
| **Gooch Dark** | 冷灰蓝 | 暗部偏冷 |
| **Gooch Intensity** | **0.4~0.6** | 不要过强 |
| **Diffuse Model** | **Oren-Nayar** | 更柔和的漫反射 |
| **Color Grading** | **FinalOutput** | 最终输出降低饱和度 |

### 6.3 低多边形（Low Poly）风格

| 参数组 | 推荐值 | 说明 |
|--------|--------|------|
| **Style** | **Cel** | 干净利落 |
| **Main Threshold** | **0.5** | 明确的明暗分界 |
| **Diffuse Smooth** | **0.0** | 完全硬边 |
| **Rim** | **Default** | 强烈边缘光 |
| **Rim Color** | `(1,1,1,0.8)` | 白色强边缘 |
| **Rim Size** | **2.0~3.0** | 较宽的边缘 |
| **Specular** | **Off** | 去掉高光保持简洁 |
| **Outline** | **ON** | Low Poly 几乎必配描边 |
| **Outline Width** | **1.5~3** | 细描边 |

### 6.4 溶解死亡特效（运行时控制）

```csharp
// 通过代码控制 Dissolve
material.SetFloat("_DissolveAmount", dissolveProgress); // 0→1
material.SetTexture("_DissolveMap", dissolvePattern);
material.SetFloat("_DissolveBorderSize", 0.15f);
material.SetTexture("_DissolveBorderRamp", borderGradientRamp);
material.SetColor("_DissolveBorderColor", new Color(1, 0.5f, 0, 1)); // 橙红色边缘
```

---

## 7. 进阶技巧与性能优化

### 7.1 Shader Variant 管理

MKToon 使用大量 `#pragma shader_feature_local` 关键字，这意味着**每种特性组合都会生成一个 Shader Variant**。实际项目中建议：

1. 使用 **Shader Variant Collection** 剔除不需要的 variant
2. 只引入你实际使用的 Shader 变体
3. 移动端特别注意 variant 数量（SM 2.5 已做精简）

### 7.2 性能分级策略

| 平台 | 建议 Shader Model | 关闭的特性 |
|------|-------------------|-----------|
| PC/主机 | SM 4.5 (目标) | 全开 |
| 中端移动 | SM 3.5 | 关闭 Parallax / Motion Vectors |
| 低端移动 | SM 2.5 | 仅保留核心: Cel/Banded + Outline + Basic Maps |

SM 2.5 自动排除的特性（从源码确认）:
- 无 Normal Map 支持
- 无 Height/Parallax
- 无 Detail Normal Map
- 无 Anisotropic Specular
- 无 Motion Vectors
- 无 XR Motion Vectors

### 7.3 与其他系统配合

| 功能 | 配合方式 |
|------|---------|
| **URP Volume 后处理** | 宧全兼容，MKToon 写入正确的 RenderType Tag |
| **Lightmaps** | 支持 LIGHTMAP_ON，完全兼容 |
| **Shadow Masks** | 支持 SHADOWMASK |
| **SRP Batcher** | 兼容（使用 MaterialPropertyBlock 访问属性）|
| **LOD Crossfade** | 支持 LOD_FADE_CROSSFADE |
| **GPU Instancing** | 支持 `#pragma multi_compile_instancing` |
| **DDBuffer (Decals)** | 支持 _DBUFFER_MRT1/2/3 |

### 7.4 运行时通过代码修改材质

```csharp
using MK.Toon;

// 修改属性的标准方式（类型安全）
Properties.workflow.SetValue(material, Workflow.Specular);
Properties.albedoColor.SetValue(material, new Color(1, 0.5f, 0.5f, 1));
Properties.light.SetValue(material, Light.Cel);
Properties.rim.SetValue(material, Rim.Split);

// 读取当前值
Light currentLightStyle = Properties.light.GetValue(material);
float threshold = Properties.lightThreshold.GetValue(material);

// 修改 Outline
Properties.outline.SetValue(material, Outline.HullClip);
Properties.outlineSize.SetValue(material, 5.0f);
Properties.outlineColor.SetValue(material, new Color(0, 0, 0, 1));

// 溶解动画
Properties.dissolve.SetValue(material, Dissolve.BorderRamp);
Properties.dissolveAmount.SetValue material, Mathf.Lerp(0, 1, time));
```

### 7.5 从 Standard/Lit 材质转换

MKToon 编辑器内置了**一键转换功能**：在材质 Inspector 顶部会出现 **"Convert From..."** 按钮（当检测到材质来自非 MKToon Shader 时）。

支持的源 Shader：
- `Standard` / `Standard (Specular setup)`
- `Universal Render Pipeline/Lit`
- `Universal Render Pipeline/Simple Lit`
- `Autodesk Interactive`

转换会自动映射：
- MetallicGlossMap → Metallic Map / Specular Map
- Smoothness → Smoothness / Roughness
- Parallax → Height Map
- Occlusion → Occlusion Map
- Detail Albedo/Normal → Detail Map
- GlossyReflections → Environment Reflections

---

## 8. 常见问题 FAQ

### Q1: 描边没有显示？
**A**: 确认你在使用 `+ Outline` 版本的 Shader（如 `Physically Based + Outline`）。然后在 URP Renderer Asset 中添加 **MK Toon Per Object Outlines** Renderer Feature。

### Q2: 材质是粉色/洋红色？
**A**: 这是典型的 URP 未正确配置的表现。检查：
1. 项目是否使用 URP
2. URP Asset 是否分配在 Graphics Settings 中
3. 材质的 Pipeline 是否匹配

### Q3: 透明材质的描边不可见？
**A**: Outline 版本强制 Surface=Opaque。这是设计如此（避免 z-fighting）。如果需要透明+描边，考虑用两个材质/Renderer。

### Q4: 如何让阴影也是卡通化的？
**A**: 在 InstallWizard 中开启全局功能 **"Stylize System Shadows"**。然后注意调整阈值——阴影阈值和光照阈值可能需要分别调优。

### Q5: Lightmap 烘焙后的光照不受卡通化影响？
**A**: Lightmap 烘焙的是间接光，MKToon 的卡通化主要影响直接光照。可以通过 Ramp 贴图和 Gooch 着色来增强烘焙场景的风格化表现。

### Q6: 性能如何？能在手机上跑吗？
**A**: MKToon 提供 SM 2.5 回退，专为移动端优化。关键性能建议：
- 避免同时开启 Artistic + Iridescence + Dissolve
- 使用 Ramp 而非 Banded（少一次 step 操作）
- 关闭 Parallax 和 Motion Vectors
- 合理使用 Shader Variant Collection

### Q7: 如何制作自定义 Ramp 贴图？
**A**:
1. 创建一个 256x1 或 512x2 的 PNG
2. 从左到右绘制：黑色(暗) → 白色(亮)
3. 如果需要非线性过渡，可以在中间画阶梯或曲线
4. 导入 Unity，将 Filter Mode 设为 Clamp
5. 赋给对应的 Ramp 属性

在线 Ramp 制作工具: https://www.rorydriscolls.com/tools/gradientrampeditor/

---

## 9. 附录：全局功能开关

在 Unity 菜单中找到 **Window → MK → Toon → Install Wizard** 打开配置面板，其中的 Global Shader Features 列表：

| 功能 | 编译宏 | 推荐场景 |
|------|--------|---------|
| Multiply Vertex Color with Albedo | `MK_COMBINE_VERTEX_COLOR_WITH_ALBEDO_MAP` | 顶点色着色项目 |
| Outline Distance Fading | `MK_OUTLINE_FADING_LINEAR` / `_EXPONENTIAL` / `_INVERSE_EXPONENTIAL` | **强烈推荐开启** |
| Force Point Filtering | `MK_POINT_FILTERING` | 像素风/复古风 |
| Dissolve Screen Space Projection | `MK_DISSOLVE_PROJECTION_SCREEN_SPACE` | 屏幕空间溶解 |
| Enable Local Antialiasing | `MK_LOCAL_ANTIALIASING` | **推荐开启** |
| Detail UV Sets | `MK_ALLOW_DETAIL_MAPS_UV_7` | 需要 UV7 时 |
| Stylize System Shadows | `MK_STYLIZE_SYSTEM_SHADOWS` | 卡通化系统阴影 |
| Legacy Noise | `MK_LEGACY_NOISE` | 向后兼容 |
| Regular Screen Space UV | `MK_REGULAR_SCREEN_SPACE` | 屏幕空间 UV |
| Legacy Screen Scaling | `MK_LEGACY_SCREEN_SCALING` | 向后兼容 |
| Multi-Pass XR Scaling | `MK_MULTI_PASS_STEREO_SCALING` | VR Multi-Pass |
| Linear Light Attenuation | `MK_LINEAR_LIGHT_DISTANCE_ATTENUATION` | 类似旧管线衰减 |

---

> **文档版本**: 2026-04 | **基于源码版本**: MKToon for URP | **作者分析**: 源码逆向工程 + Editor Tooltip 提取
