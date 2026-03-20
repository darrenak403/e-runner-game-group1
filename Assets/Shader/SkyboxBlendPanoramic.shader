Shader "Custom/SkyboxBlendPanoramic"
{
    Properties
    {
        _Tint     ("Tint Color", Color) = (1,1,1,1)
        [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0

        [NoScaleOffset] _TexA ("Panoramic A", 2D) = "grey" {}
        [NoScaleOffset] _TexB ("Panoramic B", 2D) = "grey" {}

        _Blend ("Blend (0=A, 1=B)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _TexA, _TexB;
            half4  _Tint;
            half   _Exposure;
            float  _Rotation;
            float  _Blend;

            #define PI 3.14159265358979

            struct appdata { float4 vertex : POSITION; };
            struct v2f    { float4 pos : SV_POSITION; float3 worldDir : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.worldDir = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
                return o;
            }

            float2 DirToUV(float3 dir, float rotDeg)
            {
                float rad = rotDeg * (PI / 180.0);
                float s, c;
                sincos(rad, s, c);
                dir.xz = float2(c * dir.x - s * dir.z, s * dir.x + c * dir.z);

                float u = 0.5 + atan2(dir.z, dir.x) / (2.0 * PI);
                float v = 1.0 - acos(clamp(dir.y, -1.0, 1.0)) / PI;
                return float2(u, v);
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 uv  = DirToUV(normalize(i.worldDir), _Rotation);
                half4 colA = tex2D(_TexA, uv);
                half4 colB = tex2D(_TexB, uv);

                half4 col  = lerp(colA, colB, _Blend);
                col.rgb   *= _Tint.rgb * _Exposure;
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
