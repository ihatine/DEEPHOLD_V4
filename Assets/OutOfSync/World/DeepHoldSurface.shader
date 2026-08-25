Shader "DEEPHOLD/Surface"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.35,0.3,0.25,1)
        _NoiseScale ("Texture Scale", Float) = 3.0
        _NoiseStrength ("Texture Strength", Range(0,1)) = 0.18
        _EdgeDarken ("Edge Darken", Range(0,1)) = 0.25
        _Smoothness ("Smoothness", Range(0,1)) = 0.18
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 250
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _BaseColor;
        float _NoiseScale;
        float _NoiseStrength;
        float _EdgeDarken;
        float _Smoothness;

        struct Input { float3 worldPos; float3 viewDir; };

        float hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float noise2(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            f = f*f*(3.0-2.0*f);
            float a=hash21(i), b=hash21(i+float2(1,0));
            float c=hash21(i+float2(0,1)), d=hash21(i+float2(1,1));
            return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float n = noise2(IN.worldPos.xy * _NoiseScale) * 0.7 + noise2(IN.worldPos.xy * _NoiseScale * 2.7) * 0.3;
            float3 c = _BaseColor.rgb * lerp(1.0 - _NoiseStrength, 1.0 + _NoiseStrength, n);
            float edge = saturate(1.0 - abs(IN.worldPos.z) * 1.2);
            c *= lerp(1.0 - _EdgeDarken, 1.0, edge);
            o.Albedo = c;
            o.Smoothness = _Smoothness;
            o.Metallic = 0.02;
        }
        ENDCG
    }
    FallBack "Standard"
}
