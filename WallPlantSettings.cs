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
        public static bool GraffitiPlantDefault => _graffitiPlantDefault.Value;
        public static bool GraffitiPlantSlideButton => _graffitiPlantSlideButton.Value;
        public static float GraffitiPaintSpeed => _graffitiPaintSpeed.Value;
        public static float GraffitiSize => _graffitiSize.Value;
        public static int MaxGraffiti => _maxGraffiti.Value;
        public static bool EnableAlternativePlant => _enableAlternativePlant.Value;
        public static bool AbsoluteSpeed => _absoluteSpeed.Value;
        public static float RaycastDistance => _raycastDistance.Value;
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

        private static ConfigEntry<bool> _graffitiPlantDefault;
        private static ConfigEntry<bool> _graffitiPlantSlideButton;
        private static ConfigEntry<float> _graffitiPaintSpeed;
        private static ConfigEntry<float> _graffitiSize;
        private static ConfigEntry<int> _maxGraffiti;
        private static ConfigEntry<bool> _enableAlternativePlant;
        private static ConfigEntry<bool> _absoluteSpeed;
        private static ConfigEntry<float> _raycastDistance;
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
            _graffitiPlantDefault = config.Bind("GraffitiPlant",
                "GraffitiPlantByDefault",
                false,
                "Whether to Graffiti Plant by default, and have to press the graf plant button to do a regular Wall Plant instead."
                );

            _graffitiPlantSlideButton = config.Bind("GraffitiPlant",
                "GraffitiPlantSlideButton",
                false,
                "Whether to use the slide button instead of the spray button to do a graffiti plant."
                );

            _graffitiPaintSpeed = config.Bind("GraffitiPlant",
                "GraffitiPaintSpeed",
                3f,
                "Speed at which the graffiti gets sprayed."
                );

            _graffitiSize = config.Bind("GraffitiPlant",
                "GraffitiSize",
                1.5f,
                "Size of each graffiti."
                );

            _maxGraffiti = config.Bind("GraffitiPlant",
                "MaxGraffiti",
                20,
                "Maximum amount of graffiti in a level, before they will start getting cleaned up."
                );

            _enableAlternativePlant = config.Bind("GraffitiPlant",
                "EnableAlternativePlant",
                true,
                "Whether to enable the alt plant. If Graffitti Plants are default, then Wall Plants will be disabled, and vice versa."
                );

            _absoluteSpeed = config.Bind("WallPlant",
                "Absolute Speed",
                false,
                "If this is true, then your absolute speed will be used for wall jumping regardless of direction rather than just your speed directly into the wall surface."
                );

            _raycastDistance = config.Bind("WallPlant",
               "RayCastDistance",
               2f,
               "Distance to look for walls. The higher this number is the further away you can be from walls and still be able to wallplant."
               );

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
                0.75f,
                "How much of your velocity into the wall will be transferred when you jump off the wall."
                );

            _jumpForce = config.Bind("WallPlant",
                "JumpForce",
                9f,
                "Upwards velocity applied when jumping off a wall."
                );

            _minimumSpeed = config.Bind("WallPlant",
                "MinimumSpeed",
                1f,
                "Minimum speed into a wall to be able to wall plant off of it."
                );

            _maxWallAngle = config.Bind("WallPlant",
                "MaxWallAngle",
                60f,
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
