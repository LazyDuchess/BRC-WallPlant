using Reptile;
using HarmonyLib;
using UnityEngine;

namespace WallPlant
{
    /// <summary>
    /// Just holds the trick class for the WallPlant trick, so that it can get stale and go back to standard points.
    /// </summary>
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

        // This is hacky asf, but basically we replace the handplant trick with ours, do the trick then set it back to handplant. This is a very basic trick with no special behavior so it works for us.
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
