using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Generator : MonoBehaviour
{
    private const string SaveFileName = "funnel.json";
    private const string MeshName = "Procedural Funnel";
    private const string GameObjectName = "Funnel";
    private const int RingEdgeCountResolution = 16;
    private const int SectionCount = 2; // Tube and cone
    private const float ShapeThickness = 0.1f;
    private const int SideRingStride = RingEdgeCountResolution + 1; // +1 for duplicated vertex at the seam
    private const int RingCount = SectionCount + 1; // One extra for the top of the cone
    
    [SerializeField] private InputField _topRimDiameterInputField;
    [SerializeField] private InputField _lowerTubeDiameterInputField;
    [SerializeField] private InputField _slopingSidesVerticalHeightInputField;
    [SerializeField] private InputField _tubeVerticalHeightInputField;
    [SerializeField] private InputField _textureScaleInputField;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _loadButton;
    [SerializeField] private Button _leftRotateButton, _rightRotateButton, _upRotateButton, _downRotateButton;
    [SerializeField] private Material _material;
    
    private GameObject _generatedObject;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    
    private float _topRimDiameter = 2f;
    private float _lowerTubeDiameter = 1f;
    private float _slopingSidesVerticalHeight = 2f;
    private float _tubeVerticalHeight = 1f;
    private float _textureScale = 1f;
    private Vector3[] _vertices;
    private Vector2[] _uvs;
    private int[] _triangles;
    private Mesh _mesh;
    private Quaternion _targetRotation = Quaternion.identity;
    private Vector3 _targetPosition = Vector3.zero;

    private void Awake()
    {
        AllocateMeshData();

        _mesh = new Mesh
        {
            name = MeshName
        };
    }

    private void Start()
    {
        RefreshInputFields();
        BindUi();
        Generate();
    }

    private void AllocateMeshData()
    {
        var sideVertexCount = SideRingStride * RingCount * 2;
        var capVertexCount = RingEdgeCountResolution * 2 * 2;
        var vertexCount = sideVertexCount + capVertexCount;

        var sideTriangleCount = RingEdgeCountResolution * SectionCount * 2 * 3 * 2;
        var capTriangleCount = RingEdgeCountResolution * 2 * 3 * 2;
        var triangleCount = sideTriangleCount + capTriangleCount;

        _vertices = new Vector3[vertexCount];
        _uvs = new Vector2[vertexCount];
        _triangles = new int[triangleCount];
    }

    private void BindUi()
    {
        BindFloatInput(_topRimDiameterInputField, value =>
        {
            _topRimDiameter = value;
            Generate();
        });

        BindFloatInput(_lowerTubeDiameterInputField, value =>
        {
            _lowerTubeDiameter = value;
            Generate();
        });

        BindFloatInput(_slopingSidesVerticalHeightInputField, value =>
        {
            _slopingSidesVerticalHeight = value;
            Generate();
        });

        BindFloatInput(_tubeVerticalHeightInputField, value =>
        {
            _tubeVerticalHeight = value;
            Generate();
        });

        BindFloatInput(_textureScaleInputField, value =>
        {
            _textureScale = Mathf.Max(0.01f, value);
            ApplyTextureScale();
        });
        
        _leftRotateButton.onClick.AddListener(() => _targetRotation *= Quaternion.Euler(0f, -15f, 0f));
        _rightRotateButton.onClick.AddListener(() => _targetRotation *= Quaternion.Euler(0f, 15f, 0f));
        _upRotateButton.onClick.AddListener(() => _targetRotation *= Quaternion.Euler(-15f, 0f, 0f));
        _downRotateButton.onClick.AddListener(() => _targetRotation *= Quaternion.Euler(15f, 0f, 0f));
        _quitButton.onClick.AddListener(Application.Quit);

        _saveButton.onClick.AddListener(Save);
        _loadButton.onClick.AddListener(Load);
    }

    private void BindFloatInput(InputField inputField, Action<float> onParsed)
    {
        inputField.onValueChanged.AddListener(value =>
        {
            if (float.TryParse(value, out var result))
            {
                onParsed(result);
            }
        });
    }

    private void Update()
    {
        if (_generatedObject)
        {
            _generatedObject.transform.rotation = Quaternion.Slerp(_generatedObject.transform.rotation, _targetRotation, Time.deltaTime * 5f);
            
            var boundsCenter = _meshRenderer.bounds.center;
            // move the object centre to origin
            var targetPosition = -boundsCenter;
            _generatedObject.transform.position = Vector3.Lerp(_generatedObject.transform.position, targetPosition, Time.deltaTime * 5f);
        }
    }

    private void RefreshInputFields()
    {
        _topRimDiameterInputField.text = _topRimDiameter.ToString();
        _lowerTubeDiameterInputField.text = _lowerTubeDiameter.ToString();
        _slopingSidesVerticalHeightInputField.text = _slopingSidesVerticalHeight.ToString();
        _tubeVerticalHeightInputField.text = _tubeVerticalHeight.ToString();
        _textureScaleInputField.text = _textureScale.ToString();
    }

    private void ApplyTextureScale()
    {
        if (!_meshRenderer || !_meshRenderer.material || !_meshRenderer.material.mainTexture)
        {
            return;
        }
        
        _meshRenderer.material.mainTextureScale = new Vector2(_textureScale, _textureScale);
    }

    private void Save()
    {
        var shapeData = new ShapeData
        {
            TopRimDiameter = _topRimDiameter,
            LowerTubeDiameter = _lowerTubeDiameter,
            SlopingSidesVerticalHeight = _slopingSidesVerticalHeight,
            TubeVerticalHeight = _tubeVerticalHeight
        };
        
        // Save the shape data rather than the mesh data (verts etc) to keep the file human-readable and editable
        var json = JsonUtility.ToJson(shapeData, true);
        var path = Path.Combine(Application.persistentDataPath, SaveFileName);
        File.WriteAllText(path, json);
    }

    private void Load()
    {
        var path = Path.Combine(Application.persistentDataPath, SaveFileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning("Save file not found");
            return;
        }

        var json = File.ReadAllText(path);
        var data = JsonUtility.FromJson<ShapeData>(json);

        _topRimDiameter = data.TopRimDiameter;
        _lowerTubeDiameter = data.LowerTubeDiameter;
        _slopingSidesVerticalHeight = data.SlopingSidesVerticalHeight;
        _tubeVerticalHeight = data.TubeVerticalHeight;

        _topRimDiameterInputField.text = _topRimDiameter.ToString();
        _lowerTubeDiameterInputField.text = _lowerTubeDiameter.ToString();
        _slopingSidesVerticalHeightInputField.text = _slopingSidesVerticalHeight.ToString();
        _tubeVerticalHeightInputField.text = _tubeVerticalHeight.ToString();

        Generate();
    }

    [ContextMenu(nameof(Generate))]
    public void Generate()
    {
        var sectionCount = SectionCount; // Tube and cone
        var ringCount = sectionCount + 1; // One extra for the top of the cone
        var ringRadii = new float[] { _lowerTubeDiameter/2f, _lowerTubeDiameter/2f, _topRimDiameter/2f };
        var ringHeights = new float[] { 0f, _tubeVerticalHeight, _tubeVerticalHeight + _slopingSidesVerticalHeight };
        var tubeSegments = RingEdgeCountResolution;
        var thickness = ShapeThickness;
        
        var angleStep = 360f / tubeSegments;
        var sideRingStride = tubeSegments + 1;

        var vertexIndex = 0;

        for (int sideIndex = 0; sideIndex < 2; sideIndex++)
        {
            var thicknessOffset = sideIndex == 0 ? thickness / 2f : -thickness / 2f;

            for (int i = 0; i < ringCount; i++)
            {
                var radius = ringRadii[i] + thicknessOffset;
                var y = ringHeights[i];

                for (int j = 0; j <= tubeSegments; j++)
                {
                    var wrappedJ = j % tubeSegments;
                    var angle = wrappedJ * angleStep * Mathf.Deg2Rad;

                    var x = Mathf.Cos(angle) * radius;
                    var z = Mathf.Sin(angle) * radius;
                    var vertex = new Vector3(x, y, z);

                    _vertices[vertexIndex] = vertex;

                    var u = j / (float)tubeSegments;
                    var v = i / (float)(ringCount - 1);
                    _uvs[vertexIndex] = new Vector2(u, v);

                    vertexIndex++;
                }
            }
        }

        var triangleIndex = 0;

        // Generate triangles with correct winding order for both sides in one pass without sharing vertices

        for (int sideIndex = 0; sideIndex < 2; sideIndex++)
        {
            var isInnerFace = sideIndex == 1;
            var sideOffset = sideIndex * sideRingStride * ringCount;
            
            for (int i = 0; i < sectionCount; i++)
            {
                for (int j = 0; j < tubeSegments; j++)
                {
                    var current = sideOffset + i * sideRingStride + j;
                    var next = current + 1;
                    var upperCurrent = sideOffset + (i + 1) * sideRingStride + j;
                    var upperNext = upperCurrent + 1;
                    
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
            _generatedObject = new GameObject(GameObjectName, typeof(MeshFilter), typeof(MeshRenderer));
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
        
        ApplyTextureScale();
    }
}