// Tlahuelli's skins: the same sprite with the colour of his clothes changed.
// Only pixels whose hue is inside [_HueMin, _HueMax] and that are colourful and bright enough (his blue clothes)
// are recoloured; skin, hair, outline and gold details stay as they are. Works on SpriteRenderers (flip, colour)
// and on UI Images (the shop preview).
Shader "TD_Game/SpriteSkin"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HueMin ("Source hue min", Range(0,1)) = 0.55
        _HueMax ("Source hue max", Range(0,1)) = 0.70
        _MinSat ("Source min saturation", Range(0,1)) = 0.35
        _MinVal ("Source min value", Range(0,1)) = 0.22
        _HueShift ("Hue shift", Range(-1,1)) = 0
        _SatMul ("Saturation x", Range(0,2)) = 1
        _ValMul ("Brightness x", Range(0,2)) = 1
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SkinFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _HueMin, _HueMax, _MinSat, _MinVal, _HueShift, _SatMul, _ValMul;

            float3 RgbToHsv(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HsvToRgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            fixed4 SkinFrag(v2f IN) : SV_Target
            {
                fixed4 tex = SampleSpriteTexture(IN.texcoord);
                float3 hsv = RgbToHsv(tex.rgb);
                if (hsv.x >= _HueMin && hsv.x <= _HueMax && hsv.y >= _MinSat && hsv.z >= _MinVal)
                {
                    hsv.x = frac(hsv.x + _HueShift + 1.0);
                    hsv.y = saturate(hsv.y * _SatMul);
                    hsv.z = saturate(hsv.z * _ValMul);
                    tex.rgb = HsvToRgb(hsv);
                }
                fixed4 c = tex * IN.color;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
