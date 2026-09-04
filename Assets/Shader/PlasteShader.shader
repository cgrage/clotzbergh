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
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0; // Using uv.x for the color and uv.y for surface+variant
            };

            struct v2g
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float4 color : COLOR;
                int surface : TEXCOORD1;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float4 color : COLOR;
                int surface : TEXCOORD1;
            };

            float4 _MainLightColor;
            float4 _SpecColor;
            float _Glossiness;
            float _DoStudsAndHoles;

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
                if (color == 2) return HexToFloat4(0x101010); // Black
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
                if (color == 13) return HexToFloat4(0x404040); // Dark Gray

                return float4(0, 0, 0, 1); // Default
            }

            void AddVertex(inout TriangleStream<g2f> triStream, float3 pos, float3 normal, float4 color, int surface)
            {
                g2f o;
                o.pos = UnityObjectToClipPos(float4(pos, 1.0));
                o.normal = normal;
                o.color = color;
                o.surface = surface;
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
                    AddVertex(triStream, topCenter, a.normal, a.color, a.surface);
                    AddVertex(triStream, prevTopEdge, a.normal, a.color, a.surface);
                    AddVertex(triStream, newTopEdge, a.normal, a.color, a.surface);
                    triStream.RestartStrip();

                    // Side triangle 1 (previous top edge, new bottom edge, new top edge)
                    AddVertex(triStream, prevTopEdge,  normalize(cross(newBottomEdge - prevTopEdge, newTopEdge - prevTopEdge)), a.color, a.surface);
                    AddVertex(triStream, newBottomEdge, normalize(cross(newBottomEdge - prevTopEdge, newTopEdge - prevTopEdge)), a.color, a.surface);
                    AddVertex(triStream, newTopEdge, normalize(cross(newBottomEdge - prevTopEdge, newTopEdge - prevTopEdge)), a.color, a.surface);
                    triStream.RestartStrip();

                    // Side triangle 2 (previous top edge, previous bottom edge, new bottom edge)
                    AddVertex(triStream, prevTopEdge, normalize(cross(prevBottomEdge - prevTopEdge, newBottomEdge - prevTopEdge)), a.color, a.surface);
                    AddVertex(triStream, prevBottomEdge, normalize(cross(prevBottomEdge - prevTopEdge, newBottomEdge - prevTopEdge)), a.color, a.surface);
                    AddVertex(triStream, newBottomEdge, normalize(cross(prevBottomEdge - prevTopEdge, newBottomEdge - prevTopEdge)), a.color, a.surface);
                    triStream.RestartStrip();

                    prevTopEdge = newTopEdge;
                    prevBottomEdge = newBottomEdge;
                }
            }

            // Counterpart to AddStuds: a square indent, as deep as a stud is tall. Takes the same
            // triangle layout - b and c span the quad's diagonal, a is the opposite corner - so
            // each of the quad's two triangles contributes one half of the indent.
            void AddHoles(v2g a, v2g b, v2g c, inout TriangleStream<g2f> triStream)
            {
                float holeDepth = 0.0765f; // matches studHeight
                float wallThickness = 0.0675f; // 0.0015 * 45 = 0.0675

                float3 center = (b.pos.xyz + c.pos.xyz) / 2;
                float3 normal = a.normal;

                // The face is square, so its half width follows from the half diagonal. Scaling
                // the corners about the center by this keeps the indent square and centered.
                float halfDiagonal = length(b.pos.xyz - center);
                float halfWidth = halfDiagonal * 0.70710678f;
                float scale = max(halfWidth - wallThickness, 0.0) / halfWidth;

                float3 innerA = center + (a.pos.xyz - center) * scale;
                float3 innerB = center + (b.pos.xyz - center) * scale;
                float3 innerC = center + (c.pos.xyz - center) * scale;

                float3 deepA = innerA - normal * holeDepth;
                float3 deepB = innerB - normal * holeDepth;
                float3 deepC = innerC - normal * holeDepth;

                // The rim left standing around the opening, in the original face plane.
                AddVertex(triStream, b.pos.xyz, normal, a.color, a.surface);
                AddVertex(triStream, innerB, normal, a.color, a.surface);
                AddVertex(triStream, a.pos.xyz, normal, a.color, a.surface);
                AddVertex(triStream, innerA, normal, a.color, a.surface);
                AddVertex(triStream, c.pos.xyz, normal, a.color, a.surface);
                AddVertex(triStream, innerC, normal, a.color, a.surface);
                triStream.RestartStrip();

                // Side walls, facing inwards so they are visible from outside the cavity.
                float3 wallNormalBA = normalize(center - (innerB + innerA) / 2);
                AddVertex(triStream, innerB, wallNormalBA, a.color, a.surface);
                AddVertex(triStream, deepB, wallNormalBA, a.color, a.surface);
                AddVertex(triStream, innerA, wallNormalBA, a.color, a.surface);
                AddVertex(triStream, deepA, wallNormalBA, a.color, a.surface);
                triStream.RestartStrip();

                float3 wallNormalAC = normalize(center - (innerA + innerC) / 2);
                AddVertex(triStream, innerA, wallNormalAC, a.color, a.surface);
                AddVertex(triStream, deepA, wallNormalAC, a.color, a.surface);
                AddVertex(triStream, innerC, wallNormalAC, a.color, a.surface);
                AddVertex(triStream, deepC, wallNormalAC, a.color, a.surface);
                triStream.RestartStrip();

                // Floor of the cavity.
                AddVertex(triStream, deepA, normal, a.color, a.surface);
                AddVertex(triStream, deepB, normal, a.color, a.surface);
                AddVertex(triStream, deepC, normal, a.color, a.surface);
                triStream.RestartStrip();
            }

            v2g vert(appdata v)
            {
                uint colorEnum = ((uint)v.uv.x) & 0x1F;
                uint variant = ((uint)v.uv.y) & 0x7F; // numbers are from 0 to 127
                uint surface = ((uint)v.uv.y >> 7) & 0xF; // KlotzSurfaceFeature: 0=Default, 1=HasStuds, 2=HasHoles, 3=IsRough

                float4 baseColor = GetColor(colorEnum);
                float variation = variant / 127.0;

                v2g o;
                o.pos = v.vertex;
                o.normal = v.normal;
                o.color = baseColor * (1.0 - variation * 0.1); // Vary color by up to 10%
                o.surface = surface;
                return o;
            }

            [maxvertexcount(3 + 8 * 9)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                int doStudsAndHoles = (_DoStudsAndHoles > 0.5) ? 1 : 0;
                int addStuds = (doStudsAndHoles && input[0].surface == 1) ? 1 : 0;
                int addHoles = (doStudsAndHoles && input[0].surface == 2) ? 1 : 0;

                // A hole replaces the face with a rim around its opening, so the original triangle
                // is only passed through when there is none.
                if (!addHoles)
                {
                    for (int i = 0; i < 3; ++i)
                    {
                        AddVertex(triStream,
                            input[i].pos,
                            input[i].normal,
                            input[i].color,
                            input[i].surface);
                    }

                    triStream.RestartStrip();
                }

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
                    else AddHoles(input[2], input[0], input[1], triStream);
                }
                else if (side2LengthSq >= side1LengthSq && side2LengthSq >= side3LengthSq)
                {
                    if (addStuds) AddStuds(input[0], input[1], input[2], triStream);
                    else AddHoles(input[0], input[1], input[2], triStream);
                }
                else
                {
                    if (addStuds) AddStuds(input[1], input[2], input[0], triStream);
                    else AddHoles(input[1], input[2], input[0], triStream);
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

                // Specular highlights (rough surfaces get no highlight, giving them a matte look)
                float isRough = (i.surface == 3) ? 1.0 : 0.0; // KlotzSurfaceFeature.IsRough
                float spec = pow(max(dot(normal, halfDir), 0.0), _Glossiness * 256.0) * (1.0 - isRough);

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
