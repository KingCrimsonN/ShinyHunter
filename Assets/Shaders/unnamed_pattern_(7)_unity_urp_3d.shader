Shader "Kaleidos/unnamed pattern (7) 3D URP" {
    Properties {
        _Zoom ("Zoom (mesh size in metres)", Float) = 1
        _Pan ("Pan", Vector) = (0,0,0,0)
        _Rotation ("Rotation", Float) = 0
        _Tint ("Tint", Color) = (1,1,1,1)
        _Speed ("Speed", Float) = 1
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

            #define uTime (_Time.y * _Speed)

static const float noiseScale = 6.0;
static const float gain = 0.5;
float hash3(float3 p){
  p = frac(p * float3(127.1, 311.7, 74.7));
  p += dot(p, p.yxz + 19.19);
  return frac((p.x + p.y) * p.z);
}
float snoise3(float3 p){
  float3 i = floor(p); float3 fr = frac(p);
  float3 u = fr * fr * (3.0 - 2.0 * fr);
  return lerp(
    lerp(lerp(hash3(i+float3(0,0,0)), hash3(i+float3(1,0,0)), u.x), lerp(hash3(i+float3(0,1,0)), hash3(i+float3(1,1,0)), u.x), u.y),
    lerp(lerp(hash3(i+float3(0,0,1)), hash3(i+float3(1,0,1)), u.x), lerp(hash3(i+float3(0,1,1)), hash3(i+float3(1,1,1)), u.x), u.y),
    u.z) * 2.0 - 1.0;
}

float fbm3(float3 p){
  float v = 0.0, a = 0.5, norm = 0.0;
  for (int i = 0; i < 6; i++){
    float sn = snoise3(p);
    float t = sn * 0.5 + 0.5;
    v += a * t; norm += a;
    p = p * 2.0 + float3(5.3, 1.7, 3.1);
    a *= gain;
  }
  return v / norm;
}

float fieldVolume(float3 p){
  float3 q = p * noiseScale;
  q.xy += uTime * float2(0.08, 0.08);
  q.z  += uTime * 0.08;
  return fbm3(q);
}

static const float4 PALETTE[110] = {
  float4(0.146163,0.69959,0.0475257,1.0),
  float4(0.147259,0.699306,0.0533577,1.0),
  float4(0.148355,0.699022,0.0591898,1.0),
  float4(0.149451,0.698738,0.0650219,1.0),
  float4(0.150546,0.698454,0.070854,1.0),
  float4(0.151642,0.69817,0.0766861,1.0),
  float4(0.152738,0.697886,0.0825181,1.0),
  float4(0.153834,0.697602,0.0883502,1.0),
  float4(0.15493,0.697318,0.0941823,1.0),
  float4(0.156026,0.697034,0.100014,1.0),
  float4(0.157122,0.69675,0.105846,1.0),
  float4(0.158218,0.696466,0.111679,1.0),
  float4(0.159313,0.696183,0.117511,1.0),
  float4(0.160409,0.695899,0.123343,1.0),
  float4(0.161505,0.695615,0.129175,1.0),
  float4(0.162601,0.695331,0.135007,1.0),
  float4(0.163697,0.695047,0.140839,1.0),
  float4(0.164793,0.694763,0.146671,1.0),
  float4(0.165889,0.694479,0.152503,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(1.0,1.0,1.0,1.0),
  float4(0.18671,0.689085,0.263313,1.0),
  float4(0.187806,0.688801,0.269145,1.0),
  float4(0.188902,0.688517,0.274977,1.0),
  float4(0.189998,0.688233,0.280809,1.0),
  float4(0.191094,0.687949,0.286641,1.0),
  float4(0.19219,0.687665,0.292473,1.0),
  float4(0.193286,0.687381,0.298305,1.0),
  float4(0.194382,0.687097,0.304137,1.0),
  float4(0.195477,0.686813,0.309969,1.0),
  float4(0.196573,0.686529,0.315801,1.0),
  float4(0.197669,0.686245,0.321633,1.0),
  float4(0.198765,0.685961,0.327465,1.0),
  float4(0.199861,0.685678,0.333297,1.0),
  float4(0.200957,0.685394,0.33913,1.0),
  float4(0.202053,0.68511,0.344962,1.0),
  float4(0.203149,0.684826,0.350794,1.0),
  float4(0.204244,0.684542,0.356626,1.0),
  float4(0.20534,0.684258,0.362458,1.0),
  float4(0.206436,0.683974,0.36829,1.0),
  float4(0.207532,0.68369,0.374122,1.0),
  float4(0.208628,0.683406,0.379954,1.0),
  float4(0.209724,0.683122,0.385786,1.0),
  float4(0.21082,0.682838,0.391618,1.0),
  float4(0.211916,0.682554,0.39745,1.0),
  float4(0.213012,0.682271,0.403282,1.0),
  float4(0.214107,0.681987,0.409114,1.0),
  float4(0.215203,0.681703,0.414947,1.0),
  float4(0.216299,0.681419,0.420779,1.0),
  float4(0.217395,0.681135,0.426611,1.0),
  float4(0.0,0.0,0.0,1.0),
  float4(0.0,0.0,0.0,1.0),
  float4(0.0,0.0,0.0,1.0),
  float4(0.0,0.0,0.0,1.0),
  float4(0.0,0.0,0.0,1.0),
  float4(0.0,0.0,0.0,1.0),
  float4(0.225066,0.679147,0.467435,1.0),
  float4(0.226162,0.678863,0.473267,1.0),
  float4(0.227258,0.67858,0.479099,1.0),
  float4(0.228354,0.678296,0.484931,1.0),
  float4(0.22945,0.678012,0.490764,1.0),
  float4(0.230546,0.677728,0.496596,1.0),
  float4(0.231641,0.677444,0.502428,1.0),
  float4(0.232737,0.67716,0.50826,1.0),
  float4(0.233833,0.676876,0.514092,1.0),
  float4(0.234929,0.676592,0.519924,1.0),
  float4(0.236025,0.676308,0.525756,1.0),
  float4(0.237121,0.676024,0.531588,1.0),
  float4(0.238217,0.67574,0.53742,1.0),
  float4(0.239313,0.675456,0.543252,1.0),
  float4(0.240409,0.675173,0.549084,1.0),
  float4(0.241504,0.674889,0.554916,1.0),
  float4(0.2426,0.674605,0.560748,1.0),
  float4(0.243696,0.674321,0.566581,1.0),
  float4(0.244792,0.674037,0.572413,1.0),
  float4(0.245888,0.673753,0.578245,1.0),
  float4(0.246984,0.673469,0.584077,1.0),
  float4(0.24808,0.673185,0.589909,1.0),
  float4(0.249176,0.672901,0.595741,1.0),
  float4(0.250271,0.672617,0.601573,1.0),
  float4(0.251367,0.672333,0.607405,1.0),
  float4(0.252463,0.672049,0.613237,1.0),
  float4(0.253559,0.671766,0.619069,1.0),
  float4(0.254655,0.671482,0.624901,1.0),
  float4(0.255751,0.671198,0.630733,1.0),
  float4(0.256847,0.670914,0.636565,1.0),
  float4(0.257943,0.67063,0.642398,1.0),
  float4(0.259038,0.670346,0.64823,1.0),
  float4(0.260134,0.670062,0.654062,1.0),
  float4(0.26123,0.669778,0.659894,1.0),
  float4(0.262326,0.669494,0.665726,1.0),
  float4(0.263422,0.66921,0.671558,1.0),
  float4(0.264518,0.668926,0.67739,1.0),
  float4(0.252005,0.67042,0.664055,1.0)
};
float4 paletteLookup(float x){
  int i = clamp(int(clamp(x,0.0,1.0)*256.0),0,255);
  return PALETTE[clamp(int(float(i)/255.0*110.0),0,109)];
}

float fieldSample(float3 p){
  float n = clamp(fieldVolume(p), 0.0, 1.0);
  return n;
}

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; float3 objPos : TEXCOORD0; };
            Varyings vert(Attributes IN){
                Varyings o;
                o.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.objPos = IN.positionOS.xyz;
                return o;
            }

            float4 frag(Varyings i) : SV_Target {
                float3 p = i.objPos;
                float s = sin(_Rotation), cs = cos(_Rotation);
                p.xz = mul(p.xz, float2x2(cs, -s, s, cs));
                p = p / max(_Zoom, 0.0001) + float3(_Pan.xy, 0.0);
                float n = fieldSample(p);
                float4 c = paletteLookup(n);
                float3 col = c.rgb * c.a * _Tint.rgb;
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}