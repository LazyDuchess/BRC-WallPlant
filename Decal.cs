using System;
using System.Collections.Generic;
using Reptile;
using UnityEngine;

namespace WallPlant
{
	public class Decal : MonoBehaviour
	{
		private void Awake()
		{
			Core.OnUpdate += this.OnUpdate;
		}

		private void OnUpdate()
		{
			if (this._material == null)
			{
				return;
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
			if (!this._animating)
			{
				return;
			}
			this._progress += WallPlantSettings.GraffitiPaintSpeed * Core.dt;
			if (this._progress >= 1f)
			{
				this._progress = 1f;
				this._animating = false;
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

		private void MakeDecalMesh(Transform originalTransform, Mesh mesh)
		{
			GameObject gameObject = new GameObject("Decal Mesh");
			_decals.Add(gameObject);
			gameObject.transform.SetParent(originalTransform);
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.transform.localRotation = Quaternion.identity;
			gameObject.transform.localScale = Vector3.one;
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
			Renderer renderer = gameObject.AddComponent<MeshRenderer>();
			meshFilter.sharedMesh = mesh;
			renderer.sharedMaterial = _material;
		}

		public void Build(LayerMask affectedLayers)
		{
			_material = new Material(Plugin.GraffitiMaterial);
			Bounds bounds = new Bounds(base.transform.position, base.transform.localScale);
			foreach (LevelMesh levelMesh in DecalManager.Instance.GetLevelMeshesIntersectingBounds(bounds, affectedLayers))
			{
				MakeDecalMesh(levelMesh.Renderer.transform, levelMesh.Mesh);
			}
		}

		public void AnimateSpray()
		{
			_material.SetFloat(Decal.ProgressProperty, 0f);
			_progress = 0f;
			_animating = true;
		}

		private void OnDestroy()
		{
			Core.OnUpdate -= OnUpdate;
			foreach (GameObject gameObject in _decals)
			{
				if (!(gameObject == null))
				{
					UnityEngine.Object.Destroy(gameObject);
				}
			}
			UnityEngine.Object.Destroy(_material);
		}

		private static int ProgressProperty = Shader.PropertyToID("_Progress");

		private float _progress;

		private bool _animating;

		private Material _material;

		private List<GameObject> _decals = new List<GameObject>();
	}
}
