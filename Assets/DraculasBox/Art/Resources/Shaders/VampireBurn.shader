Shader "DraculasBox/Vampire Burn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex BurnVertex
            #pragma fragment BurnFragment
            #pragma multi_compile_instancing
            struct Attributes {COMMON_2D_INPUTS half4 color:COLOR;};
            struct Varyings {COMMON_2D_OUTPUTS half4 color:COLOR;float2 burnUV:TEXCOORD3;};
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BurnRect,_Direction;
            half _Heat,_Dissolve,_Flash;
            CBUFFER_END
            Varyings BurnVertex(Attributes input)
            {
                SetUpSpriteInstanceProperties();input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
                Varyings o=CommonUnlitVertex(input);o.color=input.color*unity_SpriteColor;
                o.burnUV=(input.positionOS.xy-_BurnRect.xy)/max(_BurnRect.zw,.001)+.5;return o;
            }
            half4 BurnFragment(Varyings input):SV_Target
            {
                half4 ink=CommonUnlitFragment(input,input.color);
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
