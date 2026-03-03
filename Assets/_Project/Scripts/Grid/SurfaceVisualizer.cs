using System.Collections.Generic;
using UnityEngine;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Renders colored hex fill overlays for cells with active surfaces.
    /// Attach to the GridSystem GameObject alongside HexGridMap.
    /// Rebuilds mesh when surfaces change (checked via SurfaceSystem.IsDirty).
    /// </summary>
    [RequireComponent(typeof(HexGridMap))]
    public class SurfaceVisualizer : MonoBehaviour
    {
        private HexGridMap _gridMap;
        private SurfaceSystem _surfaceSystem;
        private Material _fillMaterial;
        private Mesh _fillMesh;
        private bool _initialized;

        public void Initialize(HexGridMap gridMap, SurfaceSystem surfaceSystem)
        {
            _gridMap = gridMap;
            _surfaceSystem = surfaceSystem;
            _fillMesh = new Mesh { name = "SurfaceOverlay" };

            EnsureFillMaterial();
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized || _surfaceSystem == null)
                return;

            if (_surfaceSystem.IsDirty)
            {
                RebuildMesh();
                _surfaceSystem.ClearDirty();
            }
        }

        private void OnRenderObject()
        {
            if (!_initialized || _fillMesh == null || _fillMesh.vertexCount == 0)
                return;

            EnsureFillMaterial();
            _fillMaterial.SetPass(0);
            Graphics.DrawMeshNow(_fillMesh, Matrix4x4.identity);
        }

        private void RebuildMesh()
        {
            _fillMesh.Clear();

            var activeSurfaces = _surfaceSystem.ActiveSurfaces;
            if (activeSurfaces.Count == 0)
                return;

            float outerRadius = _gridMap.Config.HexOuterRadius;
            float surfaceOffset = 0.08f; // Slightly above terrain

            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            foreach (var kvp in activeSurfaces)
            {
                var coord = kvp.Key;
                var instance = kvp.Value;

                Vector3 center = _gridMap.GetCellWorldPosition(coord);
                center.y += surfaceOffset;

                Color color = instance.Definition.TintColor;

                // Build hex fill (6 triangles from center to each edge)
                int baseIndex = vertices.Count;
                vertices.Add(center);
                colors.Add(color);

                Vector3[] corners = GetHexCorners(center, outerRadius * 0.9f);
                for (int i = 0; i < 6; i++)
                {
                    vertices.Add(corners[i]);
                    // Fade color at edges
                    Color edgeColor = color;
                    edgeColor.a *= 0.5f;
                    colors.Add(edgeColor);
                }

                for (int i = 0; i < 6; i++)
                {
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 1 + i);
                    triangles.Add(baseIndex + 1 + (i + 1) % 6);
                }
            }

            _fillMesh.SetVertices(vertices);
            _fillMesh.SetColors(colors);
            _fillMesh.SetTriangles(triangles, 0);
            _fillMesh.RecalculateBounds();
        }

        private static Vector3[] GetHexCorners(Vector3 center, float radius)
        {
            var corners = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angleDeg = 60f * i;
                float angleRad = angleDeg * Mathf.Deg2Rad;
                corners[i] = new Vector3(
                    center.x + radius * Mathf.Cos(angleRad),
                    center.y,
                    center.z + radius * Mathf.Sin(angleRad)
                );
            }
            return corners;
        }

        private void EnsureFillMaterial()
        {
            if (_fillMaterial != null) return;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Hidden/Internal-Colored");

            _fillMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _fillMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _fillMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _fillMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _fillMaterial.SetInt("_ZWrite", 0);
        }

        private void OnDestroy()
        {
            if (_fillMesh != null) Destroy(_fillMesh);
            if (_fillMaterial != null) DestroyImmediate(_fillMaterial);
        }
    }
}
