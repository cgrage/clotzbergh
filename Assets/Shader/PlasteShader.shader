Shader "PlasteShader"
{
    Properties
    {
        _MainLightColor ("Main Light Color", Color) = (1, 1, 1, 1)
        _SpecColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Glossiness ("Glossiness", Range(0, 1)) = 1
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
                int vertexFlags : TEXCOORD1;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _MainLightColor;
            float4 _SpecColor;
            float _Glossiness;

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

            float4 HexToFloat4(uint hexValue)
            {
                float r = ((hexValue >> 16) & 0xFF) / 255.0;
                float g = ((hexValue >> 8) & 0xFF) / 255.0;
                float b = (hexValue & 0xFF) / 255.0;
                return float4(r, g, b, 1);
            }

            float4 GetColor(uint color)
            {
                if (color == 0) return HexToFloat4(0xFFFFFF); // White
                if (color == 1) return HexToFloat4(0x808080); // Gray
                if (color == 2) return HexToFloat4(0x000000); // Black
                if (color == 3) return HexToFloat4(0xFF0000); // Red
                if (color == 4) return HexToFloat4(0x0000FF); // Blue
                if (color == 5) return HexToFloat4(0xFFFF00); // Yellow
                if (color == 6) return HexToFloat4(0x00FF00); // Green
                if (color == 7) return HexToFloat4(0x007FFF); // Azure
                if (color == 8) return HexToFloat4(0xFF7F00); // Orange
                if (color == 9) return HexToFloat4(0x000080); // Dark Blue
                if (color == 10) return HexToFloat4(0x996633); // Brown
                if (color == 11) return HexToFloat4(0x251101); // Dark Brown
                if (color == 12) return HexToFloat4(0x006400); // Dark Green

                return float4(0, 0, 0, 1); // Default
            }

            void AddVertex(inout TriangleStream<g2f> triStream, float3 pos, float3 normal, float4 color)
            {
                g2f o;
                o.pos = UnityObjectToClipPos(float4(pos, 1.0));
                o.normal = normal;
                o.color = color;
                triStream.Append(o);
            }

            void AddStuds(v2g a, v2g b, v2g c, inout TriangleStream<g2f> triStream)
            {
                float studRadius = 0.108f; // 0.0024 * 45 = 0.108
                float studHeight = 0.0765f; // 0.0017 * 45 = 0.0765
                int studSegments = 8;

                // Generate orthogonal basis
                float3 bottomCenter = (b.pos.xyz + c.pos.xyz) / 2;
                float3 tangent = normalize(b.pos.xyz - bottomCenter);
                float3 bitangent = normalize(cross(a.normal, tangent));

                float3 topCenter = bottomCenter + a.normal * studHeight;
                float angleStep = radians(180.0 / studSegments); // Angle step for each segment

                float angle = 0;
                float3 prevTopEdge = topCenter + studRadius * (cos(angle) * tangent + sin(angle) * bitangent);
                float3 prevBottomEdge = bottomCenter + studRadius * (cos(angle) * tangent + sin(angle) * bitangent);

                for (int i = 0; i < studSegments; i++)
                {
                    angle += angleStep;
                    float3 newTopEdge = topCenter + studRadius * (cos(angle) * tangent + sin(angle) * bitangent);
                    float3 newBottomEdge = bottomCenter + studRadius * (cos(angle) * tangent + sin(angle) * bitangent);
                    g2f o;

                    // Top triangle (top-center, previous top edge, new top edge)
                    AddVertex(triStream, topCenter, a.normal, a.color);
                    AddVertex(triStream, prevTopEdge, a.normal, a.color);
                    AddVertex(triStream, newTopEdge, a.normal, a.color);
                    triStream.RestartStrip();

                    // Side triangle 1 (previous top edge, new bottom edge, new top edge)
                    AddVertex(triStream, prevTopEdge,  normalize(cross(newBottomEdge - prevTopEdge, newTopEdge - prevTopEdge)), a.color);
                    AddVertex(triStream, newBottomEdge, normalize(cross(newBottomEdge - prevTopEdge, newTopEdge - prevTopEdge)), a.color);
                    AddVertex(triStream, newTopEdge, normalize(cross(newBottomEdge - prevTopEdge, newTopEdge - prevTopEdge)), a.color);
                    triStream.RestartStrip();

                    // Side triangle 2 (previous top edge, previous bottom edge, new bottom edge)
                    AddVertex(triStream, prevTopEdge, normalize(cross(prevBottomEdge - prevTopEdge, newBottomEdge - prevTopEdge)), a.color);
                    AddVertex(triStream, prevBottomEdge, normalize(cross(prevBottomEdge - prevTopEdge, newBottomEdge - prevTopEdge)), a.color);
                    AddVertex(triStream, newBottomEdge, normalize(cross(prevBottomEdge - prevTopEdge, newBottomEdge - prevTopEdge)), a.color);
                    triStream.RestartStrip();

                    prevTopEdge = newTopEdge;
                    prevBottomEdge = newBottomEdge;
                }
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
                o.color = baseColor * (1.0 - variation * 0.2); // Vary color by up to 20%
                o.vertexFlags = vertexFlags;
                return o;
            }

            [maxvertexcount(3 + 8 * 9)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                int addStuds = (input[0].vertexFlags & 0x1) >> 0;
                int addHoles = (input[0].vertexFlags & 0x2) >> 1;

                // if (!addHoles)
                {
                    // Pass through original triangle
                    for (int i = 0; i < 3; ++i)
                    {
                        AddVertex(triStream, 
                            input[i].pos, 
                            input[i].normal, 
                            input[i].color);
                    }
                }

                triStream.RestartStrip();
                if (!addStuds && !addHoles)
                    return;

                float4 side1 = input[0].pos - input[1].pos;
                float4 side2 = input[1].pos - input[2].pos;
                float4 side3 = input[2].pos - input[0].pos;

                float side1LengthSq = dot(side1, side1);
                float side2LengthSq = dot(side2, side2);
                float side3LengthSq = dot(side3, side3);

                if (side1LengthSq >= side2LengthSq && side1LengthSq >= side3LengthSq)
                {
                    if (addStuds) AddStuds(input[2], input[0], input[1], triStream);
                    // else AddStuds(input[2], input[0], input[1], triStream);
                }
                else if (side2LengthSq >= side1LengthSq && side2LengthSq >= side3LengthSq)
                {
                    if (addStuds) AddStuds(input[0], input[1], input[2], triStream);
                    // else AddStuds(input[0], input[1], input[2], triStream);
                }
                else
                {
                    if (addStuds) AddStuds(input[1], input[2], input[0], triStream);
                    // else AddStuds(input[1], input[2], input[0], triStream);
                }
            }

            half4 frag(g2f i) : SV_Target
            {
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 normal = normalize(i.normal);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.pos.xyz);
                float3 halfDir = normalize(lightDir + viewDir);

                // Diffuse lighting
                float diffuse = max(dot(normal, lightDir), 0.0);
                
                // Specular highlights
                float spec = pow(max(dot(normal, halfDir), 0.0), _Glossiness * 256.0);

                // Combine results
                float3 ambient = 0.05 * i.color.rgb;
                float3 diffuseColor = i.color.rgb * _MainLightColor.rgb * diffuse;
                float3 specularColor = _SpecColor.rgb * spec;

                return half4(ambient + diffuseColor + specularColor, i.color.a);
            }

            ENDCG
        }
    }
}
