Shader "Hidden/PostProcessing/PostProcessOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineThickness ("Outline Thickness", Float) = 1
        _DepthThreshold ("Depth Threshold", Float) = 0.1
        _NormalThreshold ("Normal Threshold", Float) = 0.4
        _ColorThreshold ("Color Threshold", Float) = 0.2
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _DepthSensitivity ("Depth Sensitivity", Float) = 1
        _NormalsSensitivity ("Normals Sensitivity", Float) = 1
        _ColorSensitivity ("Color Sensitivity", Float) = 1
        _RobertsWeight ("Diagonal Weight", Float) = 2
        _MaxDiffWeight ("Depth Response Weight", Float) = 1.5
        _InternalDepthThreshold ("Internal Depth Threshold", Float) = 0.01
        _MinimumFeatureSize ("Minimum Feature Size (m)", Float) = 0.06
        _InternalEdgeFadeStart ("Internal Edge Fade Start", Float) = 15
        _InternalEdgeFadeEnd ("Internal Edge Fade End", Float) = 35
        _EdgeSoftness ("Edge Softness", Float) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "GenerateOutlineMask"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment GenerateOutlineMask
            #pragma multi_compile_local _ _USE_DEPTH_NORMALS
            #pragma multi_compile_local _ _USE_COLOR_DIFFERENCE
            #pragma multi_compile_local _ _ENABLE_ANTI_ALIASING
            #pragma multi_compile_local _ _USE_ENHANCED_DETECTION
            #pragma multi_compile_local _ _USE_INTERNAL_EDGE_DISTANCE_FADE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float _DepthThreshold, _NormalThreshold, _ColorThreshold;
            float _DepthSensitivity, _NormalsSensitivity, _ColorSensitivity;
            float _RobertsWeight, _MaxDiffWeight;
            float _InternalDepthThreshold, _InternalEdgeFadeStart, _InternalEdgeFadeEnd, _EdgeSoftness;
            float _MinimumFeatureSize;
            float _OutlineHasNormals;

            bool IsSkyDepth(float depth)
            {
                #if UNITY_REVERSED_Z
                    return depth <= 0.000001;
                #else
                    return depth >= 0.999999;
                #endif
            }

            float EyeDepth(float raw)
            {
                // LinearEyeDepth 本身只处理透视深度。
                float depth01 = raw;
                #if UNITY_REVERSED_Z
                    depth01 = 1.0 - raw;
                #endif
                float ortho = lerp(_ProjectionParams.y, _ProjectionParams.z, depth01);
                return lerp(LinearEyeDepth(raw, _ZBufferParams), ortho, unity_OrthoParams.w);
            }

            float3 WorldPosition(float2 uv, float raw)
            {
                #if !UNITY_REVERSED_Z
                    raw = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, raw);
                #endif
                return ComputeWorldSpacePosition(uv, raw, UNITY_MATRIX_I_VP);
            }

            float Threshold(float response, float threshold)
            {
                threshold = max(threshold, 0.00001);
                #ifdef _ENABLE_ANTI_ALIASING
                    // 不用 fwidth 把孤立尖峰的阈值放宽；零响应始终严格为零。
                    float width = min(max(_EdgeSoftness, 0.00001), threshold * .49);
                    return smoothstep(threshold - width, threshold + width, response);
                #else
                    return step(threshold, response);
                #endif
            }

            float InternalFade(float depth)
            {
                #ifdef _USE_INTERNAL_EDGE_DISTANCE_FADE
                    return 1.0 - smoothstep(_InternalEdgeFadeStart,
                        max(_InternalEdgeFadeEnd, _InternalEdgeFadeStart + .001), depth);
                #else
                    return 1.0;
                #endif
            }

            float3 ReadNormal(float2 uv)
            {
                float3 n = SampleSceneNormals(uv);
                return n * rsqrt(max(dot(n, n), .00001));
            }

            // 薄装饰面可能只占一个采样点；用一致的一对支持面，不能让单点侧面
            // 否决整个折角。曲面仍按支持面自身的曲率判断，不补强断线像素。
            float3 SupportedNormal(float3 n1, float3 n2, float3 n3, out float curvature, out float sampleOffset)
            {
                float d12 = dot(n1, n2), d23 = dot(n2, n3), d13 = dot(n1, n3);
                float3 a = n1, b = n2;
                float agreement = d12;
                sampleOffset = 2.0;
                if (d23 > agreement) { a = n2; b = n3; agreement = d23; sampleOffset = 3.0; }
                if (d13 > agreement) { a = n1; b = n3; agreement = d13; sampleOffset = 3.0; }
                curvature = max(1.0 - agreement, 0.0);
                return normalize(a + b + .000001);
            }

            float PlaneNormalError(float3 normal, float3 a, float3 b)
            {
                return 1.0 - max(dot(normal, a), dot(normal, b));
            }

            // 在固定的世界尺度上跨过边缘检查支撑面。两端近乎平行且高度差很小，
            // 说明这里是薄贴层/细槽，而不是需要强调的主体折角。不按物件或材质分类。
            float DetailSupport(float2 uv, float2 axis, float3 center, float depth)
            {
                if (_MinimumFeatureSize <= .00001) return 1.0;
                float2 pixel = _BlitTexture_TexelSize.xy;
                float footprint = 2.0 * lerp(depth, 1.0, unity_OrthoParams.w)
                    * pixel.y / max(abs(UNITY_MATRIX_P._m11), .0001);
                float radius = max(4.0, _MinimumFeatureSize * 2.0 / max(footprint, .00001));
                float2 offset = axis * (radius / max(length(axis / pixel), 1.0));
                float2 uvA = clamp(uv - offset, pixel * .5, 1.0 - pixel * .5);
                float2 uvB = clamp(uv + offset, pixel * .5, 1.0 - pixel * .5);
                float rawA = SampleSceneDepth(uvA), rawB = SampleSceneDepth(uvB);
                // 真实天空轮廓绝不能因为细节尺度而被删除。
                if (IsSkyDepth(rawA) || IsSkyDepth(rawB)) return 1.0;
                float3 nA = ReadNormal(uvA), nB = ReadNormal(uvB);
                float parallel = smoothstep(.95, .995, dot(nA, nB));
                float3 pA = WorldPosition(uvA, rawA), pB = WorldPosition(uvB, rawB);
                float planeStep = max(abs(dot(nA, pB - pA)), abs(dot(nB, pB - pA)));
                float centerGap = max(abs(dot(nA, center - pA)), abs(dot(nB, center - pB)));
                float smallStep = 1.0 - smoothstep(_MinimumFeatureSize * .5, _MinimumFeatureSize, planeStep);
                float nearby = 1.0 - smoothstep(_MinimumFeatureSize, _MinimumFeatureSize * 2.0, centerGap);
                return 1.0 - parallel * smallStep * nearby;
            }

            // 比较折线两侧的面，而不是某一对像素。连续曲率/细槽的两侧内部也在转向，
            // 不应当被当成两个稳定平面之间的硬折线。核固定在屏幕尺度，不随线宽改变。
            float StructuralNormal(float2 uv, float2 axis, out float coverage)
            {
                coverage = 0.0;
                float3 a1 = ReadNormal(uv - axis);
                float3 a2 = ReadNormal(uv - axis * 2.0);
                float3 a3 = ReadNormal(uv - axis * 3.0);
                float3 b1 = ReadNormal(uv + axis);
                float3 b2 = ReadNormal(uv + axis * 2.0);
                float3 b3 = ReadNormal(uv + axis * 3.0);
                float curvatureA, curvatureB, offsetA, offsetB;
                float3 a = SupportedNormal(a1, a2, a3, curvatureA, offsetA);
                float3 b = SupportedNormal(b1, b2, b3, curvatureB, offsetB);
                float across = 1.0 - dot(a, b);
                float confidence = saturate(across - max(curvatureA, curvatureB) * 4.0);
                // 两侧都缺乏完整面支持时，窄面必须属于同一对平面；
                // 不能用“任选两个最像的点”恢复曲面/柱槽噪线。
                float normalError = max(max(PlaneNormalError(a1, a, b), PlaneNormalError(a2, a, b)),
                    max(PlaneNormalError(a3, a, b), PlaneNormalError(b1, a, b)));
                normalError = max(normalError, max(PlaneNormalError(b2, a, b), PlaneNormalError(b3, a, b)));
                float sideAError = max(1.0 - dot(a1, a2), max(1.0 - dot(a2, a3), 1.0 - dot(a1, a3)));
                float sideBError = max(1.0 - dot(b1, b2), max(1.0 - dot(b2, b3), 1.0 - dot(b1, b3)));
                // 一侧三个点完整支持同一面时，对侧薄面交界/细缝只需一对稳定点。
                // 两侧都在连续转向的柱槽没有这个支持，仍不恢复内部碎线。
                float fullSideSupport = 1.0 - smoothstep(.0001, .001, min(sideAError, sideBError));
                confidence *= max(fullSideSupport, 1.0 - smoothstep(.0005, .002, normalError));
                if (confidence <= .0001) return 0.0;

                // 从折角两侧的稳定面重建交线，而不是把中心像素直接标成黑/白。
                // 因此浅斜台阶跨越像素行时，覆盖率会连续转移，不再整条线跳一格。
                float2 uvA = clamp(uv - axis * offsetA, _BlitTexture_TexelSize.xy * .5,
                    1.0 - _BlitTexture_TexelSize.xy * .5);
                float2 uvB = clamp(uv + axis * offsetB, _BlitTexture_TexelSize.xy * .5,
                    1.0 - _BlitTexture_TexelSize.xy * .5);
                float rawA = SampleSceneDepth(uvA), rawB = SampleSceneDepth(uvB);
                if (IsSkyDepth(rawA) || IsSkyDepth(rawB)) return 0.0;
                float3 pA = WorldPosition(uvA, rawA), pB = WorldPosition(uvB, rawB);
                float3 nA = normalize(a), nB = normalize(b);
                float cosine = dot(nA, nB);
                float denominator = 1.0 - cosine * cosine;
                if (denominator < .0001) return 0.0;
                float3 linePoint = pA + (nB - nA * cosine)
                    * (dot(nB, pB - pA) / denominator);
                float3 lineDirection = normalize(cross(nA, nB));
                float2 lineUV = ComputeNormalizedDeviceCoordinates(linePoint, UNITY_MATRIX_VP);
                float2 alongUV = ComputeNormalizedDeviceCoordinates(linePoint + lineDirection, UNITY_MATRIX_VP);
                float2 pixel = _BlitTexture_TexelSize.xy;
                float2 tangent = (alongUV - lineUV) / pixel;
                float tangentLength = length(tangent);
                if (tangentLength < .0001) return 0.0;
                float2 perpendicular = float2(-tangent.y, tangent.x) / tangentLength;
                float distance = abs(dot((uv - lineUV) / pixel, perpendicular));
                #ifdef _ENABLE_ANTI_ALIASING
                    coverage = saturate(1.0 - distance);
                #else
                    coverage = step(distance, .5);
                #endif
                // 深度支持来自这一对稳定面，不再受中心像素的前/后侧归属影响。
                float footprint = 2.0 * lerp(EyeDepth(rawA), 1.0, unity_OrthoParams.w)
                    * pixel.y / max(abs(UNITY_MATRIX_P._m11), .0001);
                float planeGap = max(abs(dot(nA, pB - pA)), abs(dot(nB, pA - pB)));
                coverage *= Threshold(planeGap / max(footprint * 25.0, .0001), _InternalDepthThreshold);
                return confidence * (1.0 - smoothstep(1.0, 2.0, distance))
                    * DetailSupport(uv, axis, linePoint, EyeDepth(rawA));
            }

            float Occlusion(float2 uv, float2 pixel, float raw, float depth, float3 normal)
            {
                float3 center = WorldPosition(uv, raw);
                float footprint = 2.0 * lerp(depth, 1.0, unity_OrthoParams.w)
                    * pixel.y / max(abs(UNITY_MATRIX_P._m11), .0001);
                float result = 0.0;
                float2 directions[8] = {
                    float2(-1,0), float2(1,0), float2(0,-1), float2(0,1),
                    float2(-1,-1), float2(1,-1), float2(-1,1), float2(1,1)
                };
                #ifdef _USE_ENHANCED_DETECTION
                    const int count = 8;
                #else
                    const int count = 4;
                #endif
                [unroll] for (int i = 0; i < count; i++)
                {
                    float2 neighborUV = clamp(uv + directions[i] * pixel, pixel * .5, 1.0 - pixel * .5);
                    float neighborRaw = SampleSceneDepth(neighborUV);
                    if (IsSkyDepth(neighborRaw))
                    {
                        result = 1.0;
                        continue;
                    }
                    float neighborDepth = EyeDepth(neighborRaw);
                    // 归属判定针对这一对遮挡像素，而不是要求中心为两侧局部深度极小值。
                    if (depth > neighborDepth + max(depth * .0001, .001)) continue;
                    float3 delta = WorldPosition(neighborUV, neighborRaw) - center;
                    // 同一斜面上的深度坡度不构成遮挡。以世界像素足迹归一化，
                    // 避免同样的深度间隔在近远距离产生完全不同的判定。
                    float planeError = abs(dot(normal, delta));
                    float response = planeError / max(footprint * 25.0, .0001);
                    // 连续曲面上由插值法线/细槽引起的小残差不升级成永不淡出的遮挡线。
                    float gap = neighborDepth - depth;
                    float gapThreshold = max(footprint * 2.0, depth * _DepthThreshold * .1);
                    // 近地接触处，眼空间深度间隔可能很小，但法线方向的真实间隔仍明显。
                    float separation = smoothstep(gapThreshold * .5, gapThreshold, max(gap, planeError));
                    float diagonalWeight = i >= 4 ? max(_RobertsWeight * .5, .001) : 1.0;
                    response *= _DepthSensitivity * _MaxDiffWeight * diagonalWeight;
                    float candidate = Threshold(response, _DepthThreshold) * separation;
                    if (candidate > result)
                    {
                        // 单侧竖面残差会把掠射细槽放大成“大遮挡”。用双方平面的
                        // 最小真实分离判断尺度，不把沿地面的远距离误当成槽深。
                        float significance = 1.0;
                        if (_MinimumFeatureSize > .00001)
                        {
                            float3 neighborNormal = ReadNormal(neighborUV);
                            float physicalGap = dot(neighborNormal, neighborNormal) > .5
                                ? min(planeError, abs(dot(neighborNormal, delta))) : planeError;
                            significance = smoothstep(_MinimumFeatureSize * .5, _MinimumFeatureSize, physicalGap);
                        }
                        result = max(result, candidate * significance
                            * DetailSupport(uv, directions[i] * pixel, center, depth));
                    }
                }
                return result;
            }

            float ColorGradient(float2 uv, float2 pixel)
            {
                float3 tl = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + pixel * float2(-1,1)).rgb;
                float3 tm = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + pixel * float2(0,1)).rgb;
                float3 tr = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + pixel * float2(1,1)).rgb;
                float3 ml = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + pixel * float2(-1,0)).rgb;
                float3 mr = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + pixel * float2(1,0)).rgb;
                float3 bl = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + pixel * float2(-1,-1)).rgb;
                float3 bm = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + pixel * float2(0,-1)).rgb;
                float3 br = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + pixel * float2(1,-1)).rgb;
                float3 gx = tr + 2.0 * mr + br - tl - 2.0 * ml - bl;
                float3 gy = tl + 2.0 * tm + tr - bl - 2.0 * bm - br;
                return sqrt(dot(gx, gx) + dot(gy, gy));
            }

            float4 GenerateOutlineMask(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 pixel = _BlitTexture_TexelSize.xy;
                float raw = SampleSceneDepth(uv);
                if (IsSkyDepth(raw)) return 0.0;
                float silhouette = 0.0;
                float structure = 0.0;
                float color = 0.0;
                float fade = InternalFade(EyeDepth(raw));
                #ifdef _USE_DEPTH_NORMALS
                    if (_OutlineHasNormals > .5)
                    {
                        float3 normal = ReadNormal(uv);
                        silhouette = Occlusion(uv, pixel, raw, EyeDepth(raw), normal);
                        float coverageX, coverageY;
                        float responseX = StructuralNormal(uv, float2(pixel.x, 0), coverageX);
                        float responseY = StructuralNormal(uv, float2(0, pixel.y), coverageY);
                        float creaseX = Threshold(responseX * _NormalsSensitivity, _NormalThreshold);
                        float creaseY = Threshold(responseY * _NormalsSensitivity, _NormalThreshold);
                        // 相接的两面已有解析折线，不再叠一条按像素归属跳动的遮挡线。
                        silhouette *= 1.0 - max(creaseX, creaseY);
                        structure = max(creaseX * coverageX, creaseY * coverageY) * fade;
                    }
                #endif
                #ifdef _USE_COLOR_DIFFERENCE
                    color = Threshold(ColorGradient(uv, pixel) * _ColorSensitivity, _ColorThreshold) * fade;
                #endif
                // R：不衰减的遮挡轮廓；G：解析结构线；B：可选颜色边。不要在此合并，
                // 内部碎点的连续性过滤不能误删外轮廓。
                return float4(saturate(silhouette), saturate(structure), saturate(color), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "FilterOutlineSeeds"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FilterOutlineSeeds
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _OutlineThickness;

            float3 Mask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
            }

            float2 FilteredSeed(float2 uv)
            {
                float3 center = Mask(uv);
                float rasterEdge = max(center.r, center.b);
                [branch] if (center.g <= .00001) return float2(rasterEdge, 0.0);
                float2 pixel = _BlitTexture_TexelSize.xy;
                // 内部线需有一对相对邻居支持；对连续线保留，对孤立点不补强。
                float support = max(min(Mask(uv + float2(pixel.x, 0)).g, Mask(uv - float2(pixel.x, 0)).g),
                    min(Mask(uv + float2(0, pixel.y)).g, Mask(uv - float2(0, pixel.y)).g));
                support = max(support, max(min(Mask(uv + pixel).g, Mask(uv - pixel).g),
                    min(Mask(uv + pixel * float2(1,-1)).g, Mask(uv - pixel * float2(1,-1)).g)));
                // 仅衰减，不增强中心；相对一致性不改变作者配置的内部距离淡出强度。
                float continuity = smoothstep(.2, .6, support / max(center.g, .00001));
                return float2(rasterEdge, center.g * continuity);
            }

            float4 FilterOutlineSeeds(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 result = FilteredSeed(uv);
                float radius = max(clamp(_OutlineThickness, 0.0, 5.0) - 1.0, 0.0) * .5;
                [branch] if (radius > .001)
                {
                    // 在轮廓抗锯齿之前成线；默认 1px 不执行膨胀邻域。
                    int extent = (int)ceil(radius);
                    [loop] for (int y = -extent; y <= extent; y++)
                        [loop] for (int x = -extent; x <= extent; x++)
                        {
                            float weight = saturate(radius + 1.0 - length(float2(x,y)));
                            result = max(result, FilteredSeed(uv + float2(x,y) * _BlitTexture_TexelSize.xy) * weight);
                        }
                }
                // R/G 同为栅格边（G 提供 FXAA 亮度），B 为解析覆盖率结构线。
                return float4(result.x, result.x, result.y, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "CompositeOutline"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment CompositeOutline
            #pragma multi_compile_local _ _ENABLE_ANTI_ALIASING
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #ifdef _ENABLE_ANTI_ALIASING
                // 复用当前 URP 携带的 NVIDIA FXAA；只输入轮廓遮罩，不采样场景原色。
                #define FXAA_PC 1
                #define FXAA_HLSL_5 1
                #define FXAA_GATHER4_ALPHA 0
                #define FXAA_GREEN_AS_LUMA 1
                #define FXAA_QUALITY__PRESET 15
                #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/FXAA3_11.hlsl"
            #endif

            TEXTURE2D_X(_OutlineSourceTexture);
            float4 _OutlineColor;
            float _OutlineThickness;

            float4 CompositeOutline(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float4 original = SAMPLE_TEXTURE2D_X(_OutlineSourceTexture, sampler_LinearClamp, uv);
                if (_OutlineThickness <= 0.0) return original;
                float4 mask = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                #ifdef _ENABLE_ANTI_ALIASING
                    FxaaTex tex = {sampler_LinearClamp, _BlitTexture};
                    float silhouette = FxaaPixelShader(uv, mask, 0, tex, tex, tex,
                        _BlitTexture_TexelSize.xy, 0, 0, 0, .65, .125, .0312, 0, 0, 0, 0, 0).r;
                    float coverage = max(silhouette, mask.b);
                #else
                    float coverage = max(mask.r, mask.b);
                #endif
                // 只过滤描边覆盖率，不模糊场景，不按局部最大值重新强化噪声。
                original.rgb = lerp(original.rgb, _OutlineColor.rgb,
                    saturate(coverage * _OutlineColor.a * min(_OutlineThickness, 1.0)));
                return original;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ResolveOutlineEdgeColor"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment ResolveOutlineEdgeColor
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #define FXAA_PC 1
            #define FXAA_HLSL_5 1
            #define FXAA_GATHER4_ALPHA 0
            #define FXAA_GREEN_AS_LUMA 1
            #define FXAA_QUALITY__PRESET 15
            #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/FXAA3_11.hlsl"

            TEXTURE2D_X(_OutlineCoverageTexture);
            float _OutlineThickness;

            float EdgeCoverage(float2 uv)
            {
                float3 mask = SAMPLE_TEXTURE2D_X(_OutlineCoverageTexture, sampler_LinearClamp, uv).rgb;
                return max(mask.r, mask.b);
            }

            float4 ResolveOutlineEdgeColor(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float4 original = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (_OutlineThickness <= 0.0) return original;
                float2 pixel = _BlitTexture_TexelSize.xy;
                float coverage = EdgeCoverage(uv);
                coverage = max(coverage, max(EdgeCoverage(uv + float2(pixel.x, 0)), EdgeCoverage(uv - float2(pixel.x, 0))));
                coverage = max(coverage, max(EdgeCoverage(uv + float2(0, pixel.y)), EdgeCoverage(uv - float2(0, pixel.y))));
                if (coverage <= .0001) return original;
                FxaaTex tex = {sampler_LinearClamp, _BlitTexture};
                float3 resolved = FxaaPixelShader(uv, original, 0, tex, tex, tex,
                    pixel, 0, 0, 0, .5, .125, .0312, 0, 0, 0, 0, 0).rgb;
                // 仅几何描边及其紧邻像素，背景/纹理区域逐像素保留，Alpha 不参与解析。
                return float4(lerp(original.rgb, resolved, saturate(coverage * 4.0)), original.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
