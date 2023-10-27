using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Reptile;

namespace WallPlant
{
    public class DecalManager : MonoBehaviour
    {
        private Queue<Decal> _decals = new Queue<Decal>();
        public static DecalManager Instance;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            Instance = null;
        }

        internal static void Initialize()
        {
            StageManager.OnStagePostInitialization += StageManager_OnStagePostInitialization;
        }

        private static void StageManager_OnStagePostInitialization()
        {
            var gameObject = new GameObject("Decal Manager");
            var decalManager = gameObject.AddComponent<DecalManager>();
        }

        public void PushDecal(Decal decal)
        {
            if (_decals.Count >= WallPlantSettings.MaxGraffiti)
            {
                var oldestDecal = _decals.Dequeue();
                if (oldestDecal != null)
                    Destroy(oldestDecal);
            }
            _decals.Enqueue(decal);
        }
    }
}
