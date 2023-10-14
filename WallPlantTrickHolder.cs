using Reptile;
using HarmonyLib;
using UnityEngine;

namespace WallPlant
{
    public class WallPlantTrickHolder : MonoBehaviour
    {
        public Player Player;
        public Player.Trick WallPlantTrick = new Player.Trick(60, 20, 10);
        private void Awake()
        {
            Player = GetComponent<Player>();
        }
        public void Refresh()
        {
            var changed = false;
            WallPlantTrick.Refresh(ref changed);
        }
        public void Use(string trickName = "", int trickNum = 0)
        {
            var traversePlayer = Traverse.Create(Player);
            var handPlantTrick = traversePlayer.Field("handplantTrick");
            var handPlantTrickOld = handPlantTrick.GetValue();
            handPlantTrick.SetValue(WallPlantTrick);
            Player.DoTrick(Player.TrickType.HANDPLANT, trickName, trickNum);
            handPlantTrick.SetValue(handPlantTrickOld);
        }
        public static WallPlantTrickHolder Get(Player player)
        {
            return player.GetComponent<WallPlantTrickHolder>();
        }
    }
}
