Shader "Custom/SlatBend"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Bend ("Bend Amount", Float) = 0
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _Bend;

        struct Input
        {
            float2 uv_MainTex;
        };

        void vert(inout appdata_full v)
        {
            float bend = _Bend;

            // bend based on vertex height (Y)
            float wave = sin(v.vertex.y * 3.1415); // smooth wave
            v.vertex.x += wave * bend;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;

        }
        ENDCG
    }
    FallBack "Diffuse"
}
