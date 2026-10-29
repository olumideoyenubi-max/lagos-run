// Finishing pass on desktop: soft bloom from bright areas, a gentle contrast curve, a touch more colour and a vignette.
Shader "LR/Post" {
    Properties { _MainTex ("Source", 2D) = "white" {} _Bloom ("Bloom", 2D) = "black" {} }
    SubShader {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex; sampler2D _Bloom; float4 _MainTex_TexelSize; float4 _Params; float4 _Dir;
        ENDCG
        Pass {  // 0: keep the bright parts, shrinking as we go
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            fixed4 frag (v2f_img i) : SV_Target {
                float2 d = _MainTex_TexelSize.xy;
                fixed4 c = (tex2D(_MainTex, i.uv + d * float2(-1, -1)) + tex2D(_MainTex, i.uv + d * float2(1, -1)) + tex2D(_MainTex, i.uv + d * float2(-1, 1)) + tex2D(_MainTex, i.uv + d * float2(1, 1))) * 0.25;
                float b = max(c.r, max(c.g, c.b));
                c.rgb *= saturate((b - _Params.x) / (1 - _Params.x));
                return c;
            }
            ENDCG
        }
        Pass {  // 1: blur along _Dir
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            fixed4 frag (v2f_img i) : SV_Target {
                float2 d = _MainTex_TexelSize.xy * _Dir.xy;
                fixed4 c = tex2D(_MainTex, i.uv) * 0.227;
                c += (tex2D(_MainTex, i.uv + d * 1.385) + tex2D(_MainTex, i.uv - d * 1.385)) * 0.316;
                c += (tex2D(_MainTex, i.uv + d * 3.231) + tex2D(_MainTex, i.uv - d * 3.231)) * 0.070;
                return c;
            }
            ENDCG
        }
        Pass {  // 2: combine
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            fixed4 frag (v2f_img i) : SV_Target {
                fixed4 c = tex2D(_MainTex, i.uv);
                float2 buv = i.uv;
            #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0) buv.y = 1 - buv.y;
            #endif
                c.rgb += tex2D(_Bloom, buv).rgb * _Params.y;
                c.rgb = lerp(c.rgb, c.rgb * c.rgb * (3 - 2 * c.rgb), 0.16);
                float l = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(float3(l, l, l), c.rgb, _Params.w);
                c.rgb *= 1 - _Params.z * smoothstep(0.42, 0.85, distance(i.uv, float2(0.5, 0.5)));
                return c;
            }
            ENDCG
        }
    }
}
