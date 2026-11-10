Shader "Anexas/URP/Glass"
{
    Properties
    {
        _IOR("IOR", Range(1, 5)) = 1.5
        _ChromaticAberration("ChromaticAberration", Range(0, 1)) = 0
        _TintColor("TintColor", Color) = (0, 1, 0.8, 0)
        _TintTexture("TintTexture", 2D) = "white" {}
        _TintTextureDistortion("TintTextureDistortion", Range(0, 1)) = 0
        [Normal][NoScaleOffset]_NormalMap("NormalMap", 2D) = "bump" {}
        _NormalStrength("NormalStrength", Range(0.01, 10)) = 0.1
        _DistortionStrength("DistortionStrength", Range(0.01, 10)) = 1
        _DistortionTiling("DistortionTiling", Range(0.01, 1000)) = 400
        _Metallic("Metallic", Range(0, 1)) = 0.1
        _Smoothness("Smoothness", Range(0, 1)) = 1
        _ReflectionStrength("ReflectionStrength", Range(0, 5)) = 0.1
        [ToggleUI]_Emission("Emission", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "UniversalMaterialType" = "Lit"
            "Queue"="Transparent"
        }

        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM

            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma vertex vert
            #pragma fragment frag

            #define _NORMALMAP 1
            #define _NORMAL_DROPOFF_TS 1
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_SHADOW_COORD
            #define REQUIRE_OPAQUE_TEXTURE

            #define SHADERPASS_SHADOWCASTER (3)
            #define SHADERPASS_META (4)

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.deprecated.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _IOR;
                half _ChromaticAberration;
                half _Emission;
                half _DistortionStrength;
                half _NormalStrength;
                half4 _TintColor;
                half _DistortionTiling;
                half _Metallic;
                half _Smoothness;
                half _ReflectionStrength;
                half4 _TintTexture_TexelSize;
                half4 _TintTexture_ST;
                half _TintTextureDistortion;
                half4 _NormalMap_TexelSize;
            CBUFFER_END

            SAMPLER(SamplerState_Linear_Repeat);
            TEXTURE2D(_TintTexture);
            SAMPLER(sampler_TintTexture);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

#if (SHADERPASS == SHADERPASS_SHADOWCASTER)
            half3 _LightDirection;
            half3 _LightPosition;
#endif

            struct Attributes
            {
                half3 positionOS : POSITION;
                half3 normalOS : NORMAL;
                half4 tangentOS : TANGENT;
                half4 uv0 : TEXCOORD0;
                half4 uv1 : TEXCOORD1;
                half4 uv2 : TEXCOORD2;
            };

            struct Varyings
            {
                half4 positionCS : SV_POSITION;
                half3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                half4 texCoord0 : TEXCOORD3;
                half3 sh : TEXCOORD4;
                half4 fogFactorAndVertexLight : TEXCOORD5;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                half3 positionWS = TransformObjectToWorld(input.positionOS);
                half3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                half4 tangentWS = half4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

                output.positionWS = positionWS;
                output.normalWS = normalWS;
                output.tangentWS = tangentWS;

#if (SHADERPASS == SHADERPASS_SHADOWCASTER)
    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                half3 lightDirectionWS = normalize(_LightPosition - positionWS);
    #else
                half3 lightDirectionWS = _LightDirection;
    #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    #if UNITY_REVERSED_Z
                output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #else
                output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #endif
#elif (SHADERPASS == SHADERPASS_META)
                output.positionCS = UnityMetaVertexPosition(input.positionOS, input.uv1.xy, input.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
#else
                output.positionCS = TransformWorldToHClip(positionWS);
#endif

                output.texCoord0 = input.uv0;

#if (SHADERPASS == SHADERPASS_FORWARD) || (SHADERPASS == SHADERPASS_GBUFFER)
                OUTPUT_LIGHTMAP_UV(input.uv1, unity_LightmapST, output.staticLightmapUV);
                OUTPUT_SH(normalWS, output.sh);
#endif

                return output;
            }

            struct SurfaceDescriptionInputs
            {
                half3 WorldSpaceNormal;
                half3 TangentSpaceNormal;
                half3 WorldSpaceTangent;
                half3 WorldSpaceBiTangent;
                half3 WorldSpaceViewDirection;
                half3 WorldSpacePosition;
                half3 AbsoluteWorldSpacePosition;
                half2 NDCPosition;
                half2 PixelPosition;
                half4 uv0;
            };

            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
            {
                SurfaceDescriptionInputs output;
                ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

                half3 unnormalizedNormalWS = input.normalWS;
                const half renormFactor = 1.0 / length(unnormalizedNormalWS);

                half crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
                half3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

                output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;
                output.TangentSpaceNormal = half3(0.0f, 0.0f, 1.0f);

                output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
                output.WorldSpaceBiTangent = renormFactor * bitang;

                output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                output.WorldSpacePosition = input.positionWS;

                output.AbsoluteWorldSpacePosition = GetAbsolutePositionWS(input.positionWS);

#if UNITY_UV_STARTS_AT_TOP
                output.PixelPosition = half2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#else
                output.PixelPosition = half2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
#endif

                output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
                output.NDCPosition.y = 1.0f - output.NDCPosition.y;

                output.uv0 = input.texCoord0;

                return output;
            }

            struct SurfaceDescription
            {
                half3 BaseColor;
                half3 NormalTS;
                half3 Emission;
                half Metallic;
                half Smoothness;
                half Occlusion;
                half Alpha;
            };

            inline half ValueNoise(float2 uv)
            {
                half2 i = floor(uv);
                half2 f = frac(uv);

                f = f * f * (3.0 - 2.0 * f);

                half2 c0 = i + float2(0.0, 0.0);
                half2 c1 = i + float2(1.0, 0.0);
                half2 c2 = i + float2(0.0, 1.0);
                half2 c3 = i + float2(1.0, 1.0);

                half r0; Hash_LegacySine_2_1_float(c0, r0);
                half r1; Hash_LegacySine_2_1_float(c1, r1);
                half r2; Hash_LegacySine_2_1_float(c2, r2);
                half r3; Hash_LegacySine_2_1_float(c3, r3);

                half bottomOfGrid = lerp(r0, r1, f.x);
                half topOfGrid = lerp(r2, r3, f.x);
                half t = lerp(bottomOfGrid, topOfGrid, f.y);
                return t;
            }

            half SimpleNoise(half2 UV, float Scale)
            {
                half t = 0.0;

                half freq = pow(2.0, half(0));
                half amp = pow(0.5, half(3-0));
                t += ValueNoise(half2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

                freq = pow(2.0, half(1));
                amp = pow(0.5, half(3-1));
                t += ValueNoise(half2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

                freq = pow(2.0, half(2));
                amp = pow(0.5, half(3-2));
                t += ValueNoise(half2(UV.x*Scale/freq, UV.y*Scale/freq))*amp;

                return t;
            }

            half3 NormalFromHeight(half In, half Strength, float3 Position, half3x3 TangentMatrix)
            {
                half3 worldDerivativeX = ddx(Position);
                half3 worldDerivativeY = ddy(Position);

                half3 crossX = cross(TangentMatrix[2].xyz, worldDerivativeX);
                half3 crossY = cross(worldDerivativeY, TangentMatrix[2].xyz);

                half d = dot(worldDerivativeX, crossY);
                half sgn = d < 0.0 ? (-1.0f) : 1.0f;
                half surface = sgn / max(0.000000000000001192093f, abs(d));

                half dHdx = ddx(In);
                half dHdy = ddy(In);

                half3 surfGrad = surface * (dHdx*crossY + dHdy*crossX);

                half3 Out = SafeNormalize(TangentMatrix[2].xyz - (Strength * surfGrad));
                Out = TransformWorldToTangent(Out, TangentMatrix);

                return Out;
            }

            half2 RefractUV(half _Refractive_Index_Target, half _Refractive_Index_Origin, half3 _NormalMapVector, half3 WorldSpaceNormal, half3 AbsoluteWorldSpacePosition)
            {
                half3 normalBlend = SafeNormalize(half3(WorldSpaceNormal.rg + _NormalMapVector.rg, WorldSpaceNormal.b * _NormalMapVector.b));
                half ETA = _Refractive_Index_Origin / _Refractive_Index_Target;
                half3 refractedVector = refract(
                    normalize(-1 * mul((half3x3)UNITY_MATRIX_M, transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V)) [2].xyz)),
                    normalize(normalBlend),
                    ETA);

                half3 positionRefracted = AbsoluteWorldSpacePosition + normalize(refractedVector);
                half4 positionRefractedProjected = mul(UNITY_MATRIX_VP, half4(positionRefracted, 1.0));
                half4 screenPos = ComputeScreenPos(positionRefractedProjected);
                half2 sceneUV = screenPos.xy / screenPos.ww;
                return sceneUV;
            }

            SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
            {
                SurfaceDescription surface = (SurfaceDescription)0;
                UnityTexture2D unityTexture2D = UnityBuildTexture2DStruct(_TintTexture);

                half3x3 tbnMatrix = half3x3(IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, IN.WorldSpaceNormal);
                half tiledNoise = SimpleNoise(IN.uv0.xy, _DistortionTiling);

                half3 noiseDistortion = NormalFromHeight(tiledNoise, _DistortionStrength/5000.0, IN.WorldSpacePosition, tbnMatrix);

                half3 noiseTintTextureDistortion = noiseDistortion * _TintTextureDistortion.xxx;
                half2 distortedUV = IN.uv0.xy + noiseTintTextureDistortion.xy;
                half4 texColor = SAMPLE_TEXTURE2D(unityTexture2D.tex, unityTexture2D.samplerstate, unityTexture2D.GetTransformedUV(distortedUV));
                half4 tintedTexColor = texColor * _TintColor;

                UnityTexture2D unityTexture2DNormal = UnityBuildTexture2DStructNoScale(_NormalMap);
                half4 texNormal = SAMPLE_TEXTURE2D(unityTexture2DNormal.tex, unityTexture2DNormal.samplerstate, unityTexture2D.GetTransformedUV(distortedUV));
                texNormal.xyz = UnpackNormal(texNormal);
                half3 texNormalStrength = half3(texNormal.xy * _NormalStrength, lerp(1, texNormal.z, saturate(_NormalStrength)));;

                half2 refractUV0 = RefractUV(_IOR, 1.0, texNormalStrength, IN.WorldSpaceNormal, IN.AbsoluteWorldSpacePosition);
                half3 sceenPos0 = half3(refractUV0, 0) + noiseDistortion;
                half3 sceneColor0 = SampleSceneColor(sceenPos0.xy);

                half2 refractUV1 = RefractUV(_IOR * (1.0 + _ChromaticAberration), 1.0, texNormalStrength, IN.WorldSpaceNormal, IN.AbsoluteWorldSpacePosition);
                half3 sceenPos1 = half3(refractUV1, 0) + noiseDistortion;
                half3 sceneColor1 = SampleSceneColor(sceenPos1.xy);

                half2 refractUV2 = RefractUV(_IOR * (1.0 + _ChromaticAberration * 2.0), 1.0, texNormalStrength, IN.WorldSpaceNormal, IN.AbsoluteWorldSpacePosition);
                half3 sceenPos2 = half3(refractUV2, 0) + noiseDistortion;
                half3 sceneColor2 = SampleSceneColor(sceenPos2.xy);

                half3 sceneColor = half3(sceneColor0.r, sceneColor1.g, sceneColor2.b);

                half3 tintedTexSceneColor = tintedTexColor.xyz * sceneColor;

                half3 reflectVec = reflect(-IN.WorldSpaceViewDirection, IN.WorldSpaceNormal);
                half3 refProbeColor = DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVec, 0), unity_SpecCube0_HDR);

                half3 refProbeColorWithStrength = refProbeColor * _ReflectionStrength.xxx;

                half3 tintedTexSceneColorEmission = _Emission ? tintedTexSceneColor : half3(0, 0, 0);

                surface.BaseColor = tintedTexSceneColor;
                surface.NormalTS = noiseDistortion + texNormalStrength;
                surface.Emission = lerp(tintedTexSceneColorEmission, refProbeColorWithStrength, _ReflectionStrength/5.0);
                surface.Metallic = _Metallic;
                surface.Smoothness = _Smoothness;
                surface.Occlusion = 1.0;
                surface.Alpha = tintedTexColor.a + _TintColor.a;

                return surface;
            }

            void InitializeInputData(Varyings input, SurfaceDescription surfaceDescription, out InputData inputData)
            {
                inputData = (InputData)0;

                inputData.positionWS = input.positionWS;

#ifdef _NORMALMAP
                half crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
                half3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

                inputData.tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

    #if _NORMAL_DROPOFF_TS
                inputData.normalWS = TransformTangentToWorld(surfaceDescription.NormalTS, inputData.tangentToWorld);
    #endif

#else
                inputData.normalWS = input.normalWS;
#endif

                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                inputData.shadowCoord = half4(0, 0, 0, 0);

                inputData.fogCoord = InitializeInputDataFog(half4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;

                inputData.bakedGI = SampleSHPixel(input.sh, inputData.normalWS);

                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(input);
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

                bool isTransparent = false;
                half alpha = half(1.0);

                InputData inputData;
                InitializeInputData(input, surfaceDescription, inputData);

                half3 specular = 0;
                half metallic = surfaceDescription.Metallic;

                half3 normalTS = half3(0, 0, 0);

#if defined(_NORMALMAP) && defined(_NORMAL_DROPOFF_TS)
                normalTS = surfaceDescription.NormalTS;
#endif

                SurfaceData surface;
                surface.albedo              = surfaceDescription.BaseColor;
                surface.metallic            = saturate(metallic);
                surface.specular            = specular;
                surface.smoothness          = saturate(surfaceDescription.Smoothness),
                surface.occlusion           = surfaceDescription.Occlusion,
                surface.emission            = surfaceDescription.Emission,
                surface.alpha               = saturate(alpha);
                surface.normalTS            = normalTS;
                surface.clearCoatMask       = 0;
                surface.clearCoatSmoothness = 1;

                surface.albedo = AlphaModulate(surface.albedo, surface.alpha);

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);

                color.a = OutputAlpha(color.a, isTransparent);

                return color;
            }

            ENDHLSL
        }
    }
} // Copyright Anexas 2026