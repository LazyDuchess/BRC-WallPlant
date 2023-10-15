using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;

namespace WallPlant
{

    public static class WallPlantSettings
    {
        public static bool RegainAirMobility => _regainAirMobility.Value;
        public static float MaxWallAngle => _maxWallAngle.Value;
        public static float MaxJumpOffWallAngle => _maxJumpOffWallAngle.Value;
        public static float JumpForce => _jumpForce.Value;
        public static float SpeedMultiplier => _speedMultiplier.Value;
        public static float JumpOffWallOffset => _jumpOffWallOffset.Value;
        public static float ParkourWallOffset => 0.8f;
        public static float MoveStyleWallOffset => 0.25f;
        public static float GracePeriod => _gracePeriod.Value;
        public static float MinimumSpeed => _minimumSpeed.Value;
        public static float WallPlantsUntilMaxPenalty => _wallPlantsUntilMaxPenalty.Value;
        public static int MaximumWallPlants => _maximumWallPlants.Value;

        private static ConfigEntry<bool> _regainAirMobility;
        private static ConfigEntry<float> _maxWallAngle;
        private static ConfigEntry<float> _maxJumpOffWallAngle;
        private static ConfigEntry<float> _jumpForce;
        private static ConfigEntry<float> _speedMultiplier;
        private static ConfigEntry<float> _jumpOffWallOffset;
        private static ConfigEntry<float> _gracePeriod;
        private static ConfigEntry<float> _minimumSpeed;
        private static ConfigEntry<float> _wallPlantsUntilMaxPenalty;
        private static ConfigEntry<int> _maximumWallPlants;

        public static void Initialize(ConfigFile config)
        {
            _gracePeriod = config.Bind("WallPlant",
                "GracePeriod",
                0.15f,
                "Window of time after hitting a wall at high speed that you'll be able to wall plant off of it."
                );

            _maximumWallPlants = config.Bind("WallPlant",
                "MaxWallPlants",
                4,
                "Maximum amount of wall plants you can do in a row, before you have to grind/wallrun/land/etc. to reset the counter. Set this to 0 to allow unlimited wall plants in a row."
                );

            _wallPlantsUntilMaxPenalty = config.Bind("WallPlant",
                "WallPlantsUntilMaxPenalty",
                5f,
                "Amount of wall plants in a row until your strength drops to zero. This should probably be higher than the maximum amount of wall plants you can do so that you don't drop to zero strength at the last one. This number can have a fractional part. Set this to 0 to disable the penalty completely."
                );

            _regainAirMobility = config.Bind("WallPlant",
                "RegainAirMobility",
                true,
                "Whether you should be allowed to boost or dash again after doing a wall plant."
                );

            _speedMultiplier = config.Bind("WallPlant",
                "SpeedMultiplier",
                0.65f,
                "How much of your velocity into the wall will be transferred when you jump off the wall."
                );

            _jumpForce = config.Bind("WallPlant",
                "JumpForce",
                9f,
                "Upwards velocity applied when jumping off a wall."
                );

            _minimumSpeed = config.Bind("WallPlant",
                "MinimumSpeed",
                2.5f,
                "Minimum speed into a wall to be able to wall plant off of it."
                );

            _maxWallAngle = config.Bind("WallPlant",
                "MaxWallAngle",
                40f,
                "Maximum angle difference between the player direction and the wall angle."
                );

            _maxJumpOffWallAngle = config.Bind("WallPlant",
                "MaxJumpOffWallAngle",
                40f,
                "Maximum angle relative to the wall angle that you're allowed to jump off the wall."
                );

            _jumpOffWallOffset = config.Bind("WallPlant",
                "JumpOffWallOffset",
                0.9f,
                "How far away from the wall you'll be teleported when you jump off."
                );
        }
    }
}
