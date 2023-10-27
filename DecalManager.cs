using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WallPlant
{
    public class DecalManager : MonoBehaviour
    {
        public static DecalManager Instance;

        void Awake()
        {
            Instance = this;
        }
    }
}
