using System;
using System.Collections.Generic;
using Reptile;
using UnityEngine;

namespace WallPlant
{
	public class Decal : MonoBehaviour
	{
		public Action OnDestroyCallback;
		private Bounds _cullBounds;
		private Mesh _decalMesh;

		private void Awake()
		{
            Core.OnUpdate += OnUpdate;
        }

        private void OnRenderObject()
        {
			if (_decalMesh == null)
				return;
			if (Vector3.SqrMagnitude(Camera.current.transform.position - transform.position) > WallPlantSettings.GraffitiDrawDistance)
				return;
            var planes = GeometryUtility.CalculateFrustumPlanes(Camera.current);
			if (!GeometryUtility.TestPlanesAABB(planes, _cullBounds))
				return;
			_material.SetPass(0);
			Graphics.DrawMeshNow(_decalMesh, Matrix4x4.identity);
        }

        private void OnUpdate()
		{
			if (this._material == null)
			{
				return;
			}
			this._progress += WallPlantSettings.GraffitiPaintSpeed * Core.dt;
			if (this._progress >= 1f)
			{
				this._progress = 1f;
                this._animating = false;
                Core.OnUpdate -= OnUpdate;
            }
			this._material.SetFloat(Decal.ProgressProperty, this._progress);
		}

		public static Decal Create(Vector3 point, Vector3 normal, float size, LayerMask affectedLayers)
		{
			Decal decal = new GameObject("Decal")
			{
				transform =
				{
					position = point,
					rotation = Quaternion.LookRotation(normal),
					localScale = new Vector3(size, size, size)
				}
			}.AddComponent<Decal>();
			decal.Build(affectedLayers);
			DecalManager.Instance.PushDecal(decal);
			return decal;
		}

		public void SetSize(float size)
		{
			base.transform.localScale = new Vector3(size, size, size);
		}

		public void SetTexture(Texture texture)
		{
			_material.mainTexture = texture;
		}

		public void Build(LayerMask affectedLayers)
		{
			if (Plugin.GraffitiMaterial == null)
				return;
			if (DecalManager.Instance == null)
				return;
			_material = new Material(Plugin.GraffitiMaterial);
			_cullBounds = new Bounds(base.transform.position, base.transform.localScale * 2f);
			var intersectingMeshes = DecalManager.Instance.GetLevelMeshesIntersectingBounds(_cullBounds, affectedLayers);
            var instances = new CombineInstance[intersectingMeshes.Count];

			for (var i = 0; i < intersectingMeshes.Count; i++)
			{
				for (var n = 0; n < intersectingMeshes[i].Mesh.subMeshCount; n++)
				{
					instances[i] = new CombineInstance() { mesh = intersectingMeshes[i].Mesh, transform = intersectingMeshes[i].Renderer.localToWorldMatrix, subMeshIndex = n };
				}
			}

			_decalMesh = new Mesh();
			_decalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			try
			{
				_decalMesh.CombineMeshes(instances, true, true, false);
			}
			catch(Exception e)
			{
				Destroy(_decalMesh);
				_decalMesh = null;
			}

			if (_decalMesh.vertexCount > WallPlantSettings.MaxVertices)
			{
				Destroy(_decalMesh);
                _decalMesh = null;
            }

            float num = base.transform.lossyScale.x * 0.5f;
            float num2 = base.transform.lossyScale.y * 0.5f;
            float num3 = base.transform.lossyScale.z * 0.5f;
            Vector3 vector = base.transform.forward * num3 + base.transform.right * num + base.transform.up * num2;
            Matrix4x4 matrix4x = base.transform.worldToLocalMatrix * Matrix4x4.Translate(vector);
            this._material.SetMatrix("_Projection", matrix4x);
            this._material.SetVector("_Origin", base.transform.position);
            this._material.SetVector("_Bounds", base.transform.lossyScale * 2f);
            this._material.SetVector("_Normal", base.transform.forward);
        }

		public void AnimateSpray()
		{
			_material.SetFloat(Decal.ProgressProperty, 0f);
			_progress = 0f;
			_animating = true;
		}

		public void SetCompleted()
        {
			_material.SetFloat(Decal.ProgressProperty, 1f);
			_progress = 1f;
			_animating = false;
        }

		private void OnDestroy()
		{
			OnDestroyCallback?.Invoke();
			Core.OnUpdate -= OnUpdate;
			UnityEngine.Object.Destroy(_material);
			if (_decalMesh != null)
				UnityEngine.Object.Destroy(_decalMesh);
		}

		private static int ProgressProperty = Shader.PropertyToID("_Progress");

		private float _progress;

		private bool _animating;

		private Material _material;
	}
}
