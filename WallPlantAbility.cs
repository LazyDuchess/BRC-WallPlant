using HarmonyLib;
using Reptile;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WallPlant
{
    // Wall plant ability itself.
    public class WallPlantAbility : Ability
    {
        /// <summary>
        /// Keep track of how many times we've wall planted without grinding/wallrunning/landing/etc. Subsequent wall plants will get weaker until we hit the limit.
        /// </summary>
        public int TimesPlanted = 0;

        // By planting i mean when our feet is on the wall and we're frozen in place, planted out is when we actually launched off the wall.
        enum State
        {
            Planting,
            PlantedOut,
        }

        /// <summary>
        /// When we plant out a wall the player will face the direction of our controls, but we don't want to be able to jump off directly into the wall or parallel to it, doesn't make a lot of sense.
        /// So here we constrain the angle relative to the wall. If no input is pressed, we just launch off out of the wall in a straight line.
        /// </summary>
        private static float MaxAnglePlantOut = 40f;
        /// <summary>
        /// The speed that is added vertically when we wall plant.
        /// </summary>
        private static float JumpSpeed = 8f;
        /// <summary>
        /// Horizontal speed when we wall plant, taken from the speed we had going into the wall, multiplied by this.
        /// </summary>
        private static float SpeedMultiplier = 0.6f;
        /// <summary>
        /// The higher this number the more our speed will be penalized in subsequent wall plants without landing.
        /// </summary>
        private static float PlantPenaltyDivider = 0.65f;
        /// <summary>
        /// How far away from the wall we'll be teleported when we plant out.
        /// </summary>
        private static float JumpOffWallOffset = 0.9f;
        /// <summary>
        /// How far away from the wall we'll be when we have our feet on the wall and are on foot.
        /// </summary>
        private static float ParkourWallOffset = 0.8f;
        /// <summary>
        /// How far away from the wall we'll be when we have our feet on the wall and are using a vehicle.
        /// </summary>
        private static float VehicleWallOffset = 0.25f;
        /// <summary>
        /// Grace period for when we hit a wall at high enough speed but we're still allowed to wall plant despite losing our speed.
        /// </summary>
        private static float MinSpeedGracePeriod = 0.1f;
        /// <summary>
        /// Minimum speed into walls for a wall plant.
        /// </summary>
        private static float MinSpeed = 5f;
        /// <summary>
        /// Maximum subsequent wall plants without landing.
        /// </summary>
        private static float MaxPlants = 4;
        /// <summary>
        /// Duration of the pause when we wall plant.
        /// </summary>
        private static float HitpauseDuration = 0.17f;

        private float _speedIntoWall = 0f;
        private float _timeSinceReachedMinSpeed = 1000f;

        // Animations!
        private int _parkourHash = Animator.StringToHash("hitBounce");
        private int _footPlantHash = Animator.StringToHash("grindRetour");
        private int _footPlantOutHash = Animator.StringToHash("airTrick0");
        private int _grindDirectionHash = Animator.StringToHash("grindDirection");

        private State _state = State.Planting;
        private bool _didTrick = false;
        private Vector3 _wallNormal;
        private Vector3 _wallPoint;

        public WallPlantAbility(Player player) : base(player)
        {
            var traversePlayer = Traverse.Create(p);
            var abilities = traversePlayer.Field("abilities").GetValue<List<Ability>>();
            // Need to process this ability before air dashing, so we insert at the beginning.
            abilities.Remove(this);
            abilities.Insert(0, this);
            Init();
        }

        // Helper funtion to return WallPlantAbility from a player.
        public static WallPlantAbility Get(Player player)
        {
            var traversePlayer = Traverse.Create(player);
            var abilities = traversePlayer.Field("abilities").GetValue<List<Ability>>();
            foreach(var ability in abilities)
            {
                if (ability is WallPlantAbility)
                    return ability as WallPlantAbility;
            }
            return null;
        }

        public override void Init()
        {
            var traversePlayer = Traverse.Create(p);
            var stats = traversePlayer.Field("stats").GetValue<MovementStats>();

            normalMovement = true;
            decc = stats.airDecc;
            customGravity = 0f;
            treatPlayerAsSortaGrounded = true;
        }

        public void Trigger(Vector3 wallPoint, Vector3 wallNormal)
        {
            _wallPoint = wallPoint;
            _wallNormal = wallNormal;
            p.ActivateAbility(this);
        }

        public override void OnStartAbility()
        {
            var traversePlayer = Traverse.Create(p);
            var moveStyle = traversePlayer.Field("moveStyle").GetValue<MoveStyle>();
            var off = VehicleWallOffset;
            if (moveStyle == MoveStyle.ON_FOOT)
                off = ParkourWallOffset;
            p.SetPosAndRotHard(_wallPoint + _wallNormal * off, Quaternion.LookRotation(-_wallNormal, Vector3.up));
            TimesPlanted++;
            acc = 0f;
            targetSpeed = 0f;
            normalRotation = false;
            canStartGrind = false;
            canStartWallrun = false;
            p.RegainAirMobility();
            _didTrick = false;
            SetState(State.Planting);
        }

        // Reset times planted when appropriate and calculate grace periods. This runs even if the ability isn't active.
        public void PassiveUpdate(Ability ability)
        {
            if (ability != this)
            {
                if (p.IsGrounded() || p.IsGrinding() || ability is HandplantAbility || ability is HeadspinAbility || ability is WallrunLineAbility)
                    TimesPlanted = 0;
                if (GetWallForPlant(out Vector3 _, out Vector3 wallNormal))
                {
                    var velocityTowardsWall = Vector3.Dot(p.motor.velocity, -wallNormal);
                    if (velocityTowardsWall >= MinSpeed)
                    {
                        if (velocityTowardsWall > _speedIntoWall)
                            _speedIntoWall = velocityTowardsWall;
                        _timeSinceReachedMinSpeed = 0f;
                        return;
                    }
                }
                if (_timeSinceReachedMinSpeed > MinSpeedGracePeriod)
                    _speedIntoWall = 0f;
            }

            _timeSinceReachedMinSpeed += Core.dt;

            // i'm scared of overflows
            if (_timeSinceReachedMinSpeed > 1000f)
                _timeSinceReachedMinSpeed = 1000f;
        }

        // Checks if a gameobject is valid for a wall plant.
        private bool ValidSurface(GameObject obj)
        {
            if (obj.layer != 0 && obj.layer != 10 && obj.layer != 4)
                return false;
            if (!obj.CompareTag("Untagged"))
                return false;
            return true;
        }

        public override void FixedUpdateAbility()
        {
            p.SetVisualRotLocal0();
            var traversePlayer = Traverse.Create(p);

            var stats = traversePlayer.Field("stats").GetValue<MovementStats>();
            var airDashAbility = traversePlayer.Field("airDashAbility").GetValue<AirDashAbility>();
            var airTrickAbility = traversePlayer.Field("airTrickAbility").GetValue<AirTrickAbility>();
            var abilityTimer = traversePlayer.Field("abilityTimer").GetValue<float>();
            var anim = traversePlayer.Field("anim").GetValue<Animator>();

            anim.SetFloat(_grindDirectionHash, 0f);

            if (_state == State.PlantedOut)
            {
                // Cancel if we end up on the floor too soon.
                if (p.IsGrounded())
                {
                    p.StopCurrentAbility();
                    return;
                }
                if (abilityTimer > 0.25f && !_didTrick)
                {
                    var wallPlantTrickHolder = WallPlantTrickHolder.Get(p);
                    wallPlantTrickHolder.Use("Wall Plant", 0);
                    _didTrick = true;
                }
                acc = stats.airAcc + 10f;
                targetSpeed = stats.walkSpeed + 5f;
                normalRotation = true;
                // Allow dashes and tricks mid wall plant.
                if (abilityTimer > 0.5f)
                {
                    if (airDashAbility.CheckActivation())
                    {
                        return;
                    }
                    if (airTrickAbility.CheckActivation())
                    {
                        return;
                    }
                }
                // End.
                if (abilityTimer > 0.8f)
                {
                    p.StopCurrentAbility();
                    return;
                }
            }
            else 
            {
                if (abilityTimer > HitpauseDuration)
                    SetState(State.PlantedOut);
                return;
            }
        }

        private void PlaySfxGameplay(AudioClipID audioClipID, float randomPitchVariance = 0f)
        {
            var traversePlayer = Traverse.Create(p);
            var moveStyle = traversePlayer.Field("moveStyle").GetValue<MoveStyle>();
            var playerOneShotAudioSource = traversePlayer.Field("playerOneShotAudioSource").GetValue<AudioSource>();
            var audioManager = p.AudioManager;
            var traverseAudioManager = Traverse.Create(audioManager);
            traverseAudioManager.Method("PlaySfxGameplay", new Type[] { typeof(MoveStyle), typeof(AudioClipID), typeof(AudioSource), typeof(float) }, new object[] { moveStyle, audioClipID, playerOneShotAudioSource, randomPitchVariance}).GetValue();
        }

        private void PlaySfxGameplay(SfxCollectionID collectionId, AudioClipID audioClipID, float randomPitchVariance = 0f)
        {
            var traversePlayer = Traverse.Create(p);
            var playerOneShotAudioSource = traversePlayer.Field("playerOneShotAudioSource").GetValue<AudioSource>();
            var audioManager = p.AudioManager;
            var traverseAudioManager = Traverse.Create(audioManager);
            traverseAudioManager.Method("PlaySfxGameplay", new Type[] { typeof(SfxCollectionID), typeof(AudioClipID), typeof(AudioSource), typeof(float) }, new object[] { collectionId, audioClipID, playerOneShotAudioSource, randomPitchVariance }).GetValue();
        }

        private void SetState(State state)
        {
            var traversePlayer = Traverse.Create(p);
            var moveStyle = traversePlayer.Field("moveStyle").GetValue<MoveStyle>();
            _state = state;
            if (_state == State.Planting)
            {
                PlaySfxGameplay(SfxCollectionID.CombatSfx, AudioClipID.ShieldBlock);
                p.SetVelocity(Vector3.zero);
                if (moveStyle == MoveStyle.ON_FOOT)
                    p.PlayAnim(_parkourHash, true, true);
                else
                    p.PlayAnim(_footPlantHash, true, true);
                return;
            }

            canStartGrind = true;
            canStartWallrun = true;

            p.PlayAnim(_footPlantOutHash, true, true);

            p.PlayVoice(AudioClipID.VoiceJump);
            PlaySfxGameplay(AudioClipID.jump);

            
            var moveInput = traversePlayer.Field("moveInput").GetValue<Vector3>();
            var newRot = Quaternion.LookRotation(_wallNormal, Vector3.up).eulerAngles;

            // Calculate a jump off direction based on our input if there is input.
            if (moveInput != Vector3.zero)
            {
                moveInput.y = 0f;
                
                var moveRot = Quaternion.LookRotation(moveInput, Vector3.up).eulerAngles;
                var angleDiff = Mathf.DeltaAngle(moveRot.y, newRot.y);
                angleDiff = Mathf.Clamp(angleDiff, -MaxAnglePlantOut, MaxAnglePlantOut);
                newRot.y -= angleDiff;
            }

            newRot.x = 0f;
            var newQuat = Quaternion.Euler(newRot);
            p.SetPosAndRotHard(_wallPoint + _wallNormal * JumpOffWallOffset, newQuat);
            p.SetVisualRot(newQuat);

            var offWallVelocity = _speedIntoWall * SpeedMultiplier;
            var upVelocity = JumpSpeed;

            // Start penalizing our speed if we're wall planting multiple times in a row.
            if (TimesPlanted > 1)
            {
                var divide = TimesPlanted * PlantPenaltyDivider;
                offWallVelocity /= divide;
                upVelocity /= divide;
            }

            p.SetVelocity(_wallNormal * offWallVelocity + Vector3.up * upVelocity);

            var ringParticles = traversePlayer.Field("ringParticles").GetValue<ParticleSystem>();

            var isJumping = traversePlayer.Field("isJumping");
            var maintainSpeedJump = traversePlayer.Field("maintainSpeedJump");
            var jumpConsumed = traversePlayer.Field("jumpConsumed");
            var jumpRequested = traversePlayer.Field("jumpRequested");
            var jumpedThisFrame = traversePlayer.Field("jumpedThisFrame");
            var timeSinceLastJump = traversePlayer.Field("timeSinceLastJump");

            ringParticles.Emit(1);
            if (p.IsGrounded())
            {
                isJumping.SetValue(true);
                maintainSpeedJump.SetValue(false);
                jumpConsumed.SetValue(true);
                jumpRequested.SetValue(false);
                jumpedThisFrame.SetValue(true);
                timeSinceLastJump.SetValue(0f);
                traversePlayer.Method("ForceUnground", true).GetValue();
            }
        }


        // Returns an appropriate surface in front of us for wall planting.
        private bool GetWallForPlant(out Vector3 point, out Vector3 normal)
        {
            point = Vector3.zero;
            normal = Vector3.zero;

            var raySeparation = 0.33f;
            var rayDistance = 1.25f;
            var collisionSize = 0.5f;

            var rayAmount = 0;
            var setLastPoint = false;
            var lastPointAlongPlayerDirection = 0f;
            var lastPoint = Vector3.zero;
            var normalAccumulation = Vector3.zero;
            for(var i=-collisionSize;i<=collisionSize;i+=raySeparation)
            {
                rayAmount++;
                var ray = new Ray(p.transform.position + (Vector3.up * i) + (Vector3.up * 0.5f), p.motor.dir);
                if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
                    return false;

                //Plugin.Instance.GetLogger().LogInfo($"Hit {hit.collider.gameObject.name}, layer: {hit.collider.gameObject.layer}, tag: {hit.collider.gameObject.tag}");

                if (!ValidSurface(hit.collider.gameObject))
                    return false;

                var pointAlongPlayerDirection = Vector3.Dot(hit.point - p.transform.position, p.motor.dir);

                var hitPoint = hit.point;
                hitPoint.y = p.transform.position.y;

                if (!setLastPoint)
                {
                    lastPointAlongPlayerDirection = pointAlongPlayerDirection;
                    lastPoint = hitPoint;
                    setLastPoint = true;
                }
                else
                {
                    if (pointAlongPlayerDirection < lastPointAlongPlayerDirection)
                    {
                        lastPointAlongPlayerDirection = pointAlongPlayerDirection;
                        lastPoint = hitPoint;
                    }
                }

                normalAccumulation += hit.normal;
            }
            point = lastPoint;
            normal = (normalAccumulation / rayAmount).normalized;
            return true;
        }

        // Logic to trigger wall planting.
        public override bool CheckActivation()
        {
            if (TimesPlanted >= MaxPlants)
                return false;
            var traversePlayer = Traverse.Create(p);
            var jumpRequested = traversePlayer.Field("jumpRequested").GetValue<bool>();
            if (p.jumpButtonNew && !p.IsGrounded() && (!jumpRequested || !p.JumpIsAllowed()) && !locked)
            {
                if (!GetWallForPlant(out Vector3 wallPoint, out Vector3 wallNormal))
                    return false;
                if (_timeSinceReachedMinSpeed > MinSpeedGracePeriod)
                    return false;
                Trigger(wallPoint, wallNormal);
                return true;
            }
            return false;
        }
    }
}
