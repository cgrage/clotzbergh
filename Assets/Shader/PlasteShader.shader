Shader "PlasteShader"
{
    Properties
    {
        _MainLightColor ("Main Light Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0; // Using uv.x for color+side and uv.y for the variant
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _MainLightColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // Determine normal based on the side (least significant 3 bits)
                uint side = ((uint)v.uv.x) & 0x7;
                /**/ if (side == 0) o.normal = float3(-1, 0, 0);  // Left
                else if (side == 1) o.normal = float3(1, 0, 0); // Right
                else if (side == 2) o.normal = float3(0, -1, 0);  // Bottom
                else if (side == 3) o.normal = float3(0, 1, 0); // Top
                else if (side == 4) o.normal = float3(0, 0, -1); // Back
                else if (side == 5) o.normal = float3(0, 0, 1); // Front
                else o.normal = float3(0, 0, 0); // Default, if no valid side is found

                // Determine base color based on the higher bits (5 bits above)
                uint colorEnum = ((uint)v.uv.x >> 3) & 0x1F;
                float4 baseColor;

                /**/ if (colorEnum == 0) baseColor = float4(1, 1, 1, 1); // White
                else if (colorEnum == 1) baseColor = float4(0.5, 0.5, 0.5, 1); // Grey
                else if (colorEnum == 2) baseColor = float4(0, 0, 0, 1); // Black
                else if (colorEnum == 3) baseColor = float4(1, 0, 0, 1); // Red
                else if (colorEnum == 4) baseColor = float4(0, 0, 1, 1); // Blue
                else if (colorEnum == 5) baseColor = float4(1, 1, 0, 1); // Yellow
                else if (colorEnum == 6) baseColor = float4(0, 1, 0, 1); // Green
                else if (colorEnum == 7) baseColor = float4(0, 0.5, 1, 1); // Azure
                else if (colorEnum == 8) baseColor = float4(1, 0.5, 0, 1); // Orange
                else  baseColor = float4(1, 1, 1, 1);  // Default to white

                // Apply color variation based on variant (0 to 127)
                uint variant = (uint)v.uv.y; // numbers are from 0 to 127
                float variation = variant / 127.0;

                // Create a slight color variation
                o.color = baseColor * (1.0 - variation * 0.1); // Vary color by up to 10%

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 normal = normalize(i.normal);
                float diffuse = max(dot(normal, lightDir), 0.0);

                float3 ambient = 0.1 * i.color.rgb;
                float3 diffuseColor = i.color.rgb * _MainLightColor.rgb * diffuse;

                return half4(ambient + diffuseColor, i.color.a);
            }
            ENDCG
        }
    }
}
