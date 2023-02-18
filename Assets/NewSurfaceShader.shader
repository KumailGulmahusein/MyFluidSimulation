// Upgrade NOTE: commented out 'float4x4 _CameraToWorld', a built-in variable
// Upgrade NOTE: commented out 'float4x4 _WorldToCamera', a built-in variable
// Upgrade NOTE: replaced '_WorldToCamera' with 'unity_WorldToCamera'

Shader "Custom/NewSurfaceShader"
{
	Properties
	{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_DepthTex("Depth", 2D) = "white" {}
		_BlurRadius("Blur Radius", Range(0,1)) = 0.5
		_WaveSpeed("Wave Speed", Range(0, 10)) = 1
		_WaveHeight("Wave Height", Range(0, 0.5)) = 0.1
		_ReflectionAmount("Reflection Amount", Range(0, 1)) = 0
		_ReflectionCube("Reflection Cube", Cube) = "" {}
		_RefractionAmount("Refraction Amount", Range(0, 1)) = 0
		_RefractionIndex("Refraction Index", Range(0, 2)) = 1
		_RefractionTex("Refraction Texture", 2D) = "white" {}
	}

		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		CGPROGRAM
		#include "UnityCG.cginc"

		sampler2D _MainTex;
		sampler2D _DepthTex;
		float4 _Color;
		float _BlurDir;
		float _BlurRadius;
		float _BlurDepthFalloff;

		// float4x4 _WorldToCamera;
		// float4x4 _CameraToWorld;
		float4x4 _ProjectionMatrix;

		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
			float3 worldNormal;
			float3 worldTangent;
			float3 worldBinormal;
			float4 screenPos;
			float3 worldReflection;
			float3 worldNormalSOS;
			float3 worldTangentSOS;
			float3 worldBinormalSOS;
			float3 worldPosSOS;
			float3 worldReflSOS;
		};

		void vert(inout appdata_full v)
		{
			v.vertex = UnityObjectToClipPos(v.vertex);
			v.normal = UnityObjectToWorldNormal(v.normal);
			v.tangent = UnityObjectToWorldDir(v.tangent.xyz);
			v.binormal = UnityObjectToWorldDir(cross(v.normal, v.tangent) * v.tangent.w);
			v.texcoord.xy *= _BlurRadius;
		}

		float4 particleSpherePS(
			float2 texCoord : TEXCOORD0,
			float3 eyeSpacePos : TEXCOORD1,
			float sphereRadius : TEXCOORD2,
			float4 color : COLOR0) : SV_Target

			PSOutput particleSpherePS(PSInput input) {
			PSOutput output;

			float4 worldPos = mul(unity_CameraToWorld, float4(input.worldPos, 1.0));
			float4 screenPos = mul(ProjectionMatrix, worldPos);
			float2 texCoord = input.uv_MainTex;

			float2 wave = float2(
				sin(worldPos.x * _WaveSpeed + Time.time),
				cos(worldPos.z * _WaveSpeed + Time.time)
				);
			texCoord += wave * _WaveHeight;

			float4 color = tex2D(_MainTex, texCoord);

			float3 N;
			N.xy = texCoord * 2.0 - 1.0;
			float r2 = dot(N.xy, N.xy);
			if (r2 > 1.0) discard;
			N.z = -sqrt(1.0 - r2);

			float depth = tex2D(_DepthTex, texCoord).x;
			float3 posEye = uvToEye(texCoord, depth);
			float sphereRadius = length(mul(unity_WorldToCamera, float4(N * color.a * 0.1, 0.0)));

			float3 ddx = getEyePos(_DepthTex, texCoord + float2(texelSize, 0)) - posEye;
			float3 ddx2 = posEye - getEyePos(_DepthTex, texCoord + vec2(-texelSize, 0));
			if (abs(ddx.z) > abs(ddx2.z)) {
				ddx = ddx2;
			}
			float3 ddy = getEyePos(_DepthTex, texCoord + float2(0, texelSize)) - posEye;
			float3 ddy2 = posEye - getEyePos(_DepthTex, texCoord + vec2(0, -texelSize));
			if (abs(ddy.z) > abs(ddy2.z)) {
				ddy = ddy2;
			}

			float3 normal = normalize(cross(ddx, ddy));

			float3 viewDirection = normalize(posEye - _WorldSpaceCameraPos);
			float3 reflection = reflect(-viewDirection, normal);
			float reflectionFactor = saturate(dot(reflection, viewDirection));

			float refractionFactor = 1.0;
			if (_RefractionAmount > 0) {
				float3 refraction = refract(-viewDirection, normal, _RefractionIndex);
				refractionFactor = saturate(dot(refraction, viewDirection));
			}

			float4 finalColor = float4(0, 0, 0, 0);
			if (_ReflectionAmount > 0) {
				float4 reflectionColor = texCUBE(_ReflectionCube, reflection);
				finalColor += reflectionColor * _ReflectionAmount * reflectionFactor;
			}
			if (_RefractionAmount > 0) {
				float4 refractionColor = tex2D(_RefractionTex, (posEye.xy / posEye.z) * _RefractionScale);
				finalColor += refractionColor * _RefractionAmount * refractionFactor;
			}
			finalColor += tex2D(_MainTex, texCoord) * color;

			output.fragColor = finalColor;
			output.fragDepth = ComputeLinearDepth(posEye.z);
			return output;
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
	{
		// Calculate eye-space position and radius
		float depth = tex2D(_DepthTex, IN.uv_MainTex).x;
		float3 posEye = uvToEye(IN.uv_MainTex, depth);
		float sphereRadius = length(mul(unity_WorldToCamera, float4(N * _Color.a * 0.1, 0.0)));

		// Call particleSpherePS to render the particle sphere
		float4 color = _Color;
		particleSpherePS(o, IN.uv_MainTex, posEye, sphereRadius, color);
	}

	ENDCG