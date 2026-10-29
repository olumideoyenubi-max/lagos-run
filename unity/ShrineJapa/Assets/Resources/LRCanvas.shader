// Used only by LRCanvas to paint textures at startup: vertex colour times the texture's alpha (font atlas or white).
Shader "LR/Canvas" {
    Properties { _MainTex ("Texture", 2D) = "white" {} _UseRGB ("Use RGB", Float) = 0 }
    SubShader {
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off ZTest Always Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float _UseRGB;
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            v2f vert (appdata_full v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.color = v.color; o.uv = v.texcoord.xy; return o; }
            fixed4 frag (v2f i) : SV_Target { fixed4 t = tex2D(_MainTex, i.uv); fixed4 c = i.color; c.rgb *= lerp(fixed3(1, 1, 1), t.rgb, _UseRGB); c.a *= lerp(t.a, 1, _UseRGB); return c; }
            ENDCG
        }
    }
}
