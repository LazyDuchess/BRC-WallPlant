using System;
using System.Collections.Generic;
using Reptile;
using UnityEngine;

namespace WallPlant
{
	public class DecalManager : MonoBehaviour
	{
		private void Awake()
		{
			DecalManager.Instance = this;
			this.CacheLevelMeshes();
		}

		public List<LevelMesh> GetLevelMeshesIntersectingBounds(Bounds bounds, LayerMask layermask)
		{
			List<LevelMesh> list = new List<LevelMesh>();
			foreach (LevelMesh levelMesh in this._levelMeshes)
			{
				if (!(levelMesh.Renderer == null) && !(levelMesh.Mesh == null) && ((1 << levelMesh.Renderer.gameObject.layer) & layermask) != 0 && bounds.Intersects(levelMesh.Renderer.bounds))
				{
					list.Add(levelMesh);
				}
			}
			return list;
		}

		private void CacheLevelMeshes()
		{
			this._levelMeshes.Clear();
			foreach (MeshRenderer meshRenderer in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
			{
				MeshRenderer meshRenderer2 = meshRenderer;
				MeshFilter component = meshRenderer.GetComponent<MeshFilter>();
				if (!(component == null) && !(component.sharedMesh == null) && !meshRenderer.GetComponent<Rigidbody>())
				{
					LevelMesh levelMesh = new LevelMesh
					{
						Mesh = component.sharedMesh,
						Renderer = meshRenderer2
					};
					this._levelMeshes.Add(levelMesh);
				}
			}
		}

		private void OnDestroy()
		{
			DecalManager.Instance = null;
		}

		internal static void Initialize()
		{
			StageManager.OnStagePostInitialization += DecalManager.StageManager_OnStagePostInitialization;
		}

		private static void StageManager_OnStagePostInitialization()
		{
			new GameObject("Decal Manager").AddComponent<DecalManager>();
		}

		public void PushDecal(Decal decal)
		{
			if (this._decals.Count >= WallPlantSettings.MaxGraffiti)
			{
				Decal decal2 = this._decals.Dequeue();
				if (decal2 != null)
				{
					UnityEngine.Object.Destroy(decal2);
				}
			}
			this._decals.Enqueue(decal);
		}

		private List<LevelMesh> _levelMeshes = new List<LevelMesh>();

		private Queue<Decal> _decals = new Queue<Decal>();

		public static DecalManager Instance;
	}
}
