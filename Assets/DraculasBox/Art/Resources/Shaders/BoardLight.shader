Shader "DraculasBox/Board Light"
{
    Properties
    {
        _FloorTex ("Floor", 2D) = "white" {}
        _Before ("Previous field", 2D) = "black" {}
        _After ("Current field", 2D) = "black" {}
        [HideInInspector] _RawShadow ("Solar coverage", 2D) = "black" {}
        [HideInInspector] _SoftShadow ("Solar gradient", 2D) = "black" {}
        [HideInInspector] _Size ("Board size", Vector) = (0,0,0,0)
        [HideInInspector] _BaseTint ("Sunlit floor", Color) = (1,1,1,1)
        [HideInInspector] _ShadeTint ("Shade", Color) = (0,0,0,1)
        [HideInInspector] _ClipRect ("Editor viewport", Vector) = (0,0,0,0)
        [HideInInspector] _Mode ("Field mode", Float) = 0
        [HideInInspector] _LightBlend ("Field blend", Float) = 1
        [HideInInspector] _ClipEnabled ("Clip editor viewport", Float) = 0
        _SrcBlend ("Source blend", Float) = 1
        _DstBlend ("Destination blend", Float) = 1
        _BlendOp ("Blend operation", Float) = 3
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Blend [_SrcBlend] [_DstBlend]
        BlendOp [_BlendOp]
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 local:TEXCOORD1; float2 world:TEXCOORD2; };
            TEXTURE2D(_FloorTex); SAMPLER(sampler_FloorTex);
            TEXTURE2D(_Before); SAMPLER(sampler_Before);
            TEXTURE2D(_After); SAMPLER(sampler_After);
            TEXTURE2D(_RawShadow); SAMPLER(sampler_RawShadow);
            TEXTURE2D(_SoftShadow); SAMPLER(sampler_SoftShadow);
            CBUFFER_START(UnityPerMaterial)
            float4 _Size, _BaseTint, _ShadeTint, _ClipRect;
            float _Mode, _LightBlend, _ClipEnabled;
            CBUFFER_END
            Varyings Vert(Attributes i)
            {
                Varyings o; float3 world=TransformObjectToWorld(i.positionOS);
                o.positionCS=TransformWorldToHClip(world);o.local=i.positionOS.xy;o.world=world.xy;o.uv=i.uv;return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                if(_ClipEnabled>.5)clip(min(min(i.world.x-_ClipRect.x,_ClipRect.z-i.world.x),min(i.world.y-_ClipRect.y,_ClipRect.w-i.world.y)));
                float2 tile=i.local+_Size.xy*.5;
                clip(min(min(tile.x,tile.y),min(_Size.x-tile.x,_Size.y-tile.y)));
                float2 fieldUV=tile/_Size.xy;
                half4 before=SAMPLE_TEXTURE2D(_Before,sampler_Before,fieldUV);
                half4 after=SAMPLE_TEXTURE2D(_After,sampler_After,fieldUV);
                clip(after.r-.5);
                half4 field=lerp(before,after,_LightBlend);
                if(_Mode>.5&&_Mode<1.5)
                {
                    float2 dist=abs(frac(tile)-.5);
                    float shaft=max(field.b*(exp(-dist.y*dist.y/0.008)),field.a*(exp(-dist.x*dist.x/0.008)));
                    float core=max(field.b*(1-smoothstep(.004,.016,dist.y)),field.a*(1-smoothstep(.004,.016,dist.x)));
                    return half4(lerp(half3(1,.82,.48),half3(1,.97,.82),core),saturate(shaft*.32+core*.58));
                }
                half3 floorColor=SAMPLE_TEXTURE2D(_FloorTex,sampler_FloorTex,frac(tile)).rgb;
                if(_Mode>1.5)return half4(floorColor*lerp(_ShadeTint.rgb,_BaseTint.rgb,field.g),1);
                float2 rawSize=_Size.xy*32;
                float2 rawUV=(floor(fieldUV*rawSize)+.5)/rawSize;
                half3 raw=SAMPLE_TEXTURE2D(_RawShadow,sampler_RawShadow,rawUV).rgb;
                half3 soft=SAMPLE_TEXTURE2D(_SoftShadow,sampler_SoftShadow,fieldUV).rgb;
                half coverage=SAMPLE_TEXTURE2D(_RawShadow,sampler_RawShadow,fieldUV).b;
                // Feather only the exterior of the complete union. Preserve the exact
                // two-cell footprint; filtering must never shade a dangerous sun cell.
                // Filtered strength gives the long gradient, not the silhouette. Use
                // only the raw field's one-texel edge for a consistent grid boundary.
                float contour=smoothstep(0,1,coverage)*step(.5,raw.g);
                float strength=soft.r*contour*raw.b*(1-field.g);
                return half4(lerp(floorColor*_BaseTint.rgb,_ShadeTint.rgb,strength),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "Solar union mask"
            Tags {"LightMode"="DraculasShadowMask"}
            ZTest Always
            Blend One One
            BlendOp Max
            ColorMask RGB
            HLSLPROGRAM
            #pragma vertex MaskVertex
            #pragma fragment MaskFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes {float3 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            float4 _Size;
            Varyings MaskVertex(Attributes i)
            {
                Varyings o;o.positionCS=float4(i.positionOS.xy/_Size.xy*2,0,1);
                #if UNITY_UV_STARTS_AT_TOP
                o.positionCS.y=-o.positionCS.y;
                #endif
                o.uv=i.uv;return o;
            }
            half4 MaskFragment(Varyings i):SV_Target
            {
                float depth=saturate(max(0,i.uv.x-1)*.5);
                return half4((.94-.64*pow(depth,1.15))*i.uv.y,1,i.uv.y,0);
            }
            ENDHLSL
        }
    }
}
