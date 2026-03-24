using System;
using UnityEngine;

public class Generator : MonoBehaviour
{
    public float TopRimDiameter = 2f;
    public float LowerTubeDiameter = 1f;
    public float SlopingSidesVerticalHeight = 2f;
    public float TubeVerticalHeight = 1f;
    public GameObject GeneratedObject;

    private void OnValidate()
    {
        Generate();
    }

    [ContextMenu(nameof(Generate))]
    public void Generate()
    {
        var sectionCount = 2; // Tube and cone
        var ringCount = sectionCount + 1; // One extra for the top of the cone
        var ringRadii = new float[] { LowerTubeDiameter/2f, LowerTubeDiameter/2f, TopRimDiameter/2f };
        var ringHeights = new float[] { 0f, TubeVerticalHeight, TubeVerticalHeight + SlopingSidesVerticalHeight };
        var tubeSegments = 16;
        var thickness = 0.1f;
        
        var mesh = new Mesh();
        var vertexCount = tubeSegments * ringCount * 2; // 2 for front and back faces without sharing vertices
        vertexCount += tubeSegments * 2 * 2; // Add vertices for the top and bottom of the object
        var vertices = new Vector3[vertexCount];
        var uvs = new Vector2[vertices.Length];
        var triangleCount = tubeSegments * sectionCount * 2 * 3; // 2 triangles per quad, 3 indices per triangle
        triangleCount += tubeSegments * 2 * 3 * 2*2; // Add quads for the top and bottom of the object
        var triangles = new int[triangleCount]; // 6 indices per quad (2 triangles)
        
        var angleStep = 360f / tubeSegments;
        
        var vertexIndex = 0;

        for (int sideIndex = 0; sideIndex < 2; sideIndex++)
        {
            var thicknessOffset = sideIndex == 0 ? thickness/2f : -thickness/2f;
            
            for (int i = 0; i < ringCount; i++)
            {
                var radius = ringRadii[i] + thicknessOffset;
                var y = ringHeights[i];

                for (int j = 0; j < tubeSegments; j++)
                {
                    var angle = j * angleStep * Mathf.Deg2Rad;
                    var x = Mathf.Cos(angle) * radius;
                    var z = Mathf.Sin(angle) * radius;
                    var vertex = new Vector3(x, y, z);

                    vertices[vertexIndex] = vertex;
                    uvs[vertexIndex] = new Vector2((float)j / tubeSegments, (float)i / sectionCount);
                    vertexIndex++;
                }
            }
        }

        var triangleIndex = 0;

        // Generate triangles with correct winding order for both sides in one pass without sharing vertices

        for (int sideIndex = 0; sideIndex < 2; sideIndex++)
        {
            var isInnerFace = sideIndex == 1;
            
            for (int i = 0; i < sectionCount; i++)
            {
                for (int j = 0; j < tubeSegments; j++)
                {
                    var sideOffset = sideIndex * tubeSegments * ringCount;
                    var ringOffset = i * tubeSegments;
                    
                    var current = sideOffset + ringOffset + j;
                    var upperCurrent = sideOffset + (i + 1) * tubeSegments + j;
                    var next = sideOffset + ringOffset + (j + 1) % tubeSegments;
                    var upperNext = sideOffset + (i + 1) * tubeSegments + (j + 1) % tubeSegments;
                    
                    if (!isInnerFace)
                    {
                        triangles[triangleIndex++] = current;
                        triangles[triangleIndex++] = upperCurrent;
                        triangles[triangleIndex++] = next;

                        triangles[triangleIndex++] = next;
                        triangles[triangleIndex++] = upperCurrent;
                        triangles[triangleIndex++] = upperNext;
                    }
                    else
                    {
                        triangles[triangleIndex++] = current;
                        triangles[triangleIndex++] = next;
                        triangles[triangleIndex++] = upperCurrent;

                        triangles[triangleIndex++] = next;
                        triangles[triangleIndex++] = upperNext;
                        triangles[triangleIndex++] = upperCurrent;
                    }
                }
            }
        }

        // close the top and bottom of the object
        var capVertexOffset = tubeSegments * ringCount * 2; // Offset for the top and bottom vertices
        
        var bottomVertexOffset = vertexIndex; // Start of the bottom cap
        var bottomOuterOffset = bottomVertexOffset;
        var bottomInnerOffset = bottomVertexOffset + tubeSegments;
        
        // Add vertices for the bottom
        for (int sideIndex = 0; sideIndex < 2; sideIndex++)
        {
            var thicknessOffset = sideIndex == 0 ? thickness/2f : -thickness/2f;
            var radius = ringRadii[0] + thicknessOffset;
            var y = ringHeights[0];

            for (int j = 0; j < tubeSegments; j++)
            {
                var angle = j * angleStep * Mathf.Deg2Rad;
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                var vertex = new Vector3(x, y, z);

                vertices[vertexIndex] = vertex;
                uvs[vertexIndex] = new Vector2((float)j / tubeSegments, 1f);
                vertexIndex++;
            }
        }
        
        // Add triangles for the bottom
        for (int j = 0; j < tubeSegments; j++)
        {
            var outerCurrent = bottomOuterOffset + j;
            var outerNext = bottomOuterOffset + (j + 1) % tubeSegments;
            var innerCurrent = bottomInnerOffset + j;
            var innerNext = bottomInnerOffset + (j + 1) % tubeSegments;
            
            triangles[triangleIndex++] = innerCurrent;
            triangles[triangleIndex++] = outerCurrent;
            triangles[triangleIndex++] = innerNext;
            
            triangles[triangleIndex++] = innerNext;
            triangles[triangleIndex++] = outerCurrent;
            triangles[triangleIndex++] = outerNext;
        }
        
        var topVertexOffset = vertexIndex; // Start of the top cap
        var topOuterOffset = topVertexOffset;
        var topInnerOffset = topVertexOffset + tubeSegments;
        
        // Add vertices for the top
        for (int sideIndex = 0; sideIndex < 2; sideIndex++)
        {
            var thicknessOffset = sideIndex == 0 ? thickness/2f : -thickness/2f;
            var radius = ringRadii[^1] + thicknessOffset;
            var y = ringHeights[^1];

            for (int j = 0; j < tubeSegments; j++)
            {
                var angle = j * angleStep * Mathf.Deg2Rad;
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                var vertex = new Vector3(x, y, z);

                vertices[vertexIndex] = vertex;
                uvs[vertexIndex] = new Vector2((float)j / tubeSegments, 1f);
                vertexIndex++;
            }
        }
        
        // Add triangles for the top
        for (int j = 0; j < tubeSegments; j++)
        {
            var outerCurrent = topOuterOffset + j;
            var outerNext = topOuterOffset + (j + 1) % tubeSegments;
            var innerCurrent = topInnerOffset + j;
            var innerNext = topInnerOffset + (j + 1) % tubeSegments;
            
            triangles[triangleIndex++] = innerCurrent;
            triangles[triangleIndex++] = innerNext;
            triangles[triangleIndex++] = outerCurrent;
            
            triangles[triangleIndex++] = innerNext;
            triangles[triangleIndex++] = outerNext;
            triangles[triangleIndex++] = outerCurrent;
        }
        
        

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        
        if (!GeneratedObject)
        {
            GeneratedObject= new GameObject("Funnel", typeof(MeshFilter), typeof(MeshRenderer));
        }
        
        GeneratedObject.GetComponent<MeshFilter>().mesh = mesh;
        
        // Use urp lit shader
        var material = new Material(Shader.Find($"Universal Render Pipeline/Lit"));
        GeneratedObject.GetComponent<MeshRenderer>().material = material;
    }
}