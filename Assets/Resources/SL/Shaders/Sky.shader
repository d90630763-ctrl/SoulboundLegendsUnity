Shader "SL/Sky"
{
    Properties
    {
        _Top ("Top", Color) = (0.15,0.35,0.7,1)
        _Horizon ("Horizon", Color) = (0.75,0.85,0.95,1)
        _Bottom ("Bottom", Color) = (0.25,0.3,0.35,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "IgnoreProjector"="True" }
        Cull Front
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 pos : SV_POSITION; float h : TEXCOORD0; };
            fixed4 _Top; fixed4 _Horizon; fixed4 _Bottom;
            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.h = normalize(v.vertex.xyz).y;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float h = i.h;
                fixed4 c = (h > 0) ? lerp(_Horizon, _Top, saturate(pow(h, 0.55))) : lerp(_Horizon, _Bottom, saturate(-h * 2.5));
                return c;
            }
            ENDCG
        }
    }
}
