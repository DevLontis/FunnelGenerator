using System;
using UnityEngine;
using UnityEngine.UI;

public class Generator : MonoBehaviour
{
    [SerializeField] private InputField TopRimDiameterInputField;
    [SerializeField] private InputField LowerTubeDiameterInputField;
    [SerializeField] private InputField SlopingSidesVerticalHeightInputField;
    [SerializeField] private InputField TubeVerticalHeightInputField;
    [SerializeField] private Material _material;
    
    private GameObject _generatedObject;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    
    private float _topRimDiameter = 2f;
    private float _lowerTubeDiameter = 1f;
    private float _slopingSidesVerticalHeight = 2f;
    private float _tubeVerticalHeight = 1f;
    private Vector3[] _vertices;
    private Vector2[] _uvs;
    private int[] _triangles;
    private Mesh _mesh;

    private void Awake()
    {
        var sectionCount = 2; // Tube and cone
        var ringCount = sectionCount + 1; // One extra for the top of the cone
        var tubeSegments = 16;
        var vertexCount = tubeSegments * ringCount * 2; // 2 for front and back faces without sharing vertices
        vertexCount += tubeSegments * 2 * 2; // Add vertices for the top and bottom of the object
        var triangleCount = tubeSegments * sectionCount * 2 * 3; // 2 triangles per quad, 3 indices per triangle
        triangleCount += tubeSegments * 2 * 3 * 2*2; // Add quads for the top and bottom of the object
        
        _vertices = new Vector3[vertexCount];
        _uvs = new Vector2[_vertices.Length];
        _triangles = new int[triangleCount]; // 6 indices per quad (2 triangles)
        
        _mesh = new Mesh
        {
            name = "Procedural Funnel"
        };
    }

    private void Start()
    {
        TopRimDiameterInputField.text = _topRimDiameter.ToString();
        LowerTubeDiameterInputField.text = _lowerTubeDiameter.ToString();
        SlopingSidesVerticalHeightInputField.text = _slopingSidesVerticalHeight.ToString();
        TubeVerticalHeightInputField.text = _tubeVerticalHeight.ToString();
        
        TopRimDiameterInputField.onValueChanged.AddListener(value =>
        {
            if (float.TryParse(value, out var result))
            {
                _topRimDiameter = result;
                Generate();
            }
        });
        
        LowerTubeDiameterInputField.onValueChanged.AddListener(value =>
        {
            if (float.TryParse(value, out var result))
            {
                _lowerTubeDiameter = result;
                Generate();
            }
        });
        
        SlopingSidesVerticalHeightInputField.onValueChanged.AddListener(value =>
        {
            if (float.TryParse(value, out var result))
            {
                _slopingSidesVerticalHeight = result;
                Generate();
            }
        });
        
        TubeVerticalHeightInputField.onValueChanged.AddListener(value =>
        {
            if (float.TryParse(value, out var result))
            {
                _tubeVerticalHeight = result;
                Generate();
            }
        });
    }

    [ContextMenu(nameof(Generate))]
    public void Generate()
    {
        var sectionCount = 2; // Tube and cone
        var ringCount = sectionCount + 1; // One extra for the top of the cone
        var ringRadii = new float[] { _lowerTubeDiameter/2f, _lowerTubeDiameter/2f, _topRimDiameter/2f };
        var ringHeights = new float[] { 0f, _tubeVerticalHeight, _tubeVerticalHeight + _slopingSidesVerticalHeight };
        var tubeSegments = 16;
        var thickness = 0.1f;
        
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

                    _vertices[vertexIndex] = vertex;
                    _uvs[vertexIndex] = new Vector2((float)j / tubeSegments, (float)i / sectionCount);
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
                        _triangles[triangleIndex++] = current;
                        _triangles[triangleIndex++] = upperCurrent;
                        _triangles[triangleIndex++] = next;

                        _triangles[triangleIndex++] = next;
                        _triangles[triangleIndex++] = upperCurrent;
                        _triangles[triangleIndex++] = upperNext;
                    }
                    else
                    {
                        _triangles[triangleIndex++] = current;
                        _triangles[triangleIndex++] = next;
                        _triangles[triangleIndex++] = upperCurrent;

                        _triangles[triangleIndex++] = next;
                        _triangles[triangleIndex++] = upperNext;
                        _triangles[triangleIndex++] = upperCurrent;
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

                _vertices[vertexIndex] = vertex;
                _uvs[vertexIndex] = new Vector2((float)j / tubeSegments, 1f);
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
            
            _triangles[triangleIndex++] = innerCurrent;
            _triangles[triangleIndex++] = outerCurrent;
            _triangles[triangleIndex++] = innerNext;
            
            _triangles[triangleIndex++] = innerNext;
            _triangles[triangleIndex++] = outerCurrent;
            _triangles[triangleIndex++] = outerNext;
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

                _vertices[vertexIndex] = vertex;
                _uvs[vertexIndex] = new Vector2((float)j / tubeSegments, 1f);
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
            
            _triangles[triangleIndex++] = innerCurrent;
            _triangles[triangleIndex++] = innerNext;
            _triangles[triangleIndex++] = outerCurrent;
            
            _triangles[triangleIndex++] = innerNext;
            _triangles[triangleIndex++] = outerNext;
            _triangles[triangleIndex++] = outerCurrent;
        }
        
        

        if (!_generatedObject)
        {
            _generatedObject = new GameObject("Funnel", typeof(MeshFilter), typeof(MeshRenderer));
            _meshFilter = _generatedObject.GetComponent<MeshFilter>();
            _meshRenderer = _generatedObject.GetComponent<MeshRenderer>();
            _meshRenderer.material = _material;
        }
        
        _mesh.Clear();
        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;
        _mesh.uv = _uvs;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _meshFilter.mesh = _mesh;
    }
}