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

            float3 GetNormal(uint side)
            {
                if (side == 0) return float3(-1, 0, 0); // Left
                if (side == 1) return float3(1, 0, 0);  // Right
                if (side == 2) return float3(0, -1, 0); // Bottom
                if (side == 3) return float3(0, 1, 0);  // Top
                if (side == 4) return float3(0, 0, -1); // Back
                if (side == 5) return float3(0, 0, 1);  // Front

                return float3(0, 0, 0);
            }

            float4 GetColor(uint color)
            {
                if (color == 0) return float4(1, 1, 1, 1);       // White
                if (color == 1) return float4(0.5, 0.5, 0.5, 1); // Grey
                if (color == 2) return float4(0, 0, 0, 1);       // Black
                if (color == 3) return float4(1, 0, 0, 1);       // Red
                if (color == 4) return float4(0, 0, 1, 1);       // Blue
                if (color == 5) return float4(1, 1, 0, 1);       // Yellow
                if (color == 6) return float4(0, 1, 0, 1);       // Green
                if (color == 7) return float4(0, 0.5, 1, 1);     // Azure
                if (color == 8) return float4(1, 0.5, 0, 1);     // Orange
                if (color == 9) return float4(0, 0, 0.5, 1);     // Dark Blue

                return float4(0, 0, 0, 1);
            }

            v2f vert(appdata v)
            {
                uint side = ((uint)v.uv.x) & 0x7;
                uint colorEnum = ((uint)v.uv.x >> 3) & 0x1F;
                uint variant = (uint)v.uv.y; // numbers are from 0 to 127

                float4 baseColor = GetColor(colorEnum);
                float variation = variant / 127.0;

                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = GetNormal(side);
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
