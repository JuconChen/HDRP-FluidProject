//#include "UnityCG.cginc"

float2 rayBoxTest(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 rayDir)
{
    float3 t0 = (boundsMin - rayOrigin) / rayDir;
    float3 t1 = (boundsMax - rayOrigin) / rayDir;

    float3 tMin = min(t0, t1);
    float3 tMax = max(t0, t1);

    float dstA = max(max(tMin.x, tMin.y), tMin.z);
    float dstB = min(min(tMax.x, tMax.y), tMax.z);

    float dstToBox = max(0, dstA);
    float dstInsideBox = max(0, dstB - dstToBox);
    return float2(dstToBox, dstInsideBox);
}

float _DensityMul;
//云的吸收
float _LightAbsorptionThroughCloud;
//朝向太阳的吸收
float _LightAbsorptionTowardSun;

float SampleDensity(sampler3D volumeTex,sampler2D densityTex, float3 pos)
{
    float3 density = tex2D(densityTex,float2(1 - pos.x,1 - pos.z)) * _DensityMul;
    return  tex3D(volumeTex, pos).x * density * _DensityMul ;
}

float LightMarch(float3 boundsMin, float3 boundsMax, float3 rayOrigin, sampler3D volumeTex,sampler2D densityTex)
{
    float stepNum = 10;
    float3 rayDir = _WorldSpaceLightPos0;
    rayDir = normalize(float3(0,10,10) - rayOrigin);
    float inside = rayBoxTest(boundsMin, boundsMax, rayOrigin, rayDir).y;
    float stepSize = inside / stepNum;
    float totalDensity = 0;
    float3 samplePoint = rayOrigin;
    float3 center = mul(unity_ObjectToWorld, float4(0, 0, 0, 1));
    float3 scale = 1.0f / (boundsMax - boundsMin);
    for (int step = 0; step < stepNum; step++)
    {
        float3 pos = (samplePoint - center) * scale  + float3(0.5, 0.5, 0.5);
        totalDensity += SampleDensity(volumeTex, densityTex,pos )* stepSize;
    }
    float transmittance = exp(-totalDensity* _LightAbsorptionTowardSun);
    return transmittance;
}
//步进最大数
#define StepMaxNum 25
//boundsMin:包围盒的最小值 boundsMax:包围盒的最大值  rayOrigin:射线的起点 rayDir:射线的方向 volumeTex：体积纹理
float4 rayMarching(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 rayDir,sampler3D volumeTex,sampler2D densityTex,float depthFade)
{
    float3 center = mul(unity_ObjectToWorld, float4(0, 0, 0, 1));

    float2 hitInfo = rayBoxTest(boundsMin, boundsMax, rayOrigin, rayDir);

    hitInfo.y = min(depthFade, hitInfo.y);

    float stepSize = hitInfo.y / StepMaxNum;

    float3 samplePoint = rayOrigin + rayDir*hitInfo.x;

    float3 scale = 1.0f/(boundsMax - boundsMin);

    //透光率
    float transmittance = 1;
    float3 Energy = 0;
    for (int step = 0; step < StepMaxNum; step++)
    {
        float3 pos = (samplePoint - center) * scale + float3(0.5, 0.5, 0.5);
        //采样密度
        float density = SampleDensity(volumeTex,densityTex,pos) * stepSize;

        Energy += LightMarch(boundsMin, boundsMax, samplePoint, volumeTex,densityTex)* density* transmittance;
        //Energy += 0.01;
        
        transmittance *= exp(-density* _LightAbsorptionThroughCloud);

        samplePoint += rayDir * stepSize;
    }
    //return float4(Energy,1);
    return float4(Energy,1-transmittance);
}

float DepthFade(float4 screenPos,sampler2D cameraDepthTexture)
{
    float4 screenPosNorm = screenPos / screenPos.w;
    screenPosNorm.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? screenPosNorm.z : screenPosNorm.z * 0.5 + 0.5;
    float screenDepth = LinearEyeDepth(tex2D(cameraDepthTexture, screenPosNorm.xy));
    return abs((screenDepth - LinearEyeDepth(screenPosNorm.z)) / (1.0));
}