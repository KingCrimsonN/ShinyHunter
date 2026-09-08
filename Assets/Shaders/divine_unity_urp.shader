Shader "Kaleidos/divine 2D URP" {
    Properties {
        _Zoom ("Zoom", Float) = 1
        _Pan ("Pan", Vector) = (0,0,0,0)
        _Rotation ("Rotation", Float) = 0
        _Tint ("Tint", Color) = (1,1,1,1)
        _Speed ("Speed", Float) = 1
        _CellSize ("Pixel Size", Float) = 1.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _Zoom;
            float4 _Pan;
            float _Rotation;
            float4 _Tint;
            float _Speed;
            float _CellSize;

            #define uTime (_Time.y * _Speed)
            #define uResolution float2(640.0, 360.0)
            #define cellSize _CellSize

#define time uTime
static const float noiseScale = 8.0;
#define screenSize float2(640.0, 360.0)
#define sampleOffset (float2)(0.0)
#define renderSize float2(640.0, 360.0)

static const float octaves = 6.0;
static const float gain = 0.5;

#define morphPhase (uTime*0.12)
#define scrollSpeed float2(0.08, 0.08)
static const float2 loopD = float2(0.0, 0.0);
static const float loopZ = 0.0;
static const float warpStrength = 0.45;
static const float warpScale = 7.5;
static const float pinchStrength = 0.0;
#define warpPhase (uTime*0.0)

static const float4 PALETTE[110] = {
  float4(0.994245,0.994221,0.996014,1.0),
  float4(0.988488,0.988439,0.992029,1.0),
  float4(0.979268,0.97918,0.985652,1.0),
  float4(0.968882,0.968752,0.978477,1.0),
  float4(0.958485,0.95831,0.971303,1.0),
  float4(0.948075,0.947856,0.964129,1.0),
  float4(0.937651,0.937388,0.956955,1.0),
  float4(0.927215,0.926907,0.949781,1.0),
  float4(0.916763,0.916411,0.942606,1.0),
  float4(0.906297,0.9059,0.935432,1.0),
  float4(0.895816,0.895374,0.928258,1.0),
  float4(0.885321,0.884833,0.921084,1.0),
  float4(0.874808,0.874275,0.91391,1.0),
  float4(0.864279,0.863701,0.906735,1.0),
  float4(0.853734,0.85311,0.899561,1.0),
  float4(0.843173,0.842502,0.892387,1.0),
  float4(0.832593,0.831875,0.885213,1.0),
  float4(0.821993,0.821229,0.878039,1.0),
  float4(0.811376,0.810565,0.870864,1.0),
  float4(0.800738,0.79988,0.86369,1.0),
  float4(0.79008,0.789174,0.856516,1.0),
  float4(0.779401,0.778448,0.849342,1.0),
  float4(0.7687,0.767699,0.842168,1.0),
  float4(0.757978,0.756927,0.834994,1.0),
  float4(0.74723,0.746132,0.827819,1.0),
  float4(0.736461,0.735313,0.820645,1.0),
  float4(0.725665,0.724468,0.813471,1.0),
  float4(0.714844,0.713597,0.806297,1.0),
  float4(0.703996,0.702699,0.799123,1.0),
  float4(0.69312,0.691773,0.791948,1.0),
  float4(0.682216,0.680818,0.784774,1.0),
  float4(0.671282,0.669833,0.7776,1.0),
  float4(0.667018,0.663674,0.76943,1.0),
  float4(0.67443,0.665319,0.759267,1.0),
  float4(0.691618,0.670181,0.746117,1.0),
  float4(0.718653,0.680576,0.730975,1.0),
  float4(0.737986,0.683144,0.699491,1.0),
  float4(0.755848,0.687338,0.67185,1.0),
  float4(0.77371,0.689177,0.640861,1.0),
  float4(0.791572,0.691244,0.610617,1.0),
  float4(0.809434,0.693519,0.58105,1.0),
  float4(0.827295,0.695985,0.552104,1.0),
  float4(0.845157,0.698627,0.523727,1.0),
  float4(0.863019,0.701428,0.495871,1.0),
  float4(0.875716,0.731341,0.433578,1.0),
  float4(0.8873,0.76256,0.374937,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(0.897667,0.771628,0.366017,1.0),
  float4(0.888778,0.744749,0.423626,1.0),
  float4(0.871106,0.714313,0.493323,1.0),
  float4(0.844434,0.708527,0.533436,1.0),
  float4(0.817762,0.703076,0.574696,1.0),
  float4(0.791089,0.698004,0.617262,1.0),
  float4(0.764417,0.693367,0.661317,1.0),
  float4(0.737745,0.686899,0.703223,1.0),
  float4(0.707936,0.678392,0.738093,1.0),
  float4(0.679214,0.666897,0.754975,1.0),
  float4(0.664894,0.660803,0.765649,1.0),
  float4(0.66301,0.661522,0.772185,1.0),
  float4(0.669836,0.66838,0.776652,1.0),
  float4(0.67665,0.675226,0.781119,1.0),
  float4(0.683452,0.68206,0.785586,1.0),
  float4(0.690244,0.688883,0.790053,1.0),
  float4(0.697023,0.695694,0.79452,1.0),
  float4(0.703792,0.702494,0.798987,1.0),
  float4(0.71055,0.709283,0.803454,1.0),
  float4(0.767854,0.740674,0.736973,1.0),
  float4(0.797716,0.745343,0.642369,1.0),
  float4(0.827577,0.750598,0.550756,1.0),
  float4(0.857439,0.75636,0.461725,1.0),
  float4(0.8873,0.76256,0.374937,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(0.8873,0.76256,0.374937,1.0),
  float4(0.87173,0.771228,0.473008,1.0),
  float4(0.85616,0.78014,0.572237,1.0),
  float4(0.84059,0.789316,0.67271,1.0),
  float4(0.825019,0.798771,0.774519,1.0),
  float4(0.790886,0.789983,0.857058,1.0),
  float4(0.797523,0.79665,0.861525,1.0),
  float4(0.804152,0.803309,0.865991,1.0),
  float4(0.810773,0.80996,0.870458,1.0),
  float4(0.817388,0.816604,0.874925,1.0),
  float4(0.823993,0.823239,0.879392,1.0),
  float4(0.830594,0.829868,0.883859,1.0),
  float4(0.837186,0.836489,0.888326,1.0),
  float4(0.843771,0.843103,0.892793,1.0),
  float4(0.85035,0.84971,0.89726,1.0),
  float4(0.856921,0.85631,0.901727,1.0),
  float4(0.863485,0.862903,0.906194,1.0),
  float4(0.870043,0.869489,0.910661,1.0),
  float4(0.876594,0.876069,0.915128,1.0),
  float4(0.88314,0.882643,0.919595,1.0),
  float4(0.889679,0.88921,0.924062,1.0),
  float4(0.896212,0.895772,0.928529,1.0),
  float4(0.902739,0.902327,0.932996,1.0),
  float4(0.909261,0.908876,0.937463,1.0),
  float4(0.915777,0.91542,0.94193,1.0),
  float4(0.922288,0.921958,0.946397,1.0),
  float4(0.928791,0.92849,0.950864,1.0),
  float4(0.935289,0.935016,0.95533,1.0),
  float4(0.941783,0.941538,0.959797,1.0),
  float4(0.948271,0.948053,0.964264,1.0),
  float4(0.954755,0.954564,0.968731,1.0),
  float4(0.961233,0.96107,0.973198,1.0),
  float4(0.967706,0.96757,0.977665,1.0),
  float4(0.974174,0.974066,0.982132,1.0),
  float4(0.980638,0.980556,0.986599,1.0),
  float4(0.987096,0.987042,0.991066,1.0),
  float4(0.992834,0.992804,0.995037,1.0),
  float4(0.996417,0.996402,0.997518,1.0)
};
float4 paletteLookup(float x){
  int i = clamp(int(clamp(x,0.0,1.0)*256.0),0,255);
  return PALETTE[clamp(int(float(i)/255.0*110.0),0,109)];
}

#define TAU 6.28318530718
#define MAX_OCTAVES 10

float2 frameC(float2 uv) {
    float2 r = renderSize;
    return (uv - 0.5) * screenSize / r.y;
}
float2 frameUV(float2 c) {
    float2 r = renderSize;
    return 0.5 + c * r.y / screenSize;
}

float hash3(float3 p) {
    p = frac(p * float3(127.1, 311.7, 74.7));
    p += dot(p, p.yxz + 19.19);
    return frac((p.x + p.y) * p.z);
}

float vnoise3(float3 p) {
    float3 i = floor(p);
    float3 f = frac(p);
    float3 u = f * f * (3.0 - 2.0 * f);
    return lerp(
        lerp(lerp(hash3(i + float3(0,0,0)), hash3(i + float3(1,0,0)), u.x),
            lerp(hash3(i + float3(0,1,0)), hash3(i + float3(1,1,0)), u.x), u.y),
        lerp(lerp(hash3(i + float3(0,0,1)), hash3(i + float3(1,0,1)), u.x),
            lerp(hash3(i + float3(0,1,1)), hash3(i + float3(1,1,1)), u.x), u.y),
        u.z
    );
}

float basisSample(float3 p, float2 d, float pz) {

    return vnoise3(p) * 2.0 - 1.0;
}

float fbm3(float3 p) {
    float v = 0.0, a = 0.5, norm = 0.0;
    float2  d  = loopD;
    float pz = loopZ;
    int oct = int(octaves + 0.5);
    for (int i = 0; i < MAX_OCTAVES; i++) {
        if (i >= oct) break;
        float sn = basisSample(p, d, pz);
        float t;
        {
            t = sn * 0.5 + 0.5;
        }
        v    += a * t;
        norm += a;
        p     = p * 2.0 + float3(5.3, 1.7, 3.1);
        d    *= 2.0;
        pz   *= 2.0;
        a    *= gain;
    }
    return v / norm;
}

#define DOMAIN_AMP  0.35
#define PINCH_R     0.8
#define PINCH_K     0.9
#define PINCH_PULSE 0.5

float whash2(float2 p) {
    p = frac(p * float2(127.1, 311.7));
    p += dot(p, p.yx + 19.19);
    return frac((p.x + p.y) * 43.32);
}
float wnoise2(float2 p) {
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(whash2(i + float2(0.0, 0.0)), whash2(i + float2(1.0, 0.0)), u.x),
               lerp(whash2(i + float2(0.0, 1.0)), whash2(i + float2(1.0, 1.0)), u.x), u.y);
}
float wfbm2(float2 p) {
    float v = 0.0, a = 0.5, norm = 0.0;
    for (int i = 0; i < 3; i++) {
        v += a * (wnoise2(p) * 2.0 - 1.0);
        norm += a; p = p * 2.0 + float2(5.3, 1.7); a *= 0.5;
    }
    return v / norm;
}

float2 warpField(float2 uv) {
    float2 fp = (float2)(warpPhase);
    
    return float2(wfbm2(uv * warpScale + fp), wfbm2(uv * warpScale + fp + float2(31.4, 17.7)));
}
float2 warpUV(float2 uv) {
    
    {
        float2 q = warpField(uv);
        return uv + warpStrength * DOMAIN_AMP * warpField(uv + warpStrength * q);
    }
    float2  c   = frameC(uv);
    float r   = length(c);
    if (r > 1e-5) {
        float k  = pinchStrength * (1.0 + PINCH_PULSE * sin(warpPhase * TAU));
        float rn = min(r / PINCH_R, 1.0);
        float factor;
        if (k >= 0.0) {
            factor = pow(rn, k * PINCH_K);
        } else {
            factor = 1.0 + (-k) * PINCH_K * (1.0 - rn * rn); 
        }
        c *= factor;
    }
    return frameUV(c);
}

float2 rotateUV(float2 uv) {
    return uv;
}

float fieldN(float2 uv, float mzExtra) {
    float mz = morphPhase + mzExtra;

    float2 nsv = (float2)(noiseScale);
    return fbm3(float3(uv * nsv + time * scrollSpeed, mz));
}

float4 shade(float2 screen_coords) {
    float2 block = floor((screen_coords + sampleOffset) / cellSize) * cellSize;
    float2 uv0   = warpUV(block / screenSize);
    float mzAdd = 0.0, valAdd = 0.0;
    float2 uvDisp = (float2)(0.0);

    float n = fieldN(rotateUV(uv0 + uvDisp), mzAdd);
    n = clamp(n + valAdd, 0.0, 1.0);
    return paletteLookup(n);
}


            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes IN){
                Varyings o;
                o.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.uv = IN.uv;
                return o;
            }

            float4 frag(Varyings i) : SV_Target {
                float2 px = 1.0 / fwidth(i.uv);
                float ar = px.x / px.y;
                float2 uv = i.uv - 0.5;
                if (ar > 1.77778) uv.x *= ar / 1.77778; else uv.y *= 1.77778 / ar;
                float s = sin(_Rotation), cs = cos(_Rotation);
                uv = mul(uv, float2x2(cs, -s, s, cs));
                uv = uv / _Zoom + _Pan.xy;
                float4 c = shade((uv + 0.5) * float2(640.0, 360.0));
                return float4(c.rgb * c.a, 1.0) * _Tint;
            }
            ENDHLSL
        }
    }
}