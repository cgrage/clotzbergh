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
                float2 uv : TEXCOORD0; // Using uv.x for the color and uv.y for the side
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
                o.normal = float3(0, 1, 0);

                uint color = (((uint)v.uv.x) >> 3) & 0x1F;
                /**/ if (color == 0) o.color = float4(1, 1, 1, 1); // White
                else if (color == 1) o.color = float4(0.5, 0.5, 0.5, 1); // Gray
                else if (color == 2) o.color = float4(0, 0, 0, 1); // Black
                else if (color == 3) o.color = float4(1, 0, 0, 1); // Red
                else if (color == 4) o.color = float4(0, 0, 1, 1); // Blue
                else if (color == 5) o.color = float4(1, 1, 0, 1); // Yellow
                else if (color == 6) o.color = float4(0, 1, 0, 1); // Green

                uint side = (((uint)v.uv.x) >> 0) & 0x7;
                /**/ if (side == 0) o.normal = float3(-1,  0,  0); // Left
                else if (side == 1) o.normal = float3( 1,  0,  0); // Right
                else if (side == 2) o.normal = float3( 0, -1,  0); // Bottom
                else if (side == 3) o.normal = float3( 0,  1,  0); // Top
                else if (side == 4) o.normal = float3( 0,  0, -1); // Back
                else if (side == 5) o.normal = float3( 0,  0,  1); // Front

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
