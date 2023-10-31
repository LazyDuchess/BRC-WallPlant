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

        private bool _graffiti = false;
        private bool _graffitiPlaced = false;

        private LayerMask _wallPlantLayerMask;

        public WallPlantAbility(Player player) : base(player)
        {
            // Need to process this ability before air dashing, so we insert at the beginning.
            p.abilities.Remove(this);
            p.abilities.Insert(0, this);
            _wallPlantLayerMask = (1 << 0) | (1 << 10) | (1 << 4);
        }

        // Helper function to return WallPlantAbility from a player.
        public static WallPlantAbility Get(Player player)
        {
            foreach(var ability in player.abilities)
            {
                if (ability is WallPlantAbility)
                    return ability as WallPlantAbility;
            }
            return null;
        }

        public override void Init()
        {
            normalMovement = true;
            decc = p.stats.airDecc;
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
            var off = WallPlantSettings.MoveStyleWallOffset;
            if (p.moveStyle == MoveStyle.ON_FOOT)
                off = WallPlantSettings.ParkourWallOffset;
            p.SetPosAndRotHard(_wallPoint + _wallNormal * off, Quaternion.LookRotation(-_wallNormal, Vector3.up));
            TimesPlanted++;
            acc = 0f;
            targetSpeed = 0f;
            normalRotation = false;
            canStartGrind = false;
            canStartWallrun = false;
            _didTrick = false;
            _graffitiPlaced = false;
            _graffiti = WallPlantSettings.GraffitiPlantDefault;

            if (WallPlantSettings.RegainAirMobility)
                p.RegainAirMobility();

            SetState(State.Planting);
        }

        // Reset times planted when appropriate and calculate grace periods. This runs even if the ability isn't active.
        public void PassiveUpdate()
        {
            _hasWall = false;
            // Just doing this to integrate the trick more seamlessly into the game as it's possible to sequence break otherwise.
            if (Core.Instance.SaveManager.CurrentSaveSlot.CurrentStoryObjective == Story.ObjectiveID.EscapePoliceStation)
                locked = p.airDashAbility.locked;

            if (p.ability != this)
            {
                if (p.IsGrounded() || p.IsGrinding() || p.ability is HandplantAbility || p.ability is HeadspinAbility || p.ability is WallrunLineAbility)
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
                            if (WallPlantSettings.AbsoluteSpeed)
                            {
                                var absoluteVelocity = p.motor.velocity.magnitude;
                                if (absoluteVelocity > _speedIntoWall)
                                    _speedIntoWall = absoluteVelocity;
                            }
                            else
                            {
                                if (velocityTowardsWall > _speedIntoWall)
                                    _speedIntoWall = velocityTowardsWall;
                            }
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
            if (((1 << obj.layer) & _wallPlantLayerMask) == 0)
                return false;
            if (!obj.CompareTag("Untagged"))
                return false;
            return true;
        }

        private bool DoGraffiti()
        {
            _graffitiPlaced = true;
            var decalRay = new Ray(p.transform.position + (Vector3.up * 0.5f), p.transform.forward);
            if (!Physics.Raycast(decalRay, out RaycastHit hit, 2f, _wallPlantLayerMask, QueryTriggerInteraction.Ignore))
                return false;
            if (hit.collider.attachedRigidbody != null)
                return false;
            p.SetSpraycanState(Player.SpraycanState.SPRAY);
            p.AudioManager.PlaySfxGameplay(SfxCollectionID.GraffitiSfx, AudioClipID.Spray);
            Decal decal = Decal.Create(hit.point, -hit.normal, WallPlantSettings.GraffitiSize, _wallPlantLayerMask);
            decal.SetTexture(GraffitiDatabase.GetGraffitiTexture(p));
            decal.transform.SetParent(hit.collider.transform);
            decal.AnimateSpray();
            return true;
        }

        public override void FixedUpdateAbility()
        {
            p.SetVisualRotLocal0();

            p.anim.SetFloat(_grindDirectionHash, 0f);

            if (_state == State.PlantedOut)
            {
                // Cancel if we end up on the floor too soon.
                if (p.IsGrounded())
                {
                    p.StopCurrentAbility();
                    return;
                }

                if (p.abilityTimer > 0.4f)
                {
                    p.SetBoostpackAndFrictionEffects(BoostpackEffectMode.OFF, FrictionEffectMode.OFF);
                }

                if (p.abilityTimer > 0.25f && !_didTrick)
                {
                    var wallPlantTrickHolder = WallPlantTrickHolder.Get(p);
                    var trickName = "Wall Plant";
                    if (_graffiti)
                        trickName = "Graffiti Plant";
                    wallPlantTrickHolder.Use(trickName, 0);
                    _didTrick = true;
                }
                acc = p.stats.airAcc + 10f;
                targetSpeed = p.stats.walkSpeed + 5f;
                normalRotation = true;
                // End.
                if (p.abilityTimer > 0.5f)
                {
                    p.StopCurrentAbility();
                    return;
                }
            }
            else 
            {
                if (WallPlantSettings.EnableAlternativePlant)
                {
                    if (WallPlantSettings.GraffitiPlantDefault)
                    {
                        if (!_graffitiPlaced && _graffiti)
                        {
                            if (p.sprayButtonHeld && !WallPlantSettings.GraffitiPlantSlideButton)
                                _graffiti = false;
                            else if ((p.slideButtonHeld || p.slideButtonNew) && WallPlantSettings.GraffitiPlantSlideButton)
                                _graffiti = false;
                        }
                    }
                    else
                    {
                        if (p.abilityTimer <= HitpauseDuration && !_graffiti && !_graffitiPlaced)
                        {
                            if (p.sprayButtonHeld && !WallPlantSettings.GraffitiPlantSlideButton)
                                _graffiti = true;
                            else if ((p.slideButtonHeld || p.slideButtonNew) && WallPlantSettings.GraffitiPlantSlideButton)
                                _graffiti = true;
                        }
                    }
                }

                if (_graffiti && !_graffitiPlaced && p.abilityTimer > 0.1f)
                    _graffiti = DoGraffiti();

                if (p.abilityTimer > HitpauseDuration)
                    SetState(State.PlantedOut);
                return;
            }
        }

        private void SetState(State state)
        {
            _state = state;
            if (_state == State.Planting)
            {
                p.AudioManager.PlaySfxGameplay(SfxCollectionID.CombatSfx, AudioClipID.ShieldBlock, p.playerOneShotAudioSource);
                p.SetVelocity(Vector3.zero);
                if (p.moveStyle == MoveStyle.ON_FOOT)
                    p.PlayAnim(_parkourHash, true, true);
                else
                    p.PlayAnim(_footPlantHash, true, true);
                return;
            }

            canStartGrind = true;
            canStartWallrun = true;

            p.PlayAnim(_footPlantOutHash, true, true);

            p.PlayVoice(AudioClipID.VoiceJump);
            p.AudioManager.PlaySfxGameplay(p.moveStyle, AudioClipID.jump, p.playerOneShotAudioSource);

            p.SetBoostpackAndFrictionEffects(BoostpackEffectMode.ON, FrictionEffectMode.OFF);

            p.ringParticles.Emit(1);

            var moveInput = p.moveInput;
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

            if (WallPlantSettings.WallPlantsUntilMaxPenalty <= 0f)
                multiply = 1f;

            offWallVelocity *= multiply;
            upVelocity *= multiply;

            p.SetVelocity(_wallNormal * offWallVelocity + Vector3.up * upVelocity);

           
            if (p.IsGrounded())
            {
                p.isJumping = true;
                p.maintainSpeedJump = false;
                p.jumpConsumed = true;
                p.jumpRequested = false;
                p.jumpedThisFrame = true;
                p.timeSinceLastJump = 0f;
                p.ForceUnground(true);
            }
        }


        // Returns an appropriate surface in front of us for wall planting.
        private bool GetWallForPlant(out Vector3 point, out Vector3 normal)
        {
            point = Vector3.zero;
            normal = Vector3.zero;

            var raySeparation = 0.33f;
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
                if (!Physics.Raycast(ray, out RaycastHit hit, WallPlantSettings.RaycastDistance, ~0, QueryTriggerInteraction.Ignore))
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
            if (TimesPlanted >= WallPlantSettings.MaximumWallPlants && WallPlantSettings.MaximumWallPlants > 0)
                return false;
            if (p.jumpButtonNew && !p.IsGrounded() && (!p.jumpRequested || !p.JumpIsAllowed()) && !locked)
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
