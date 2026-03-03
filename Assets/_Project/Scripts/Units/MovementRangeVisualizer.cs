using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using TurnBasedTactics.Core;
using TurnBasedTactics.Combat;
using TurnBasedTactics.Grid;

namespace TurnBasedTactics.Units
{
    /// <summary>
    /// DOS2-style movement range visualizer.
    /// Reachable cells  → blue ground tint + bright perimeter border
    /// Unreachable cells → gray dimming overlay
    /// Raycast starts close to cell Y to avoid projecting onto buildings above.
    /// </summary>
    public class MovementRangeVisualizer : MonoBehaviour
    {
        [Header("Reachable — blue tint + perimeter border")]
        [SerializeField] private Color _reachableFillColor = new Color(0.3f, 0.6f, 1f, 0.55f);
        [SerializeField] private Color _perimeterBorderColor = new Color(0.3f, 0.75f, 1f, 0.85f);

        [Header("Unreachable — gray overlay")]
        [SerializeField] private Color _unreachableFillColor = new Color(0.4f, 0.4f, 0.4f, 0.3f);

        [Header("Projection")]
        [Tooltip("Layers to raycast against for terrain surface (exclude Units layer)")]
        [SerializeField] private LayerMask _terrainLayerMask = ~0;
        [Tooltip("How far above cell Y to start the raycast (keep small to avoid hitting buildings)")]
        [SerializeField] private float _localRayHeight = 2f;
        [SerializeField] private float _surfaceOffset = 0.05f;

        private HexGridMap _gridMap;
        private UnitRegistry _registry;

        // Mesh objects
        private GameObject _reachableMeshGO;
        private GameObject _unreachableMeshGO;
        private MeshFilter _reachableMeshFilter;
        private MeshFilter _unreachableMeshFilter;
        private MeshRenderer _reachableRenderer;
        private MeshRenderer _unreachableRenderer;
        private Material _fillMaterial;

        // GL.Lines perimeter border
        private Material _lineMaterial;
        private List<Vector3> _perimeterSegments; // pairs of (start, end)

        // State
        private Dictionary<HexCoord, float> _currentReachable;
        private bool _showRange;
        private int _activeUnitId = -1;

        // Edge index → HexDirection mapping for flat-top hex
        // Edge i connects corner[i] to corner[(i+1)%6].
        // Edge 0 (30°) → dir 0 (E), Edge 1 (90°) → dir 5 (SE),
        // Edge 2 (150°) → dir 4 (SW), Edge 3 (210°) → dir 3 (W),
        // Edge 4 (270°) → dir 2 (NW), Edge 5 (330°) → dir 1 (NE)
        private static readonly int[] EdgeToDirection = { 0, 5, 4, 3, 2, 1 };

        // ----------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------

        public void Initialize(HexGridMap gridMap, UnitRegistry registry)
        {
            _gridMap = gridMap;
            _registry = registry;

            int unitsLayer = LayerMask.NameToLayer("Units");
            if (unitsLayer >= 0)
                _terrainLayerMask &= ~(1 << unitsLayer);

            CreateMeshObjects();
        }

        public bool IsReachable(HexCoord coord)
        {
            return _currentReachable != null && _currentReachable.ContainsKey(coord);
        }

        public void ShowRange(UnitRuntime unit)
        {
            ClearHighlights();

            if (unit == null || !unit.CanMove) return;

            // Effective range is the lesser of movement points and current AP
            int effectiveRange = Mathf.Min(unit.Stats.MovementPoints, unit.CurrentAP);

            var pathConfig = HexPathfinder.PathConfig.Default;
            _currentReachable = HexPathfinder.GetReachable(
                _gridMap, unit.GridPosition, effectiveRange, pathConfig);

            float radius = _gridMap.Config.HexOuterRadius * 0.95f;

            // ── Reachable: blue fill ──
            BuildHexMesh(_reachableMeshFilter, _currentReachable.Keys, radius, _reachableFillColor);

            // ── Perimeter border: only edges where neighbor is NOT reachable ──
            BuildPerimeterSegments(radius);

            // ── Unreachable: gray dimming overlay ──
            int extendedRange = effectiveRange + 3;
            var grayCoords = new List<HexCoord>();
            var extendedCoords = HexCoord.GetRange(unit.GridPosition, extendedRange);
            foreach (var coord in extendedCoords)
            {
                if (_currentReachable.ContainsKey(coord)) continue;
                if (!_gridMap.TryGetCell(coord, out HexCell cell)) continue;
                if (!cell.Walkable) continue;
                grayCoords.Add(coord);
            }
            BuildHexMesh(_unreachableMeshFilter, grayCoords, radius, _unreachableFillColor);

            _reachableMeshGO.SetActive(true);
            _unreachableMeshGO.SetActive(grayCoords.Count > 0);
            _showRange = true;
        }

