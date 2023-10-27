using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using CommonAPI;

namespace WallPlant
{
    public class Decal : MonoBehaviour
    {
        private Mesh _mesh;
        private Material _material;

        public static Decal Create(Vector3 point, Vector3 normal)
        {
            var gameObject = new GameObject("Decal");
            var decal = gameObject.AddComponent<Decal>();
            decal.Build(point, normal);
            return decal;
        }

        public void SetSize(float size)
        {
            transform.localScale = new Vector3(size, size, size);
        }

        public void SetTexture(Texture texture)
        {
            _material.mainTexture = texture;
        }

        public void Build(Vector3 point, Vector3 normal)
        {
            transform.position = point + (normal * 0.0025f);
            transform.rotation = Quaternion.LookRotation(normal, Vector3.up);
            _mesh = new Mesh();

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();

            verts.Add(new Vector3(-0.5f, -0.5f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            verts.Add(new Vector3(0.5f, -0.5f, 0f));
            uvs.Add(new Vector2(0f, 0f));
            verts.Add(new Vector3(0.5f, 0.5f, 0f));
            uvs.Add(new Vector2(0f, 1f));

            tris.Add(0);
            tris.Add(1);
            tris.Add(2);

            verts.Add(new Vector3(-0.5f, 0.5f, 0f));
            uvs.Add(new Vector2(1f, 1f));

            tris.Add(2);
            tris.Add(3);
            tris.Add(0);

            norms.Add(Vector3.forward);
            norms.Add(Vector3.forward);
            norms.Add(Vector3.forward);
            norms.Add(Vector3.forward);

            _mesh.SetVertices(verts);
            _mesh.SetNormals(norms);
            _mesh.SetUVs(0, uvs);
            _mesh.SetIndices(tris, MeshTopology.Triangles, 0);

            _material = new Material(AssetAPI.GetShader(AssetAPI.ShaderNames.AmbientEnvironmentTransparent));

            var filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = _mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = _material;
        }

        private void OnDestroy()
        {
            Destroy(_mesh);
            Destroy(_material);
        }
    }
}
