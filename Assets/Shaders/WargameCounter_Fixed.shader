Shader "Custom/WargameCounter_Fixed"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        // _Color ("Base Color", Color) = (1, 1, 1, 1)
        _BorderColor ("Border Color", Color) = (0.1, 0.1, 0.1, 1)
        _BorderWidth ("Border Width", Range(0, 0.2)) = 0.05
        _CornerRadius ("Corner Radius", Range(0, 0.5)) = 0.15
        _GradientIntensity ("Gradient Intensity", Range(0, 1)) = 0.3
        _GradientCenter ("Gradient Center", Range(0, 1)) = 0.5
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 0.2)
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.2)
        _ShadowOffset ("Shadow Offset", Vector) = (0.01, -0.01, 0, 0)
        _BevelAmount ("Bevel Amount", Range(0, 0.1)) = 0.02
        _EdgeLight ("Edge Light Intensity", Range(0, 1)) = 0.1
        _Smoothness ("Edge Smoothness", Range(0, 0.1)) = 0.01
    }
    
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
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
            // float4 _Color;
            float4 _BorderColor;
            float _BorderWidth;
            float _CornerRadius;
            float _GradientIntensity;
            float _GradientCenter;
            float4 _HighlightColor;
            float4 _ShadowColor;
            float4 _ShadowOffset;
            float _BevelAmount;
            float _EdgeLight;
            float _Smoothness;
            
            float roundedBoxSDF(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                o.color = v.color;

                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float2 centeredUV = i.uv - 0.5;
                
                float sdf = roundedBoxSDF(centeredUV, float2(0.5, 0.5), _CornerRadius);
                
                float edgeSoftness = _Smoothness;
                
                float alpha = 1.0 - smoothstep(-edgeSoftness, edgeSoftness, sdf);
                
                float innerSDF = roundedBoxSDF(centeredUV, float2(0.5 - _BorderWidth, 0.5 - _BorderWidth), 
                                             max(0, _CornerRadius - _BorderWidth));
                
                float innerMask = 1.0 - smoothstep(-edgeSoftness, edgeSoftness, innerSDF);
                
                float borderMask = alpha - innerMask;
                
                float distanceFromCenter = length(centeredUV * 2.0);
                
                distanceFromCenter = saturate(distanceFromCenter / 0.707);
                
                float gradientFactor = pow(distanceFromCenter, 2.0);
                float adjustedGradient = lerp(distanceFromCenter, gradientFactor, _GradientIntensity);
                
                float centerHighlight = 1.0 - smoothstep(0, _GradientCenter, adjustedGradient);
                
                float4 mainTex = tex2D(_MainTex, i.uv);
                // float4 baseColor = mainTex * _Color;
                float4 baseColor = mainTex * i.color;
                
                float2 dir = normalize(centeredUV);
                float light = dot(dir, normalize(float2(0.5, -0.5))) * 0.5 + 0.5;
                light = pow(light, 2.0);
                
                float edgeDistance = 1.0 - smoothstep(0, 0.05, sdf + edgeSoftness);
                float edgeGlow = pow(edgeDistance, 3.0) * _EdgeLight;
                
                float4 innerColor = baseColor;
                
                float gradientValue = 1.0 - adjustedGradient * 0.4 + centerHighlight * 0.3;
                innerColor.rgb *= gradientValue;
                
                innerColor.rgb += light * _BevelAmount;
                
                innerColor.rgb += _HighlightColor.rgb * centerHighlight * _HighlightColor.a;
                
                innerColor.rgb += edgeGlow;
                
                float4 finalColor = float4(0, 0, 0, 0);
                
                finalColor = lerp(finalColor, _BorderColor, borderMask);
                
                finalColor = lerp(finalColor, innerColor, innerMask);
                
                finalColor.a *= alpha;
                
                if (alpha > 0)
                {
                    float2 shadowUV = centeredUV + _ShadowOffset.xy;
                    float shadowSDF = roundedBoxSDF(shadowUV, float2(0.5, 0.5), _CornerRadius);
                    float shadowAlpha = 1.0 - smoothstep(-edgeSoftness, edgeSoftness, shadowSDF);
                    
                    if (shadowAlpha > 0 && innerMask > 0)
                    {
                        float shadowStrength = (1.0 - innerMask) * _ShadowColor.a * 0.5;
                        finalColor.rgb = lerp(finalColor.rgb, _ShadowColor.rgb, shadowStrength);
                    }
                }
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Sprites/Default"
}