        public void ClearHighlights()
        {
            _currentReachable = null;
            _perimeterSegments = null;
            _showRange = false;

            if (_reachableMeshGO != null)
                _reachableMeshGO.SetActive(false);
            if (_unreachableMeshGO != null)
                _unreachableMeshGO.SetActive(false);
        }

        // ----------------------------------------------------------------
        // Perimeter border — only the outermost edges of the reachable area
        // ----------------------------------------------------------------

        private void BuildPerimeterSegments(float radius)
        {
            _perimeterSegments = new List<Vector3>();

            foreach (var coord in _currentReachable.Keys)
            {
                Vector3 hexCenter = _gridMap.GetCellWorldPosition(coord);
                Vector3[] corners = GetHexCorners(hexCenter, radius);

                for (int edge = 0; edge < 6; edge++)
                {
                    // Check if the neighbor across this edge is also reachable
                    HexDirection neighborDir = (HexDirection)EdgeToDirection[edge];
                    HexCoord neighbor = coord.GetNeighbor(neighborDir);

                    if (_currentReachable.ContainsKey(neighbor))
                        continue; // Interior edge — skip

                    // Perimeter edge — project and add as line segment
                    Vector3 a = ProjectOnTerrain(corners[edge]);
                    Vector3 b = ProjectOnTerrain(corners[(edge + 1) % 6]);

                    // Slight lift above fill mesh
                    a += Vector3.up * 0.02f;
                    b += Vector3.up * 0.02f;

                    _perimeterSegments.Add(a);
                    _perimeterSegments.Add(b);
                }
            }
        }

        private void OnRenderObject()
        {
            if (!_showRange || _perimeterSegments == null || _perimeterSegments.Count == 0)
                return;

            EnsureLineMaterial();
            _lineMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);
            GL.Color(_perimeterBorderColor);

            for (int i = 0; i < _perimeterSegments.Count; i += 2)
            {
                GL.Vertex(_perimeterSegments[i]);
                GL.Vertex(_perimeterSegments[i + 1]);
            }

            GL.End();
            GL.PopMatrix();
        }

        // ----------------------------------------------------------------
        // Mesh generation — filled hex overlay
        // ----------------------------------------------------------------

        private void BuildHexMesh(
            MeshFilter meshFilter,
            IEnumerable<HexCoord> coords,
            float radius,
            Color fillColor)
        {
            if (meshFilter == null) return;

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();

            Color edgeColor = fillColor;
            edgeColor.a *= 0.25f;

            foreach (var coord in coords)
            {
                Vector3 hexCenter = _gridMap.GetCellWorldPosition(coord);
                Vector3 projectedCenter = ProjectOnTerrain(hexCenter);

                int baseIndex = vertices.Count;

                // Center vertex
                vertices.Add(projectedCenter);
                colors.Add(fillColor);

                // Corner vertices
                Vector3[] corners = GetHexCorners(hexCenter, radius);
                for (int i = 0; i < 6; i++)
                {
                    vertices.Add(ProjectOnTerrain(corners[i]));
                    colors.Add(edgeColor);
                }

                // Mid-ring for smoother gradient + terrain conformance
                float midRatio = 0.55f;
                Color midColor = Color.Lerp(fillColor, edgeColor, 0.35f);
                int midBase = vertices.Count;

                for (int i = 0; i < 6; i++)
                {
                    Vector3 midPoint = Vector3.Lerp(hexCenter, corners[i], midRatio);
                    vertices.Add(ProjectOnTerrain(midPoint));
                    colors.Add(midColor);
                }

                // Inner ring: center → mid-ring (6 tris)
                for (int i = 0; i < 6; i++)
                {
                    triangles.Add(baseIndex);
                    triangles.Add(midBase + i);
                    triangles.Add(midBase + (i + 1) % 6);
                }

                // Outer ring: mid-ring → corners (12 tris)
                for (int i = 0; i < 6; i++)
                {
                    int nextI = (i + 1) % 6;

                    triangles.Add(midBase + i);
                    triangles.Add(baseIndex + 1 + i);
                    triangles.Add(baseIndex + 1 + nextI);

                    triangles.Add(midBase + i);
                    triangles.Add(baseIndex + 1 + nextI);
                    triangles.Add(midBase + nextI);
                }
            }

            var mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.name = "HexRangeOverlay";
                meshFilter.sharedMesh = mesh;
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        // ----------------------------------------------------------------
        // Terrain projection — start ray close to cell Y, not from far above
        // ----------------------------------------------------------------

        /// <summary>
        /// Project a world position onto terrain via downward raycast.
        /// Starts the ray just above the cell's expected Y position so that
        /// buildings/props far above the ground plane are never hit.
        /// </summary>
        private Vector3 ProjectOnTerrain(Vector3 worldPos)
        {
            // Start ray from cellY + localRayHeight (e.g. cellY + 2m)
            // If ground is at Y=-3.25, ray starts at Y=-1.25, so buildings at Y=0 are above the ray
            Vector3 rayOrigin = new Vector3(worldPos.x, worldPos.y + _localRayHeight, worldPos.z);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                    _localRayHeight * 2f, _terrainLayerMask))
            {
                return hit.point + Vector3.up * _surfaceOffset;
            }

