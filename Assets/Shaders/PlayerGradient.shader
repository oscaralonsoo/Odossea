Shader "Custom/VerticalGradient_PlayerRelative"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (1,1,1,1)
        _BottomColor ("Bottom Color", Color) = (0,0,0,1)

        _MinY ("Bottom Height", Float) = -0.5
        _MaxY ("Top Height", Float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float relativeY : TEXCOORD0;
            };

            fixed4 _TopColor;
            fixed4 _BottomColor;
            float _MinY;
            float _MaxY;

            v2f vert (appdata v)
            {
                v2f o;

                // Posición en mundo del vértice
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // Posición en mundo del pivot del objeto
                float objectWorldY = unity_ObjectToWorld._m13;

                // Altura RELATIVA al player (ignora rotación)
                o.relativeY = worldPos.y - objectWorldY;

                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = saturate((i.relativeY - _MinY) / (_MaxY - _MinY));
                return lerp(_BottomColor, _TopColor, t);
            }
            ENDCG
        }
    }
}
