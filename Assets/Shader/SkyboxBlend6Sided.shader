// Blend shader cho AllSkyFree 6 Sided skybox
// C# copy _FrontTex/_BackTex/_LeftTex/_RightTex/_UpTex/_DownTex từ cả 2 material vào đây
// rồi animate _Blend từ 0->1 để crossfade A->B

Shader "Custom/SkyboxBlend6Sided"
{
    Properties
    {
        // Skybox A (from)
        _FrontTexA ("Front A (+Z)", 2D) = "grey" {}
        _BackTexA  ("Back A (-Z)",  2D) = "grey" {}
        _LeftTexA  ("Left A (+X)",  2D) = "grey" {}
        _RightTexA ("Right A (-X)", 2D) = "grey" {}
        _UpTexA    ("Up A (+Y)",    2D) = "grey" {}
        _DownTexA  ("Down A (-Y)",  2D) = "grey" {}

        // Skybox B (to)
        _FrontTexB ("Front B (+Z)", 2D) = "grey" {}
        _BackTexB  ("Back B (-Z)",  2D) = "grey" {}
        _LeftTexB  ("Left B (+X)",  2D) = "grey" {}
        _RightTexB ("Right B (-X)", 2D) = "grey" {}
        _UpTexB    ("Up B (+Y)",    2D) = "grey" {}
        _DownTexB  ("Down B (-Y)",  2D) = "grey" {}

        _Blend     ("Blend (0=A, 1=B)", Range(0, 1)) = 0
        _Tint      ("Tint", Color) = (.5, .5, .5, .5)
        _Exposure  ("Exposure", Float) = 1
        _Rotation  ("Rotation", Range(0, 360)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _FrontTexA, _BackTexA, _LeftTexA, _RightTexA, _UpTexA, _DownTexA;
            sampler2D _FrontTexB, _BackTexB, _LeftTexB, _RightTexB, _UpTexB, _DownTexB;
            half4  _Tint;
            half   _Exposure;
            half   _Blend;
            float  _Rotation;

            struct appdata { float4 vertex : POSITION; };
            struct v2f     { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            float3 RotateY(float3 v, float deg)
            {
                float rad = deg * UNITY_PI / 180.0;
                float s, c;
                sincos(rad, s, c);
                return float3(c * v.x + s * v.z, v.y, -s * v.x + c * v.z);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = RotateY(v.vertex.xyz, _Rotation);
                return o;
            }

            half4 BlendFace(sampler2D tA, sampler2D tB, float2 uv)
            {
                return lerp(tex2D(tA, uv), tex2D(tB, uv), _Blend);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float3 a = abs(d);
                float2 uv;
                half4 col;

                if (a.x >= a.y && a.x >= a.z)
                {
                    if (d.x > 0) // Left  (+X)
                    {
                        uv  = float2(-d.z, d.y) / a.x * 0.5 + 0.5;
                        col = BlendFace(_LeftTexA, _LeftTexB, uv);
                    }
                    else         // Right (-X)
                    {
                        uv  = float2( d.z, d.y) / a.x * 0.5 + 0.5;
                        col = BlendFace(_RightTexA, _RightTexB, uv);
                    }
                }
                else if (a.y >= a.x && a.y >= a.z)
                {
                    if (d.y > 0) // Up    (+Y)
                    {
                        uv  = float2( d.x, -d.z) / a.y * 0.5 + 0.5;
                        col = BlendFace(_UpTexA, _UpTexB, uv);
                    }
                    else         // Down  (-Y)
                    {
                        uv  = float2( d.x,  d.z) / a.y * 0.5 + 0.5;
                        col = BlendFace(_DownTexA, _DownTexB, uv);
                    }
                }
                else
                {
                    if (d.z > 0) // Front (+Z)
                    {
                        uv  = float2( d.x, d.y) / a.z * 0.5 + 0.5;
                        col = BlendFace(_FrontTexA, _FrontTexB, uv);
                    }
                    else         // Back  (-Z)
                    {
                        uv  = float2(-d.x, d.y) / a.z * 0.5 + 0.5;
                        col = BlendFace(_BackTexA, _BackTexB, uv);
                    }
                }

                col.rgb *= _Tint.rgb * unity_ColorSpaceDouble.rgb * _Exposure;
                return col;
            }
            ENDCG
        }
    }
}
