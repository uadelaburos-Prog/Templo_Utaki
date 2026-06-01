Shader "Custom/SpotlightOverlay"
{
    Properties
    {
        _PlayerViewport ("Player Viewport (0-1)", Vector) = (0.5, 0.5, 0, 0)
        _Radius         ("Spotlight Radius (fracción de altura)", Float) = 0.18
        _Softness       ("Borde suave (fracción de altura)",     Float) = 0.10
        _MaxAlpha       ("Oscuridad máxima",                     Float) = 0.0
        _MainTex        ("Texture",                              2D)    = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "RenderType"        = "Transparent"
            "IgnoreProjector"   = "True"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4    _PlayerViewport;
            float     _Radius;
            float     _Softness;
            float     _MaxAlpha;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv     = i.texcoord;
                float2 center = _PlayerViewport.xy;

                // _ScreenParams.xy = (renderWidth, renderHeight) — siempre exacto para este pass
                float  aspect = _ScreenParams.x / _ScreenParams.y;
                float2 diff   = float2((uv.x - center.x) * aspect, uv.y - center.y);
                float  dist   = length(diff);

                // smoothstep: 0 en el centro (visible), 1 en el borde (negro)
                float mask = smoothstep(_Radius, _Radius + _Softness, dist);

                return fixed4(0.0, 0.0, 0.0, mask * _MaxAlpha);
            }
            ENDCG
        }
    }
}
