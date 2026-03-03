using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Renders colored diamond/shield markers on cells that have cover.
    /// Yellow for HalfCover, blue for FullCover.
    /// Attach to the GridSystem GameObject alongside HexGridMap.
    /// </summary>
    [RequireComponent(typeof(HexGridMap))]
    public class CoverVisualizer : MonoBehaviour
    {
        private static readonly Color HalfCoverColor = new Color(1f, 0.85f, 0.2f, 0.7f);
        private static readonly Color FullCoverColor = new Color(0.3f, 0.5f, 1f, 0.7f);

        private HexGridMap _gridMap;
        private Material _material;
        private Mesh _mesh;
        private bool _initialized;

        public void Initialize(HexGridMap gridMap)
        {
            _gridMap = gridMap;
            _mesh = new Mesh { name = "CoverOverlay" };

            EnsureMaterial();
            BuildMesh();
            _initialized = true;
        }

        private void OnRenderObject()
        {
            if (!_initialized || _mesh == null || _mesh.vertexCount == 0)
                return;

            EnsureMaterial();
            _material.SetPass(0);
            Graphics.DrawMeshNow(_mesh, Matrix4x4.identity);
        }

        private void BuildMesh()
        {
            _mesh.Clear();

            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            float markerSize = _gridMap.Config.HexOuterRadius * 0.25f;
            float coverOffset = 0.12f; // Above terrain and surfaces

            foreach (var kvp in _gridMap.AllCells)
            {
                var cell = kvp.Value;
                if (cell.Cover == CoverType.None)
                    continue;

                Vector3 center = _gridMap.GetCellWorldPosition(cell.Coord);
                center.y += coverOffset;

                Color color = cell.Cover == CoverType.FullCover ? FullCoverColor : HalfCoverColor;

                // Draw a small diamond (4 triangles from center)
                int baseIndex = vertices.Count;
                vertices.Add(center);
                colors.Add(color);

                // 4 points of the diamond
                vertices.Add(center + new Vector3(0, 0, markerSize));       // N
                vertices.Add(center + new Vector3(markerSize, 0, 0));       // E
                vertices.Add(center + new Vector3(0, 0, -markerSize));      // S
                vertices.Add(center + new Vector3(-markerSize, 0, 0));      // W

                for (int i = 0; i < 4; i++)
                    colors.Add(color);

                // 4 triangles
                triangles.Add(baseIndex); triangles.Add(baseIndex + 1); triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex); triangles.Add(baseIndex + 2); triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex); triangles.Add(baseIndex + 3); triangles.Add(baseIndex + 4);
                triangles.Add(baseIndex); triangles.Add(baseIndex + 4); triangles.Add(baseIndex + 1);
            }

            if (vertices.Count == 0)
                return;

            _mesh.SetVertices(vertices);
            _mesh.SetColors(colors);
            _mesh.SetTriangles(triangles, 0);
            _mesh.RecalculateBounds();
        }

        private void EnsureMaterial()
        {
            if (_material != null) return;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Hidden/Internal-Colored");

            _material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _material.SetInt("_ZWrite", 0);
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_material != null) DestroyImmediate(_material);
        }
    }
}