            // Fallback: use original position
            return worldPos + Vector3.up * _surfaceOffset;
        }

        // ----------------------------------------------------------------
        // Mesh & material setup
        // ----------------------------------------------------------------

        private void CreateMeshObjects()
        {
            _fillMaterial = CreateFillMaterial();

            _reachableMeshGO = CreateMeshChild("ReachableOverlay", _fillMaterial);
            _unreachableMeshGO = CreateMeshChild("UnreachableOverlay", _fillMaterial);

            _reachableMeshFilter = _reachableMeshGO.GetComponent<MeshFilter>();
            _unreachableMeshFilter = _unreachableMeshGO.GetComponent<MeshFilter>();
            _reachableRenderer = _reachableMeshGO.GetComponent<MeshRenderer>();
            _unreachableRenderer = _unreachableMeshGO.GetComponent<MeshRenderer>();

            _reachableMeshGO.SetActive(false);
            _unreachableMeshGO.SetActive(false);
        }

        private GameObject CreateMeshChild(string name, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            return go;
        }

        private static Material CreateFillMaterial()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("UI/Default");

            var mat = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            mat.SetColor("_Color", Color.white);
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_Cull", (int)CullMode.Off);
            mat.renderQueue = 3100;

            return mat;
        }

        private void EnsureLineMaterial()
        {
            if (_lineMaterial != null) return;

            var shader = Shader.Find("Hidden/Internal-Colored");
            _lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _lineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull", (int)CullMode.Off);
            _lineMaterial.SetInt("_ZWrite", 0);
            _lineMaterial.SetInt("_ZTest", (int)CompareFunction.LessEqual);
        }

        // ----------------------------------------------------------------
        // EventBus subscriptions
        // ----------------------------------------------------------------

        private void OnEnable()
        {
            EventBus.Subscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Subscribe<UnitDeselectedEvent>(OnUnitDeselected);
            EventBus.Subscribe<UnitMoveCompletedEvent>(OnUnitMoveCompleted);
            EventBus.Subscribe<ActiveUnitChangedEvent>(OnActiveUnitChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<UnitSelectedEvent>(OnUnitSelected);
            EventBus.Unsubscribe<UnitDeselectedEvent>(OnUnitDeselected);
            EventBus.Unsubscribe<UnitMoveCompletedEvent>(OnUnitMoveCompleted);
            EventBus.Unsubscribe<ActiveUnitChangedEvent>(OnActiveUnitChanged);
        }

        private void OnActiveUnitChanged(ActiveUnitChangedEvent evt)
        {
            _activeUnitId = evt.UnitId;
        }

        private void OnUnitSelected(UnitSelectedEvent evt)
        {
            if (!_registry.TryGetUnit(evt.UnitId, out UnitRuntime unit)) return;
            if (unit.TeamId != 0) return;

            if (_activeUnitId >= 0 && evt.UnitId != _activeUnitId)
            {
                ClearHighlights();
                return;
            }

            ShowRange(unit);
        }

        private void OnUnitDeselected(UnitDeselectedEvent evt)
        {
            ClearHighlights();
        }

        private void OnUnitMoveCompleted(UnitMoveCompletedEvent evt)
        {
            ClearHighlights();
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private static Vector3[] GetHexCorners(Vector3 center, float outerRadius)
        {
            var corners = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angleDeg = 60f * i;
                float angleRad = angleDeg * Mathf.Deg2Rad;
                corners[i] = new Vector3(
                    center.x + outerRadius * Mathf.Cos(angleRad),
                    center.y,
                    center.z + outerRadius * Mathf.Sin(angleRad));
            }
            return corners;
        }

        private void OnDestroy()
        {
            if (_fillMaterial != null)
                DestroyImmediate(_fillMaterial);
            if (_lineMaterial != null)
                DestroyImmediate(_lineMaterial);
            if (_reachableMeshGO != null)
                Destroy(_reachableMeshGO);
            if (_unreachableMeshGO != null)
                Destroy(_unreachableMeshGO);
        }
    }
}
