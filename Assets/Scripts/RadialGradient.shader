Shader "Custom/RadialGradient"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _FadeEdge ("Edge Fade", Range(0,1)) = 0.5
        _FadePower ("Fade Power", Range(0.1,5)) = 2.0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _FadeEdge;
            float _FadePower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate distance from center (0.5, 0.5)
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center) * 2.0; // *2 to normalize to 0-1

                // Create radial fade: 0 at center, 1 at edges
                float radialFade = saturate(pow(dist, _FadePower));

                // Apply edge fade threshold
                float alpha = 1.0 - smoothstep(1.0 - _FadeEdge, 1.0, radialFade);

                // Sample texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color * i.color;

                // Apply radial fade to alpha
                col.a *= alpha;

                return col;
            }
            ENDCG
        }
    }
}
