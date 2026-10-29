// The game's one surface shader: per-pixel Lambert under a hemisphere light and a sun that casts real-time shadows,
// with optional gloss and rim light, alpha cut-out, and linear fog. Colours and fog come from globals set in LRGame.
// _Lit 0 makes it unlit; the blend properties cover the few transparent materials.
Shader "LR/Lit" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Emission ("Emission", Color) = (0,0,0,0)
        _Lit ("Lit", Float) = 1
        _Fog ("Fog", Float) = 1
        _Gloss ("Gloss", Float) = 0
        _Rim ("Rim", Float) = 0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1
        _SrcBlend ("Src Blend", Float) = 1
        _DstBlend ("Dst Blend", Float) = 0
        _ZWrite ("ZWrite", Float) = 1
        _Cull ("Cull", Float) = 2
        _Cutoff ("Alpha Cutoff", Float) = 0
    }
    SubShader {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass {
            Tags { "LightMode" = "ForwardBase" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile __ LR_BUMP
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color; fixed4 _Emission; float _Lit; float _Fog; float _Cutoff; float _Gloss; float _Rim;
            float4 _LRSky; float4 _LRGround; float4 _LRSun; float4 _LRSunDir; float4 _LRFogColor; float4 _LRFogParams;
            sampler2D _BumpMap; float _BumpScale;
            struct v2f {
                float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; float fog : TEXCOORD2; float3 world : TEXCOORD3;
                SHADOW_COORDS(4)
            #ifdef LR_BUMP
                float3 tangent : TEXCOORD5; float3 bitangent : TEXCOORD6;
            #endif
            };
            v2f vert (appdata_tan v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                float d = -UnityObjectToViewPos(v.vertex).z;
                o.fog = lerp(1, saturate((_LRFogParams.y - d) / (_LRFogParams.y - _LRFogParams.x)), _Fog);
            #ifdef LR_BUMP
                o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
                o.bitangent = cross(o.normal, o.tangent) * v.tangent.w;
            #endif
                TRANSFER_SHADOW(o);
                return o;
            }
            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                clip(c.a - _Cutoff);
                float3 n = normalize(i.normal) * (facing > 0 ? 1 : -1);
            #ifdef LR_BUMP
                // the normal maps are plain RGB images in the DirectX convention (green points down)
                float3 tn = tex2D(_BumpMap, i.uv).xyz * 2 - 1;
                n = normalize(i.tangent * tn.x * _BumpScale - i.bitangent * tn.y * _BumpScale + n * tn.z);
            #endif
                float3 view = normalize(_WorldSpaceCameraPos - i.world);
                fixed shadow = SHADOW_ATTENUATION(i);
                float ndl = max(0, dot(n, _LRSunDir.xyz));
                float3 light = lerp(_LRGround.rgb, _LRSky.rgb, n.y * 0.5 + 0.5) + _LRSun.rgb * ndl * shadow;
                light += _LRSky.rgb * pow(1 - saturate(dot(n, view)), 3) * _Rim;
                float3 spec = _LRSun.rgb * pow(max(0, dot(n, normalize(_LRSunDir.xyz + view))), 40) * _Gloss * shadow;
                c.rgb = c.rgb * lerp(float3(1, 1, 1), light, _Lit) + spec * _Lit + _Emission.rgb;
                c.rgb = lerp(_LRFogColor.rgb, c.rgb, i.fog);
                return c;
            }
            ENDCG
        }
        Pass {
            Tags { "LightMode" = "ShadowCaster" }
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_ST; fixed4 _Color; float _Cutoff;
            struct v2f { V2F_SHADOW_CASTER; float2 uv : TEXCOORD1; };
            v2f vert (appdata_base v) {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target {
                clip(tex2D(_MainTex, i.uv).a * _Color.a - max(_Cutoff, 0.05));
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
