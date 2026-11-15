Shader "MarteSim/SkyboxMarte2D"
{
    Properties
    {
        _ColorCenit ("Color Cenit", Color) = (0.15, 0.20, 0.35, 1)
        _ColorHorizonte ("Color Horizonte", Color) = (0.85, 0.45, 0.25, 1)
        _BrilloAtmosferico ("Brillo Atmosferico", Range(0.1, 2.0)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Background-100" "RenderType"="Opaque" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ColorCenit;
            fixed4 _ColorHorizonte;
            float _BrilloAtmosferico;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = lerp(_ColorHorizonte, _ColorCenit, pow(saturate(i.uv.y), 0.75));
                return col * _BrilloAtmosferico;
            }
            ENDCG
        }
    }
}