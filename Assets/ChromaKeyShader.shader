Shader "Custom/ChromaKey"
{
    Properties {
        _MainTex ("Video Texture", 2D) = "white" {}
        _ChromaColor ("Chroma Key Color", Color) = (0,1,0,1) // —Î
        _Threshold ("Tolerance", Range(0,1)) = 0.3
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _ChromaColor;
            float _Threshold;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };

            v2f vert(appdata v){ v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target {
                float4 col = tex2D(_MainTex, i.uv);
                float diff = distance(col.rgb, _ChromaColor.rgb);
                if(diff < _Threshold) discard; // —Î‚ð“§–¾‚É
                return col;
            }
            ENDCG
        }
    }
}
