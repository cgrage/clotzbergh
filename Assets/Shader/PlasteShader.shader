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
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0; // Using uv.x for color+side and uv.y for the variant
            };

            struct v2g
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float4 color : COLOR;
                int addStuds : TEXCOORD1;
            };

            struct g2f
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

                return float3(0, 0, 0); // Default
            }

            float4 GetColor(uint color)
            {
                if (color == 0) return float4(1, 1, 1, 1);       // White
                if (color == 1) return float4(0.5, 0.5, 0.5, 1); // Gray
                if (color == 2) return float4(0, 0, 0, 1);       // Black
                if (color == 3) return float4(1, 0, 0, 1);       // Red
                if (color == 4) return float4(0, 0, 1, 1);       // Blue
                if (color == 5) return float4(1, 1, 0, 1);       // Yellow
                if (color == 6) return float4(0, 1, 0, 1);       // Green
                if (color == 7) return float4(0, 0.5, 1, 1);     // Azure
                if (color == 8) return float4(1, 0.5, 0, 1);     // Orange
                if (color == 9) return float4(0, 0, 0.5, 1);     // Dark Blue

                return float4(0, 0, 0, 1); // Default
            }

            v2g vert(appdata v)
            {
                uint side = ((uint)v.uv.x) & 0x7;
                uint colorEnum = ((uint)v.uv.x >> 3) & 0x1F;
                uint variant = ((uint)v.uv.y) & 0x7F; // numbers are from 0 to 127
                uint vertexFlags = ((uint)v.uv.y >> 7) & 0xF;

                float4 baseColor = GetColor(colorEnum);
                float variation = variant / 127.0;

                v2g o;
                o.pos = v.vertex;
                o.normal = GetNormal(side);
                o.color = baseColor * (1.0 - variation * 0.1); // Vary color by up to 10%
                o.addStuds = (vertexFlags > 0) ? 1 : 0;
                return o;
            }

            [maxvertexcount(3 + 8 * 9)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                int addStuds = 0;

                // Pass through original triangle
                for (int i = 0; i < 3; ++i)
                {
                    g2f o;
                    o.pos = UnityObjectToClipPos(input[i].pos);
                    o.normal = input[i].normal;
                    o.color = input[i].color;
                    addStuds += input[i].addStuds;
                    triStream.Append(o);
                }

                triStream.RestartStrip();

                if (addStuds == 0)
                    return;

                float studRadius = 0.108f; // 0.0024 * 45 = 0.108
                float studHeight = 0.0765f; // 0.0017 * 45 = 0.0765

                // Calculate the longest side to determine the base vertices
                int a, b, c;

                float4 side1 = input[0].pos - input[1].pos;
                float4 side2 = input[1].pos - input[2].pos;
                float4 side3 = input[2].pos - input[0].pos;

                float side1LengthSq = dot(side1, side1);
                float side2LengthSq = dot(side2, side2);
                float side3LengthSq = dot(side3, side3);

                if (side1LengthSq >= side2LengthSq && side1LengthSq >= side3LengthSq)
                {
                    a = 2, b = 0, c = 1;
                }
                else if (side2LengthSq >= side1LengthSq && side2LengthSq >= side3LengthSq)
                {
                    a = 0, b = 1, c = 2;
                }
                else
                {
                    a = 1, b = 2, c = 0;
                }

                float3 bottomCenter = (input[b].pos.xyz + input[c].pos.xyz) / 2;
                float3 normal = input[a].normal;
                float4 color = input[0].color; // all verts have same color

                // Generate orthogonal basis
                float3 tangent = normalize(input[b].pos.xyz - bottomCenter);
                float3 bitangent = normalize(cross(normal, tangent));

                float3 topCenter = bottomCenter + normal * studHeight;
                int segments = 8;
                float angleStep = radians(180.0 / segments); // Angle step for each segment
                float angle = 0;
                float3 prevTopEdge = topCenter + studRadius * (cos(angle) * tangent + sin(angle) * bitangent);
                float3 prevBottomEdge = bottomCenter + studRadius * (cos(angle) * tangent + sin(angle) * bitangent);

                for (int i = 0; i < segments; i++)
                {
                    angle += angleStep;
                    float3 newTopEdge = topCenter + studRadius * (cos(angle) * tangent + sin(angle) * bitangent);
                    float3 newBottomEdge = bottomCenter + studRadius * (cos(angle) * tangent + sin(angle) * bitangent);
                    g2f o;

                    // Top triangle (top-center, previous top edge, new top edge)
                    o.pos = UnityObjectToClipPos(float4(topCenter, 1.0));
                    o.normal = normal;
                    o.color = color;
                    triStream.Append(o);

                    o.pos = UnityObjectToClipPos(float4(prevTopEdge, 1.0));
                    o.normal = normal;
                    o.color = color;
                    triStream.Append(o);

                    o.pos = UnityObjectToClipPos(float4(newTopEdge, 1.0));
                    o.normal = normal;
                    o.color = color;
                    triStream.Append(o);

                    triStream.RestartStrip();

                    // Side triangle 1 (previous top edge, new bottom edge, new top edge)
                    o.pos = UnityObjectToClipPos(float4(prevTopEdge, 1.0));
                    o.normal = normalize(cross(newBottomEdge - prevTopEdge, newTopEdge - prevTopEdge));
                    o.color = color;
                    triStream.Append(o);

                    o.pos = UnityObjectToClipPos(float4(newBottomEdge, 1.0));
                    o.normal = normalize(cross(newBottomEdge - prevTopEdge, newTopEdge - prevTopEdge));
                    o.color = color;
                    triStream.Append(o);

                    o.pos = UnityObjectToClipPos(float4(newTopEdge, 1.0));
                    o.normal = normalize(cross(newBottomEdge - prevTopEdge, newTopEdge - prevTopEdge));
                    o.color = color;
                    triStream.Append(o);

                    triStream.RestartStrip();

                    // Side triangle 2 (previous top edge, previous bottom edge, new bottom edge)
                    o.pos = UnityObjectToClipPos(float4(prevTopEdge, 1.0));
                    o.normal = normalize(cross(prevBottomEdge - prevTopEdge, newBottomEdge - prevTopEdge));
                    o.color = color;
                    triStream.Append(o);

                    o.pos = UnityObjectToClipPos(float4(prevBottomEdge, 1.0));
                    o.normal = normalize(cross(prevBottomEdge - prevTopEdge, newBottomEdge - prevTopEdge));
                    o.color = color;
                    triStream.Append(o);

                    o.pos = UnityObjectToClipPos(float4(newBottomEdge, 1.0));
                    o.normal = normalize(cross(prevBottomEdge - prevTopEdge, newBottomEdge - prevTopEdge));
                    o.color = color;
                    triStream.Append(o);

                    triStream.RestartStrip();

                    prevTopEdge = newTopEdge;
                    prevBottomEdge = newBottomEdge;
                }
            }

            half4 frag(g2f i) : SV_Target
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
