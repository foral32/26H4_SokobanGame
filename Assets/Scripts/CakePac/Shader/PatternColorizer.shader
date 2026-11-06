Shader "CakePac/PatternColorizer"
{
    Properties
    {
        [Header(Pattern)]
        _MainTex ("Pattern Texture", 2D) = "white" {}
        
        [Header(Colors)]
        _MainColor ("Main Color (White Areas)", Color) = (0.9960784, 0.7647059, 0.2980392, 1) // 淡橙色
        _SecondaryColor ("Secondary Color (Transparent Areas)", Color) = (1.0, 0.9294118, 0.6509804, 1) //  浅黄色
        [Toggle] _InvertColors ("Invert Main/Secondary Colors", Float) = 0
        
        [Header(Auto Color Calculation)]
        [Toggle] _UseAutoCalculation ("Use Auto Color Calculation", Float) = 0
        _SaturationOffset ("Saturation Offset", Range(-1, 1)) = -0.2
        _BrightnessOffset ("Brightness Offset", Range(-1, 1)) = 0
        
        [Header(Transform)]
        _RotationAngle ("Rotation Angle (Degrees)", Range(-180, 180)) = 0
        _RotationCenter ("Rotation Center", Vector) = (0.5, 0.5, 0, 0)
        
        [Header(Animation)]
        _ScrollSpeed ("Scroll Speed", Vector) = (0, 0, 0, 0)
        
        [Header(Rendering)]
        _Alpha ("Alpha", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
        }
        
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainColor;
            float4 _SecondaryColor;
            float _InvertColors;
            float _SaturationOffset;
            float _BrightnessOffset;
            float _UseAutoCalculation;
            float _RotationAngle;
            float4 _RotationCenter;
            float2 _ScrollSpeed;
            float _Alpha;
            
            // RGB转HSV
            float3 rgb2hsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }
            
            // HSV转RGB
            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }
            
            // 通过主色的饱和度、亮度偏移计算副色
            float3 calculateSecondaryColor(float3 mainColor, float satOffset, float brightOffset)
            {
                float3 hsv = rgb2hsv(mainColor);
                hsv.y = saturate(hsv.y + satOffset); // 饱和度偏移
                hsv.z = saturate(hsv.z + brightOffset); // 亮度偏移
                return hsv2rgb(hsv);
            }
            
            // UV旋转函数
            float2 rotateUV(float2 uv, float angle, float2 center)
            {
                // 转换角度为弧度
                float radian = radians(angle);
                float cosA = cos(radian);
                float sinA = sin(radian);
                
                // 将UV移动到旋转中心
                uv -= center;
                
                // 应用旋转矩阵
                float2 rotatedUV;
                rotatedUV.x = uv.x * cosA - uv.y * sinA;
                rotatedUV.y = uv.x * sinA + uv.y * cosA;
                
                // 移回原位置
                rotatedUV += center;
                
                return rotatedUV;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // 处理UV变换
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // 在顶点着色器中添加滚动偏移
                o.uv.x += _Time * _ScrollSpeed.x;
                o.uv.y += _Time * _ScrollSpeed.y;
                
                o.color = v.color;
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 应用旋转变换
                float2 rotatedUV = rotateUV(i.uv, _RotationAngle, _RotationCenter.xy);
                
                // 采样模板纹理（使用旋转后的UV）
                fixed4 pattern = tex2D(_MainTex, rotatedUV);
                
                // 确定主色和副色
                float3 color1 = _MainColor.rgb;
                float3 color2;
                
                if (_UseAutoCalculation > 0.5)
                {
                    // 通过主色的饱和度、亮度偏移自动计算副色
                    color2 = calculateSecondaryColor(color1, _SaturationOffset, _BrightnessOffset);
                }
                else
                {
                    // 使用指定的副色
                    color2 = _SecondaryColor.rgb;
                }
                
                // 根据反转开关决定哪个是主色哪个是副色
                float3 mainColor = lerp(color1, color2, _InvertColors);
                float3 secondaryColor = lerp(color2, color1, _InvertColors);
                
                // 使用alpha通道判断：alpha高的用主色，alpha低的用副色
                float3 finalColor = lerp(secondaryColor, mainColor, pattern.a);
                
                // 最终alpha
                float finalAlpha = _Alpha;
                
                // 应用顶点颜色
                finalColor *= i.color.rgb;
                finalAlpha *= i.color.a;
                
                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}