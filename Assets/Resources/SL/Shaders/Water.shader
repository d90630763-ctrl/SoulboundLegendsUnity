Shader "SL/Water"
{
    Properties
    {
        _Color ("Color", Color) = (0.1,0.45,0.65,0.72)
        _Glossiness ("Smoothness", Range(0,1)) = 0.9
        _Wave ("Wave Height", Float) = 0.04
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert
        #pragma target 3.0
        struct Input { float3 worldPos; };
        fixed4 _Color;
        half _Glossiness;
        half _Wave;
        void vert (inout appdata_full v)
        {
            float t = _Time.y;
            v.vertex.y += sin(v.vertex.x * 1.7 + t * 1.3) * _Wave + cos(v.vertex.z * 1.3 + t) * _Wave;
        }
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float s = 0.5 + 0.5 * sin(IN.worldPos.x * 0.9 + _Time.y * 1.1) * cos(IN.worldPos.z * 0.8 - _Time.y);
            o.Albedo = _Color.rgb * (0.85 + 0.3 * s);
            o.Metallic = 0;
            o.Smoothness = _Glossiness;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
