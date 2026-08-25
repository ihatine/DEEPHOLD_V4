Shader "DEEPHOLD/LightRay"
{
    Properties { _Color ("Color", Color) = (1,0.82,0.48,0.12) _Softness("Softness", Range(0.2,4))=1.5 }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0
        fixed4 _Color; float _Softness;
        struct Input { float2 uv; };
        void surf(Input IN, inout SurfaceOutput o)
        {
            float edge = pow(saturate(1.0 - abs(IN.uv.x*2.0-1.0)), _Softness);
            float fade = smoothstep(0.0, 0.18, IN.uv.y) * smoothstep(1.0, 0.72, IN.uv.y);
            o.Albedo = _Color.rgb;
            o.Emission = _Color.rgb * 0.35;
            o.Smoothness = 0.1;
            o.Alpha = _Color.a * edge * fade;
        }
        ENDCG
    }
}
