Shader "DraculasBox/Vampire Burn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            // URP 14 (Unity 2022.3) has no Core2D.hlsl/2DCommon.hlsl; follow Sprite-Unlit-Default instead.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex BurnVertex
            #pragma fragment BurnFragment
            #pragma multi_compile_instancing
            struct Attributes
            {
                float3 positionOS:POSITION;
                half4 color:COLOR;
                float2 uv:TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                half4 color:COLOR;
                float2 uv:TEXCOORD0;
                float2 burnUV:TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);float4 _MainTex_ST;
            CBUFFER_START(UnityPerMaterial)
            float4 _BurnRect,_Direction;
            half _Heat,_Dissolve,_Flash;
            half4 _Color,_RendererColor;
            CBUFFER_END
            Varyings BurnVertex(Attributes input)
            {
                Varyings o=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS=TransformObjectToHClip(input.positionOS);
                o.uv=TRANSFORM_TEX(input.uv,_MainTex);
                o.color=input.color*_Color*_RendererColor;
                o.burnUV=(input.positionOS.xy-_BurnRect.xy)/max(_BurnRect.zw,.001)+.5;return o;
            }
            half4 BurnFragment(Varyings input):SV_Target
            {
                half4 ink=input.color*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv);
                float2 block=floor(input.burnUV*12);
                float noise=frac(sin(dot(block,float2(127.1,311.7)))*43758.5453);
                float progression=.20+dot(input.burnUV-.5,_Direction.xy)*.42+noise*.56;
                float remaining=progression-_Dissolve;
                ink.rgb=lerp(ink.rgb,ink.rgb*half3(.19,.15,.18),_Heat*.82);
                ink.rgb=lerp(ink.rgb,half3(1,.94,.76),_Flash*.68);
                float rim=(1-smoothstep(.012,.095,remaining))*step(.005,_Dissolve);
                ink.rgb=lerp(ink.rgb,half3(1,.76,.39),rim*.90);
                ink.a*=saturate(remaining/.018);return ink;
            }
            ENDHLSL
        }
    }
}
