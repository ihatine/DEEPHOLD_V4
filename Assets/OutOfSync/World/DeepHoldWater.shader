Shader "DEEPHOLD/Water"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.03,0.22,0.42,0.88)
        _FoamColor ("Foam", Color) = (0.35,0.75,0.9,0.65)
        _WaveScale ("Wave Scale", Float) = 2.2
        _WaveSpeed ("Wave Speed", Float) = 0.65
        _Opacity ("Opacity", Range(0,1)) = 0.9
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        CGPROGRAM
        #pragma surface surf Standard alpha:fade fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;
        fixed4 _FoamColor;
        float _WaveScale;
        float _WaveSpeed;
        float _Opacity;
        struct Input { float3 worldPos; float3 viewDir; };

        float wave(float2 p)
        {
            float t = _Time.y * _WaveSpeed;
            return sin(p.x * _WaveScale + t) * 0.5 + sin(p.y * _WaveScale * 1.37 - t * 0.8) * 0.35 + sin((p.x+p.y) * _WaveScale * 0.7 + t*0.4) * 0.15;
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float w = wave(IN.worldPos.xy);
            float highlight = smoothstep(0.35, 0.8, w * 0.5 + 0.5);
            float2 cell = abs(frac(IN.worldPos.xy) - 0.5) * 2.0;
            float edge = max(cell.x, cell.y);
            float foam = smoothstep(0.82, 1.0, edge) * 0.25;
            o.Albedo = lerp(_Color.rgb, _FoamColor.rgb, highlight * 0.22 + foam);
            o.Emission = _FoamColor.rgb * (highlight * 0.08);
            o.Smoothness = 0.92;
            o.Metallic = 0.05;
            o.Alpha = _Opacity;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
