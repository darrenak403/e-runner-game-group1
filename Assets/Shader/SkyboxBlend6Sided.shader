Shader "Custom/SkyboxBlend6Sided"
{
    Properties
    {
        _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
        [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0

        // --- Skybox A (6 textures) ---
        [NoScaleOffset] _FrontTexA ("Front [+Z]  (A)", 2D) = "grey" {}
        [NoScaleOffset] _BackTexA  ("Back  [-Z]  (A)", 2D) = "grey" {}
        [NoScaleOffset] _LeftTexA  ("Left  [+X]  (A)", 2D) = "grey" {}
        [NoScaleOffset] _RightTexA ("Right [-X]  (A)", 2D) = "grey" {}
        [NoScaleOffset] _UpTexA    ("Up    [+Y]  (A)", 2D) = "grey" {}
        [NoScaleOffset] _DownTexA  ("Down  [-Y]  (A)", 2D) = "grey" {}

        // --- Skybox B (6 textures) ---
        [NoScaleOffset] _FrontTexB ("Front [+Z]  (B)", 2D) = "grey" {}
        [NoScaleOffset] _BackTexB  ("Back  [-Z]  (B)", 2D) = "grey" {}
        [NoScaleOffset] _LeftTexB  ("Left  [+X]  (B)", 2D) = "grey" {}
        [NoScaleOffset] _RightTexB ("Right [-X]  (B)", 2D) = "grey" {}
        [NoScaleOffset] _UpTexB    ("Up    [+Y]  (B)", 2D) = "grey" {}
        [NoScaleOffset] _DownTexB  ("Down  [-Y]  (B)", 2D) = "grey" {}

        // 0 = Skybox A, 1 = Skybox B
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

            sampler2D _FrontTexA, _BackTexA, _LeftTexA, _RightTexA, _UpTexA, _DownTexA;
            sampler2D _FrontTexB, _BackTexB, _LeftTexB, _RightTexB, _UpTexB, _DownTexB;

            half4 _Tint;
            half  _Exposure;
            float _Rotation;
            float _Blend;

            struct appdata { float4 vertex : POSITION; };
            struct v2f    { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            // Rotate direction around Y axis
            float3 RotateY(float3 d, float deg)
            {
                float rad = deg * (3.14159265 / 180.0);
                float s, c;
                sincos(rad, s, c);
                return float3(c * d.x + s * d.z, d.y, -s * d.x + c * d.z);
            }

            // Sample 6-sided sky from a world direction
            half4 Sample6(sampler2D front, sampler2D back,
                          sampler2D left,  sampler2D right,
                          sampler2D up,    sampler2D down,
                          float3 d)
            {
                float3 a = abs(d);
                float2 uv;

                // Pick dominant axis
                if (a.z >= a.x && a.z >= a.y)      // Front / Back
                {
                    if (d.z > 0) { uv = float2(-d.x,  d.y) / a.z; return tex2D(front, uv * 0.5 + 0.5); }
                    else         { uv = float2( d.x,  d.y) / a.z; return tex2D(back,  uv * 0.5 + 0.5); }
                }
                else if (a.x >= a.y)               // Left / Right
                {
                    if (d.x > 0) { uv = float2( d.z,  d.y) / a.x; return tex2D(left,  uv * 0.5 + 0.5); }
                    else         { uv = float2(-d.z,  d.y) / a.x; return tex2D(right, uv * 0.5 + 0.5); }
                }
                else                               // Up / Down
                {
                    if (d.y > 0) { uv = float2( d.x, -d.z) / a.y; return tex2D(up,   uv * 0.5 + 0.5); }
                    else         { uv = float2( d.x,  d.z) / a.y; return tex2D(down, uv * 0.5 + 0.5); }
                }
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(RotateY(i.dir, _Rotation));

                half4 colA = Sample6(_FrontTexA, _BackTexA, _LeftTexA, _RightTexA, _UpTexA, _DownTexA, dir);
                half4 colB = Sample6(_FrontTexB, _BackTexB, _LeftTexB, _RightTexB, _UpTexB, _DownTexB, dir);

                half4 col  = lerp(colA, colB, _Blend);
                col.rgb   *= _Tint.rgb * unity_ColorSpaceDouble.rgb * _Exposure;
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
