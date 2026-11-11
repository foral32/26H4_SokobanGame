Shader "DraculasBox/Board Light"
{
    Properties
    {
        _FloorTex ("Floor", 2D) = "white" {}
        _Before ("Previous field", 2D) = "black" {}
        _After ("Current field", 2D) = "black" {}
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
                half3 floor=SAMPLE_TEXTURE2D(_FloorTex,sampler_FloorTex,frac(tile)).rgb;
                if(_Mode>1.5)return half4(floor*lerp(_ShadeTint.rgb,_BaseTint.rgb,field.g),1);
                // One-cell footprint followed by precisely two cells of projected shade.
                // Min blending merges overlapping casters without accumulating darkness.
                float depth=saturate(max(0,i.uv.x-1)*.5);
                float strength=(.94-.64*pow(depth,1.15))*i.uv.y*(1-field.g);
                return half4(lerp(floor*_BaseTint.rgb,_ShadeTint.rgb,strength),1);
            }
            ENDHLSL
        }
    }
}
