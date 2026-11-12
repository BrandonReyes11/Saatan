// Saatan Historia - Shader para los modelos voxel de Assets/Art (árboles, arbustos, hierba, piedras).
// Los FBX no traen texturas: el color de cada voxel viene en los COLORES DE VÉRTICE.
// El shader Lit de URP ignora esos colores (todo se vería blanco), por eso usamos este.
// Iluminación simple (Lambert) con luz principal, luces adicionales (Forward+) y niebla.
Shader "Saatan/VegetacionVoxel"
{
    Properties
    {
        _BaseColor ("Tinte", Color) = (1, 1, 1, 1)
        _Brillo ("Brillo", Range(0, 3)) = 1
        [Toggle] _ColorSRGB ("Colores de vértice en sRGB", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Brillo;
            half _ColorSRGB;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS);

                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);

                half3 c = v.color.rgb;
                if (_ColorSRGB > 0.5h) c = SRGBToLinear(c);
                o.color = half4(c * _BaseColor.rgb * _Brillo, 1.0h);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                InputData d = (InputData)0;
                d.positionWS = i.positionWS;
                d.normalWS = NormalizeNormalPerPixel(i.normalWS);
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                d.fogCoord = i.fogFactor;
                d.vertexLighting = half3(0, 0, 0);
                d.bakedGI = SampleSH(d.normalWS);
                d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                d.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = i.color.rgb;
                s.alpha = 1.0h;
                s.occlusion = 1.0h;
                s.specular = half3(0, 0, 0);
                s.smoothness = 0.0h;
                s.normalTS = half3(0, 0, 1);

                half4 c = UniversalFragmentBlinnPhong(d, s);
                c.rgb = MixFog(c.rgb, d.fogCoord);
                c.a = 1.0h;
                return c;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VertProf
            #pragma fragment FragProf
            #pragma multi_compile_instancing

            struct AttrProf
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VarProf
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VarProf VertProf(AttrProf v)
            {
                VarProf o = (VarProf)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 FragProf(VarProf i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Particles/Simple Lit"
}
