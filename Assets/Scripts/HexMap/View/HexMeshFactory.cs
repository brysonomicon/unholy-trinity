using System.Collections.Generic;
using UnityEngine;

namespace UnholyTrinity.Hex
{
    /// <summary>Builds and caches simple procedural meshes for tiles, so the map works without art assets.</summary>
    public static class HexMeshFactory
    {
        static readonly Dictionary<(HexOrientation, float, float), Mesh> PrismCache = new Dictionary<(HexOrientation, float, float), Mesh>();
        static readonly Dictionary<float, Mesh> ArrowCache = new Dictionary<float, Mesh>();

        /// <summary>A hexagonal prism with its base at y = 0 (top and sides only; the underside is never seen).</summary>
        public static Mesh Prism(HexOrientation orientation, float size, float height)
        {
            var key = (orientation, size, height);
            if (PrismCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var layout = new HexLayout(orientation, size, Vector3.zero);
            var top = Vector3.up * height;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            // Top face: centre plus six corners, wound clockwise when viewed from above.
            vertices.Add(top);
            for (int i = 0; i < 6; i++)
                vertices.Add(top + layout.CornerOffset(i));
            for (int i = 0; i < 6; i++)
                triangles.AddRange(new[] { 0, 1 + (i + 1) % 6, 1 + i });

            // Sides: separate vertices per face for flat shading.
            for (int i = 0; i < 6; i++)
            {
                var a = layout.CornerOffset(i);
                var b = layout.CornerOffset((i + 1) % 6);
                int start = vertices.Count;
                vertices.Add(a);
                vertices.Add(a + top);
                vertices.Add(b + top);
                vertices.Add(b);
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            var mesh = new Mesh { name = $"HexPrism_{orientation}_{size}_{height}" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            PrismCache[key] = mesh;
            return mesh;
        }

        /// <summary>A flat triangle pointing along +Z, for showing a fixed boost direction.</summary>
        public static Mesh Arrow(float size)
        {
            if (ArrowCache.TryGetValue(size, out var cached) && cached != null)
                return cached;

            var mesh = new Mesh { name = $"HexArrow_{size}" };
            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(0f, 0f, 0.6f * size),
                new Vector3(0.35f * size, 0f, -0.3f * size),
                new Vector3(-0.35f * size, 0f, -0.3f * size),
            });
            mesh.SetTriangles(new[] { 0, 1, 2 }, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            ArrowCache[size] = mesh;
            return mesh;
        }
    }
}
