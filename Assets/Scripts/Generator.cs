using UnityEngine;

public class Generator : MonoBehaviour
{
    [ContextMenu(nameof(Generate))]
    public void Generate()
    {
        var sectionCount = 2; // Tube and cone
        var ringCount = sectionCount + 1; // One extra for the top of the cone
        var ringRadii = new float[] { 1f, 1f, 2f };
        var ringHeights = new float[] { 0f, 1f, 2f };
        var tubeSegments = 16;
        
        var mesh = new Mesh();
        var vertices = new Vector3[tubeSegments * ringCount * 2];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[tubeSegments * sectionCount * 6 * 2]; // 6 indices per quad (2 triangles)
        
        var angleStep = 360f / tubeSegments;
        
        var vertexIndex = 0;
        
        for (int sideIndex = 0; sideIndex < 2; sideIndex++)
        for (int i = 0; i < ringCount; i++)
        {
            var radius = ringRadii[i];
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
        
        var triangleIndex = 0;

        // Generate triangles with correct winding order for both sides in one pass without sharing vertices
        for (int i = 0; i < sectionCount; i++)
        {
            for (int j = 0; j < tubeSegments; j++)
            {
                var current = i * tubeSegments + j;
                var next = i * tubeSegments + (j + 1) % tubeSegments;
                var upperCurrent = (i + 1) * tubeSegments + j;
                var upperNext = (i + 1) * tubeSegments + (j + 1) % tubeSegments;

                // Front face triangle
                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = upperCurrent;
                triangles[triangleIndex++] = upperNext;

                // Front face triangle
                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = upperNext;
                triangles[triangleIndex++] = next;

                // Back face triangle
                triangles[triangleIndex++] = current + vertices.Length / 2;
                triangles[triangleIndex++] = upperNext + vertices.Length / 2;
                triangles[triangleIndex++] = upperCurrent + vertices.Length / 2;

                // Back face triangle
                triangles[triangleIndex++] = current + vertices.Length / 2;
                triangles[triangleIndex++] = next + vertices.Length / 2;
                triangles[triangleIndex++] = upperNext + vertices.Length / 2;
            }
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        
        var funnelGameObject = new GameObject("Funnel", typeof(MeshFilter), typeof(MeshRenderer));
        
        funnelGameObject.GetComponent<MeshFilter>().mesh = mesh;
        
        // Use urp lit shader
        var material = new Material(Shader.Find($"Universal Render Pipeline/Lit"));
        funnelGameObject.GetComponent<MeshRenderer>().material = material;
    }
}