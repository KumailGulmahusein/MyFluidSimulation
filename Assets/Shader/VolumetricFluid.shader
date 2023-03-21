Shader "Custom/VolumetricFluid" {
    Properties{
        _FluidColor("Fluid Color", Color) = (1, 1, 1, 1)
        _FluidOffset("Fluid Offset", Range(-1, 1)) = 0
        _FluidSize("Fluid Size", Range(0, 1)) = 0.1
        _FluidDensity("Fluid Density", Range(0, 1)) = 0.2
        _FluidSpeed("Fluid Speed", Range(0, 1)) = 0.5
    }

        SubShader{
            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                // Define the shader properties
                sampler2D _CameraTexture;
                sampler2D _CameraDepthTexture;
                float4 _FluidColor;
                float4 _FluidOffset;
                float _FluidSize;
                float _FluidDensity;
                float _FluidSpeed;
                sampler3D _FluidTexture;

                // Define the vertex shader
                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float2 uv : TEXCOORD0;
                };

                struct v2f {
                    float2 uv : TEXCOORD0;
                    float4 worldPos : TEXCOORD1;
                    float4 screenPos : TEXCOORD2;
                    float3 worldNormal : TEXCOORD3;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                v2f vert(appdata v, uint id : SV_InstanceID) {
                    v2f o;
                    o.uv = v.uv;
                    o.worldPos = mul(unity_ObjectToWorld[id], v.vertex);
                    o.screenPos = UnityObjectToClipPos(v.vertex);
                    o.worldNormal = mul(unity_ObjectToWorld[id], v.normal);
                    UNITY_SETUP_INSTANCE_ID(o);
                    return o;
                }
                fixed4 frag(v2f i) : SV_Target{
                    // Sample the color from the camera texture
                    fixed4 col = tex2D(_CameraTexture, i.uv);

                // Calculate the fluid position
                float3 fluidPos = i.worldPos + _FluidOffset.xyz;

                // Calculate the distance from the fluid surface
                float dist = length(fluidPos - _WorldSpaceCameraPos) - _FluidSize;

                // Calculate the fluid density
                float density = saturate(dist / _FluidDensity);

                // Calculate the fluid speed
                float speed = saturate(dist / _FluidSpeed);

                // Sample the texture
                fixed4 finalColor = tex3D(_FluidTexture, i.worldPos.xyz);

                // Blend the texture with the final color
                fixed4 texColor = tex3D(_FluidTexture, i.worldPos.xyz);

                // Calculate the final color
                col.rgb = lerp(col.rgb, _FluidColor.rgb, density);
                col.a = lerp(col.a, speed, density);

                // Apply bilateral filter
                float4 filteredColor = fixed4(0, 0, 0, 0);
                float totalWeight = 0.0;
                float depth = tex2D(_CameraDepthTexture, i.uv).r;
                for (int y = -5; y <= 5; y++) {
                    for (int x = -5; x <= 5; x++) {
                        float4 sampleColor = tex2D(_CameraTexture, i.uv + float2(x, y) * 0.004);
                        float sampleDepth = tex2D(_CameraDepthTexture, i.uv + float2(x, y) * 0.004).r;
                        float weight = exp(-(length(sampleColor.rgb - col.rgb) * 50.0 + length(sampleDepth - depth) * 500.0));
                        filteredColor += sampleColor * weight;
                        totalWeight += weight;
                    }
                }
                col.rgb = filteredColor.rgb / totalWeight;

                // Return the final color
                return col;
            }
            ENDCG
        }
    }
        FallBack "Diffuse"
}