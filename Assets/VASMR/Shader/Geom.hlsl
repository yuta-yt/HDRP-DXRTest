#define DIVIDE 6
#define TAU 6.28318530718
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/jp.keijiro.noiseshader/Shader/SimplexNoise3D.hlsl"

StructuredBuffer<float3> _PositionBuffer;


float _Radius;
float _Length;

float3 _Wind;
float _windSpeed;
float _windScale;
float _windStrength;

float _bendForward;
float _bendCurve;

float _rotRand;

float _time;

float3x3 rot3D(float3 axis, float angle)
{
    float c, s;
    sincos(angle, s, c);

    float t = 1 - c;
    float x = axis.x;
    float y = axis.y;
    float z = axis.z;

    return float3x3(
        t * x * x + c,      t * x * y - s * z,  t * x * z + s * y,
        t * x * y + s * z,  t * y * y + c,      t * y * z - s * x,
        t * x * z - s * y,  t * y * z + s * x,  t * z * z + c
    );
}

struct MeshAttributes
{
    float3 position;
    float3 normal;
    float4 tangent;
    float2 uv;
};

AttributesMesh ConvertToAttributesMesh(MeshAttributes mesh)
{
    AttributesMesh am;
    am.positionOS = mesh.position;
#ifdef ATTRIBUTES_NEED_NORMAL
    am.normalOS = mesh.normal;
#endif
#ifdef ATTRIBUTES_NEED_TANGENT
    am.tangentOS = mesh.tangent;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD0
    am.uv0 = mesh.uv;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD1
    am.uv1 = 0;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD2
    am.uv2 = 0;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD3
    am.uv3 = 0;
#endif
#ifdef ATTRIBUTES_NEED_COLOR
    am.color = 0;
#endif
    UNITY_TRANSFER_INSTANCE_ID(input, am);
    return am;
}

PackedVaryingsType VertexOutput(MeshAttributes mesh)
{
    AttributesMesh am = ConvertToAttributesMesh(mesh);

    VaryingsType vt;
    vt.vmesh = VertMesh(am);
#if SHADERPASS == SHADERPASS_MOTION_VECTORS
    AttributesPass ap;
    ap.previousPositionOS = mesh.position;
    return MotionVectorVS(vt, am, ap);
#endif
    return PackVaryingsType(vt);
}

// Geometry shader function body
[maxvertexcount(DIVIDE*4)]
void GrassGeom( point AttributesToGS input[1],
                inout TriangleStream<PackedVaryingsType> outStream)
{
    uint vid = input[0].vertexID;

    float div = 1 / (float)DIVIDE;

    float3 pos = _PositionBuffer[vid];
    float length = _Length * pos.y * (snoise(pos * 1 + 2)+1*.5);

    pos += snoise_grad(pos*20) * float3(1,0,1) * .1;
    float3 p = float3(pos.x, 0, pos.z);

    float forward = (snoise(p.zzx * 30)+1*.5) * _bendForward;

    float ep = .04;
    p.y -= ep;

    float3 pp = p;

    float3x3 r = rot3D(float3(0,1,0), snoise(p.zxz * 80) * TAU * _rotRand);

    float2 windN = snoise_grad(p.xzx * _windScale + _Wind * _Time * _windSpeed).xz * 2 - 1;
    float3x3 windRot = rot3D(normalize(float3(windN.x, 0, windN.y)), _windStrength * TAU);

    float3 X = mul(mul(float3(1,0,0), r), windRot);
    float3 Z = mul(mul(float3(0,0,1), r), windRot);
    float3 Y = cross(Z, X);

    [unroll]
    for(int i = 0; i < DIVIDE - 1; i++){
        MeshAttributes o0, o1, o2, o3;

        //float dy = length * div * i;
        float t = div * i;
        float f = pow(t, _bendCurve) * forward;
        
        float3 pN = p + (Y * length * div) + Z * f;
        float rad  =  1 - (div * i);
        float radN =  1 - (div * (i + 1));
        
        float3 pd  =  X * _Radius * .5 * rad; 
        float3 pdN =  X * _Radius * .5 * radN; 

        o0.position = p  + pd;
        o1.position = p  - pd;
        o2.position = pN - pdN;
        o3.position = pN + pdN;

        float3 pNN = pN + (Y * length * div) + Z * f;
        float3 d = normalize(pN - pp);
        float3 dN = normalize(pNN - p);
        float3 n = normalize(cross(d, X));
        float3 nN = normalize(cross(dN, X));
        //n = float3(0,0,-1);
        //n *= -sign(n.z);
        o0.normal = n;
        o1.normal = n;
        o2.normal = nN;
        o3.normal = nN;

        o0.uv = float2( 1, rad);
        o1.uv = float2( 0, rad);
        o2.uv = float2( 0, radN);
        o3.uv = float2( 1, radN);

        o0.tangent = float4(d,-1);
        o1.tangent = float4(d,-1);
        o2.tangent = float4(dN,-1);
        o3.tangent = float4(dN,-1);

        outStream.Append(VertexOutput(o0));
        outStream.Append(VertexOutput(o1));
        outStream.Append(VertexOutput(o3));
        outStream.Append(VertexOutput(o2));
        outStream.RestartStrip();

        p = pN;
        pp = p;
    }

    //float dy = length * div * (DIVIDE-1);
    //p.y = dy - ep;
    float3 pN = p + (Y * length * div) + Z * pow(.95, _bendCurve)*forward;
    float rad  =  1 - (div * (DIVIDE-1));
    float3 pd  =  float3(_Radius * .5 * rad, 0, 0); 

    MeshAttributes o0, o1, o2;

    o0.position = p  + pd;
    o1.position = p  - pd;
    o2.position = pN;

    float3 d = normalize(pN - pp);
    float3 n = normalize(cross(d, float3(1,0,0)));
    //n = float3(0,0,-1);
    n *= -sign(n.z);
    o0.normal = n;
    o1.normal = n;
    o2.normal = n;

    o0.uv = float2( 1, rad);
    o1.uv = float2( 0, rad);
    o2.uv = float2(.5, 1);

    o0.tangent = float4(d,-1);
    o1.tangent = float4(d,-1);
    o2.tangent = float4(d,-1);

    outStream.Append(VertexOutput(o0));
    outStream.Append(VertexOutput(o1));
    outStream.Append(VertexOutput(o2));
    outStream.RestartStrip();
}