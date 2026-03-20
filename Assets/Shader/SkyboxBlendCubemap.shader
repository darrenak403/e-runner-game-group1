// Blend shader cho Skybox/Cubemap
// C# copy _Tex từ cả 2 material rồi animate _Blend 0->1 để crossfade A->B

Shader "Custom/SkyboxBlendCubemap"
{
    Properties
    {
        _TexA      ("Cubemap A", Cube) = "grey" {}
        _TexB      ("Cubemap B", Cube) = "grey" {}

        _Blend     ("Blend (0=A, 1=B)", Range(0, 1)) = 0

        _TintA     ("Tint A", Color) = (.5, .5, .5, .5)
        _TintB     ("Tint B", Color) = (.5, .5, .5, .5)

        _ExposureA ("Exposure A", Float) = 1
        _ExposureB ("Exposure B", Float) = 1

        _RotationA ("Rotation A", Range(0, 360)) = 0
        _RotationB ("Rotation B", Range(0, 360)) = 0
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

            samplerCUBE _TexA, _TexB;
            half   _Blend;
            half4  _TintA, _TintB;
            half   _ExposureA, _ExposureB;
            float  _RotationA, _RotationB;

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
                o.dir = v.vertex.xyz; // direction thô, rotate trong frag
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dirA = normalize(RotateY(i.dir, _RotationA));
                float3 dirB = normalize(RotateY(i.dir, _RotationB));

                half4 colA = texCUBE(_TexA, dirA);
                half4 colB = texCUBE(_TexB, dirB);

                colA.rgb *= _TintA.rgb * unity_ColorSpaceDouble.rgb * _ExposureA;
                colB.rgb *= _TintB.rgb * unity_ColorSpaceDouble.rgb * _ExposureB;

                return lerp(colA, colB, _Blend);
            }
            ENDCG
        }
    }
}
