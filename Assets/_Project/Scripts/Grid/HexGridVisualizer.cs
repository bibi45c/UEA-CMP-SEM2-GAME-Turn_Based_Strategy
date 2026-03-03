using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Renders a toggleable hex wireframe overlay using GL.Lines.
    /// Attach alongside HexGridMap on the GridSystem GameObject.
    /// Toggle via ShowGrid property or player settings.
    /// </summary>
    [RequireComponent(typeof(HexGridMap))]
    public class HexGridVisualizer : MonoBehaviour
    {
        private HexGridMap _gridMap;
        private Material _lineMaterial;
        private bool _showGrid;

        // Cached vertex arrays per cell for performance
        private Dictionary<HexCoord, Vector3[]> _cachedVertices;
        private bool _cacheBuilt;

        public bool ShowGrid
        {
            get => _showGrid;
            set => _showGrid = value;
        }

        private void Awake()
        {
            _gridMap = GetComponent<HexGridMap>();
        }

        /// <summary>
        /// Call after HexGridMap.Initialize() to build the vertex cache.
        /// </summary>
        public void BuildCache()
        {
            if (_gridMap == null)
                _gridMap = GetComponent<HexGridMap>();
            if (_gridMap == null || !_gridMap.IsInitialized) return;

            _cachedVertices = new Dictionary<HexCoord, Vector3[]>();

            foreach (var kv in _gridMap.AllCells)
            {
                var coord = kv.Key;
                var cell = kv.Value;
                Vector3 center = _gridMap.GetCellWorldPosition(coord);
                center.y += 0.05f; // Slight offset above terrain

                Vector3[] verts = GetHexCorners(center, _gridMap.Config.HexOuterRadius);
                _cachedVertices[coord] = verts;
            }

            _showGrid = _gridMap.Config.ShowGridByDefault;
            _cacheBuilt = true;

            Debug.Log($"[HexGridVisualizer] Cache built for {_cachedVertices.Count} cells.");
        }

        private void OnRenderObject()
        {
            if (!_showGrid || !_cacheBuilt || _cachedVertices == null) return;

            EnsureLineMaterial();
            _lineMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);

            var config = _gridMap.Config;

            foreach (var kv in _cachedVertices)
            {
                HexCell cell = _gridMap.GetCell(kv.Key);
                if (cell == null) continue;

                Color color = cell.Walkable ? config.GridLineColor : config.UnwalkableColor;
                GL.Color(color);

                Vector3[] verts = kv.Value;
                for (int i = 0; i < 6; i++)
                {
                    GL.Vertex(verts[i]);
                    GL.Vertex(verts[(i + 1) % 6]);
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        /// <summary>
        /// Get the 6 corner positions of a flat-top hex.
        /// </summary>
        private static Vector3[] GetHexCorners(Vector3 center, float outerRadius)
        {
            var corners = new Vector3[6];

            for (int i = 0; i < 6; i++)
            {
                // Flat-top hex: first corner at 0 degrees
                float angleDeg = 60f * i;
                float angleRad = angleDeg * Mathf.Deg2Rad;
                corners[i] = new Vector3(
                    center.x + outerRadius * Mathf.Cos(angleRad),
                    center.y,
                    center.z + outerRadius * Mathf.Sin(angleRad)
                );
            }

            return corners;
        }

        private void EnsureLineMaterial()
        {
            if (_lineMaterial != null) return;

            // Unity built-in shader for colored lines
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            _lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _lineMaterial.SetInt("_ZWrite", 0);
            _lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null)
            {
                DestroyImmediate(_lineMaterial);
            }
        }
    }
}
