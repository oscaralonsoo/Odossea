Shader "Custom/HeightGradientSmooth"
{
    Properties
    {
        _ColorLow ("Color Bajo", Color) = (0,1,0,1)
        _ColorHigh ("Color Alto", Color) = (0.5,0.25,0,1)
        _MinHeight ("Altura Min", Float) = 0
        _MaxHeight ("Altura Max", Float) = 10
        _Smoothness ("Suavidad", Range(0.1, 3)) = 1
        _MainTex ("Sprite", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float worldY : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _ColorLow;
            float4 _ColorHigh;
            float _MinHeight;
            float _MaxHeight;
            float _Smoothness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldY = worldPos.y;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = smoothstep(_MinHeight, _MaxHeight, i.worldY);
                t = pow(t, _Smoothness);

                fixed4 gradientColor = lerp(_ColorLow, _ColorHigh, t);
                fixed4 sprite = tex2D(_MainTex, i.uv);

                return sprite * gradientColor;
            }
            ENDCG
        }
    }
}
