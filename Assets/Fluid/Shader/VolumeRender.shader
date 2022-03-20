Shader "Unlit/VolumeRender"
{
    Properties
    {
        _densityRT ("density", 2D) = "white" {}
        _noise3D("noise3D", 3D) = "white" {}

        _DensityMul("DensityMul", float) = 36.0
        _LightAbsorptionThroughCloud("LightAbsorptionThroughCloud", float) = 0.5
        _LightAbsorptionTowardSun("LightAbsorptionTowardSun", float) = 0.24
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue" = "Transparent"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        AlphaToMask Off
		Cull Back
		ColorMask RGBA
		ZWrite Off
		ZTest LEqual
		Offset 0 , 0

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "UnityShaderVariables.cginc"
            #include "VolumeRender.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 texcoord1 : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _densityRT;
            sampler3D _noise3D;
            float4 _densityRT_ST;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _densityRT);
                UNITY_TRANSFER_FOG(o,o.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 scale = float3(length(unity_ObjectToWorld[0].xyz), length(unity_ObjectToWorld[1].xyz), length(unity_ObjectToWorld[2].xyz));
                float3 worldPos = mul(unity_ObjectToWorld, float4(float3(0, 0, 0), 1)).xyz;
                float3 boundsMin = (worldPos + (scale * float3(-0.5, -0.5, -0.5)));
                float3 boundsMax = (worldPos + (scale * float3(0.5, 0.5, 0.5)));
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize((i.worldPos - _WorldSpaceCameraPos));

                float depthfade = DepthFade(i.texcoord1, _CameraDepthTexture);
                float4 cloudColor = rayMarching(boundsMin, boundsMax, rayOrigin, rayDir, _noise3D,_densityRT, depthfade);

                //fixed4 col = tex2D(_densityRT, i.uv);
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return float4(clamp(cloudColor.x, 0, 1), clamp(cloudColor.y, 0, 1), clamp(cloudColor.z, 0, 1), clamp(cloudColor.w, 0, 1));
                return cloudColor * _LightColor0;
            }
            ENDCG
        }
    }
}
