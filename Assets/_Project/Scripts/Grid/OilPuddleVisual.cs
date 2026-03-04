using UnityEngine;

namespace TurnBasedTactics.Grid
{
    /// <summary>
    /// Visual representation of an oil surface puddle.
    /// Placed on hex cells to show oil surface effect.
    /// </summary>
    public class OilPuddleVisual : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Color _oilColor = new Color(0.2f, 0.15f, 0.05f, 0.7f); // Dark brownish
        [SerializeField] private float _puddleRadius = 0.6f;
        [SerializeField] private float _heightOffset = 0.02f;

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propBlock;

        private void Awake()
        {
            BuildPuddleMesh();
        }

        private void BuildPuddleMesh()
        {
            // Create a simple quad mesh for the puddle
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            _renderer = gameObject.AddComponent<MeshRenderer>();

            // Create quad mesh
            Mesh mesh = new Mesh();
            mesh.name = "OilPuddleQuad";

            float size = _puddleRadius * 2f;
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-size/2, 0, -size/2),
                new Vector3(size/2, 0, -size/2),
                new Vector3(-size/2, 0, size/2),
                new Vector3(size/2, 0, size/2)
            };

            int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };

            Vector2[] uvs = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            // Create material
            var material = new Material(Shader.Find("Standard"));
            material.color = _oilColor;
            material.SetFloat("_Metallic", 0.3f);
            material.SetFloat("_Glossiness", 0.6f);
            _renderer.material = material;

            // Position slightly above ground
            transform.localPosition = new Vector3(0, _heightOffset, 0);
        }

        public void SetColor(Color color)
        {
            _oilColor = color;
            if (_renderer != null && _renderer.material != null)
            {
                _renderer.material.color = color;
            }
        }
    }
}
