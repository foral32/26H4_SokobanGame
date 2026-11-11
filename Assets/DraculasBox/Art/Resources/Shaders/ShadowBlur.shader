Shader "Hidden/DraculasBox/Shadow Field Filter"
{
    Properties {_MainTex("Union field",2D)="black"{}}
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Filter
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _BlurStep;
            half4 Filter(v2f_img i):SV_Target
            {
                half4 field=tex2D(_MainTex,i.uv)*.227027;
                field+=(tex2D(_MainTex,i.uv+_BlurStep.xy*1.384615)+tex2D(_MainTex,i.uv-_BlurStep.xy*1.384615))*.316216;
                field+=(tex2D(_MainTex,i.uv+_BlurStep.xy*3.230769)+tex2D(_MainTex,i.uv-_BlurStep.xy*3.230769))*.070270;
                return field;
            }
            ENDCG
        }
    }
}
