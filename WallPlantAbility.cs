using HarmonyLib;
using Reptile;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WallPlant
{
    public class WallPlantAbility : Ability
    {
        public int TimesPlanted = 0;
        enum State
        {
            Planting,
            PlantedOut,
        }
        private const float MinSpeedGracePeriod = 0.1f;
        private const float MinSpeed = 5f;
        private const float MaxPlants = 1;
        private const float HitpauseDuration = 0.17f;

        private float _timeSinceReachedMinSpeed = 0f;
        private bool _special = false;
        //private int _handPlantHash = Animator.StringToHash("hitBounce");
        private int _parkourHash = Animator.StringToHash("hitBounce");
        private int _footPlantHash = Animator.StringToHash("grindRetour");
        private int _footPlantOutHash = Animator.StringToHash("poleFlip");
        private int _grindDirectionHash = Animator.StringToHash("grindDirection");
        //private int _footPlantOutHash = Animator.StringToHash("grindRetourEnd");

        private State _state = State.Planting;
        private bool _didTrick = false;
        private Vector3 _wallNormal;
        private Vector3 _wallPoint;

        public WallPlantAbility(Player player) : base(player)
        {
            var traversePlayer = Traverse.Create(p);
            var abilities = traversePlayer.Field("abilities").GetValue<List<Ability>>();
            // Need to process this before air dashing and such.
            abilities.Remove(this);
            abilities.Insert(0, this);
            Init();
        }

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
            p.SetPosAndRotHard(_wallPoint + _wallNormal * 0.8f, Quaternion.LookRotation(-_wallNormal, Vector3.up));
            TimesPlanted++;
            acc = 0f;
            targetSpeed = 0f;
            normalRotation = false;
            canStartGrind = false;
            canStartWallrun = false;
            canUseSpraycan = false;
            p.RegainAirMobility();
            _didTrick = false;
            SetState(State.Planting);
        }

        public void PassiveUpdate(Ability ability)
        {
            if (p.IsGrounded() || p.IsGrinding() || ability is HandplantAbility || ability is HeadspinAbility)
                TimesPlanted = 0;
            if (GetWallForPlant(out Vector3 _, out Vector3 wallNormal))
            {
                var velocityTowardsWall = Vector3.Dot(p.motor.velocity, -wallNormal);
                if (velocityTowardsWall >= MinSpeed)
                {
                    _timeSinceReachedMinSpeed = 0f;
                    return;
                }
            }
            _timeSinceReachedMinSpeed += Core.dt;
            if (_timeSinceReachedMinSpeed > 1000f)
                _timeSinceReachedMinSpeed = 1000f;
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
                if (p.IsGrounded())
                {
                    p.StopCurrentAbility();
                    return;
                }
                if (abilityTimer > 0.25f && !_didTrick)
                {
                    var trickName = "Sticker Slap";
                    if (!_special)
                        trickName = "Wall Plant";
                    WallPlantTrickHolder.Get(p).Use(trickName, 0);
                    _didTrick = true;
                }
                acc = stats.airAcc + 10f;
                targetSpeed = stats.walkSpeed + 5f;
                normalRotation = true;
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
                if (abilityTimer > 0.8f)
                {
                    p.StopCurrentAbility();
                    return;
                }
            }
            else if (abilityTimer > HitpauseDuration)
            {
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
                //PlaySfxGameplay(AudioClipID.land);
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
            canUseSpraycan = true;

            if (moveStyle != MoveStyle.ON_FOOT)
                p.PlayAnim(_footPlantOutHash, true, true);
            p.PlayVoice(AudioClipID.VoiceJump);
            PlaySfxGameplay(AudioClipID.jump);

            var newRot = Quaternion.LookRotation(_wallNormal, Vector3.up).eulerAngles;
            newRot.x = 0f;
            var newQuat = Quaternion.Euler(newRot);
            p.SetRotHard(newQuat);
            p.SetVisualRot(newQuat);

            var offWallVelocity = 11f;
            var upVelocity = 12f;

            if (TimesPlanted > 0)
            {
                var divide = TimesPlanted * 2;
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

        private bool GetWallForPlant(out Vector3 point, out Vector3 normal)
        {
            point = Vector3.zero;
            normal = Vector3.zero;

            var raySeparation = 0.25f;
            var rayDistance = 1f;
            var collisionSize = 0.5f;

            var rayAmount = 0;
            var setLastPoint = false;
            var lastPointAlongPlayerDirection = 0f;
            var lastPoint = Vector3.zero;
            var normalAccumulation = Vector3.zero;
            for(var i=-collisionSize;i<=collisionSize;i+=raySeparation)
            {
                rayAmount++;
                var ray = new Ray(p.transform.position + (Vector3.up * i), p.motor.dir);
                if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
                    return false;

                //var logger = Plugin.Instance.GetLogger();
                //logger.LogInfo($"Hit object {hit.collider.gameObject.name}. Layer: {hit.collider.gameObject.layer}. Tag: {hit.collider.gameObject.tag}");

                if (!hit.collider.gameObject.CompareTag("Untagged"))
                    return false;

                if (hit.collider.gameObject.layer != 0 && hit.collider.gameObject.layer != 10)
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

        
        public override bool CheckActivation()
        {
            if (TimesPlanted >= MaxPlants)
                return false;
            var traversePlayer = Traverse.Create(p);
            var jumpRequested = traversePlayer.Field("jumpRequested").GetValue<bool>();
            if (this.p.jumpButtonNew && !this.p.IsGrounded() && (!jumpRequested || !this.p.JumpIsAllowed()) && !this.locked)
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
