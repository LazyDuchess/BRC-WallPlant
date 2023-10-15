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
        /// Duration of the pause when we wall plant.
        /// </summary>
        private static float HitpauseDuration = 0.2f;

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
        private bool _hasWall = false;

        public WallPlantAbility(Player player) : base(player)
        {
            var traversePlayer = Traverse.Create(p);
            var abilities = traversePlayer.Field("abilities").GetValue<List<Ability>>();
            // Need to process this ability before air dashing, so we insert at the beginning.
            abilities.Remove(this);
            abilities.Insert(0, this);
            Init();
        }

        // Helper function to return WallPlantAbility from a player.
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
            var off = WallPlantSettings.MoveStyleWallOffset;
            if (moveStyle == MoveStyle.ON_FOOT)
                off = WallPlantSettings.ParkourWallOffset;
            p.SetPosAndRotHard(_wallPoint + _wallNormal * off, Quaternion.LookRotation(-_wallNormal, Vector3.up));
            TimesPlanted++;
            acc = 0f;
            targetSpeed = 0f;
            normalRotation = false;
            canStartGrind = false;
            canStartWallrun = false;
            _didTrick = false;

            if (WallPlantSettings.RegainAirMobility)
                p.RegainAirMobility();

            SetState(State.Planting);
        }

        // Reset times planted when appropriate and calculate grace periods. This runs even if the ability isn't active.
        public void PassiveUpdate(Ability ability)
        {
            _hasWall = false;
            // Just doing this to integrate the trick more seamlessly into the game as it's possible to sequence break otherwise.
            if (Core.Instance.SaveManager.CurrentSaveSlot.CurrentStoryObjective == Story.ObjectiveID.EscapePoliceStation)
            {
                var pTraverse = Traverse.Create(p);
                var airDashAbility = pTraverse.Field("airDashAbility").GetValue<AirDashAbility>();
                if (airDashAbility.locked)
                    locked = true;
                else 
                    locked = false;
            }

            if (ability != this)
            {
                if (p.IsGrounded() || p.IsGrinding() || ability is HandplantAbility || ability is HeadspinAbility || ability is WallrunLineAbility)
                    TimesPlanted = 0;
                if (GetWallForPlant(out Vector3 wallPoint, out Vector3 wallNormal))
                {
                    var angleBetweenWall = Vector3.Angle(p.motor.dir, -wallNormal);
                    if (angleBetweenWall <= WallPlantSettings.MaxWallAngle)
                    {
                        _hasWall = true;
                        _wallNormal = wallNormal;
                        _wallPoint = wallPoint;
                        var velocityTowardsWall = Vector3.Dot(p.motor.velocity, -wallNormal);
                        if (velocityTowardsWall >= WallPlantSettings.MinimumSpeed)
                        {
                            if (velocityTowardsWall > _speedIntoWall)
                                _speedIntoWall = velocityTowardsWall;
                            _timeSinceReachedMinSpeed = 0f;
                            return;
                        }
                    }
                }
                if (_timeSinceReachedMinSpeed > WallPlantSettings.GracePeriod)
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

                if (abilityTimer > 0.4f)
                {
                    p.SetBoostpackAndFrictionEffects(BoostpackEffectMode.OFF, FrictionEffectMode.OFF);
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

            p.SetBoostpackAndFrictionEffects(BoostpackEffectMode.ON, FrictionEffectMode.OFF);

            var ringParticles = traversePlayer.Field("ringParticles").GetValue<ParticleSystem>();
            ringParticles.Emit(1);

            var moveInput = traversePlayer.Field("moveInput").GetValue<Vector3>();
            var newRot = Quaternion.LookRotation(_wallNormal, Vector3.up).eulerAngles;

            // Calculate a jump off direction based on our input if there is input.
            if (moveInput != Vector3.zero)
            {
                moveInput.y = 0f;
                
                var moveRot = Quaternion.LookRotation(moveInput, Vector3.up).eulerAngles;
                var angleDiff = Mathf.DeltaAngle(moveRot.y, newRot.y);
                angleDiff = Mathf.Clamp(angleDiff, -WallPlantSettings.MaxJumpOffWallAngle, WallPlantSettings.MaxJumpOffWallAngle);
                newRot.y -= angleDiff;
            }

            newRot.x = 0f;
            var newQuat = Quaternion.Euler(newRot);
            p.SetPosAndRotHard(_wallPoint + _wallNormal * WallPlantSettings.JumpOffWallOffset, newQuat);
            p.SetVisualRot(newQuat);

            var offWallVelocity = _speedIntoWall * WallPlantSettings.SpeedMultiplier;
            var upVelocity = WallPlantSettings.JumpForce;

            // Start penalizing our speed if we're wall planting multiple times in a row.
            var penaltyPlants = WallPlantSettings.WallPlantsUntilMaxPenalty - 1f;
            var currentPlant = (float)TimesPlanted - 1;
            var multiply = -(currentPlant - penaltyPlants) / penaltyPlants;
            offWallVelocity *= multiply;
            upVelocity *= multiply;

            p.SetVelocity(_wallNormal * offWallVelocity + Vector3.up * upVelocity);

            var isJumping = traversePlayer.Field("isJumping");
            var maintainSpeedJump = traversePlayer.Field("maintainSpeedJump");
            var jumpConsumed = traversePlayer.Field("jumpConsumed");
            var jumpRequested = traversePlayer.Field("jumpRequested");
            var jumpedThisFrame = traversePlayer.Field("jumpedThisFrame");
            var timeSinceLastJump = traversePlayer.Field("timeSinceLastJump");

           
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
            var rayDistance = 1.5f;
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
            if (!_hasWall)
                return false;
            if (TimesPlanted >= WallPlantSettings.MaximumWallPlants)
                return false;
            var traversePlayer = Traverse.Create(p);
            var jumpRequested = traversePlayer.Field("jumpRequested").GetValue<bool>();
            if (p.jumpButtonNew && !p.IsGrounded() && (!jumpRequested || !p.JumpIsAllowed()) && !locked)
            {
                if (_timeSinceReachedMinSpeed > WallPlantSettings.GracePeriod)
                    return false;
                Trigger(_wallPoint, _wallNormal);
                return true;
            }
            return false;
        }
    }
}
