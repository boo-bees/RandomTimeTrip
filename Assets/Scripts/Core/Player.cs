using System.Threading.Tasks;
using Core.Enums;
using UnityEngine;
using Zenject;

namespace Core
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class Player : KinematicObject
    {
        public class Factory : PlaceholderFactory<Player>
        {
            public override Player Create()
            {
                var player = base.Create();
                player._collider2d.enabled = true;
                player._isControlEnabled = false;
                player.Teleport(Vector3.zero);
                player._jumpState = JumpState.Grounded;
                //TODO: set player transform to the main camera to follow it
                player.SetIsControlEnabledWithDelayAsync(true, 2000);
                return player;
            }
        }

        private const float JumpModifier = 1f;
        private const float JumpDeceleration = 0.5f;
        private const float MaxSpeed = 7;
        private const float JumpTakeOffSpeed = 7;
        private const string HorizontalAxisName = "Horizontal";
        private const string JumpButtonName = "Jump";
        private const string GroundedAnimatorState = "grounded";
        private const string VelocityXAnimatorState = "velocityX";
        private const float FlipXMarginOfError = 0.01f;
        private static readonly int GroundedAnimatorStateCached = Animator.StringToHash(GroundedAnimatorState);
        private static readonly int VelocityXAnimatorStateCached = Animator.StringToHash(VelocityXAnimatorState);

        [SerializeField] private Collider2D _collider2d;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;
        [SerializeField] private AudioClip _jumpAudio;

        private JumpState _jumpState = JumpState.Grounded;
        private Vector2 _move;
        private bool _hasJumpForceIncreasingStopped;
        private bool _hasJumpForceIncreasingStarted;
        private bool _isControlEnabled = true;

        private async void SetIsControlEnabledWithDelayAsync(bool value, int delayInMilliseconds)
        {
            await Task.Delay(delayInMilliseconds);
            _isControlEnabled = value;
        }

        public Bounds Bounds => _collider2d.bounds;

        protected override void Update()
        {
            UpdateInput();
            UpdateJumpState();
            base.Update();
        }

        protected override void ComputeVelocity()
        {
            ComputeJumpVelocity();

            FlipXInMovementDirection();

            _animator.SetBool(GroundedAnimatorStateCached, _isGrounded);
            _animator.SetFloat(VelocityXAnimatorStateCached, Mathf.Abs(_velocity.x) / MaxSpeed);

            _targetVelocity = _move * MaxSpeed;
        }

        private void FlipXInMovementDirection()
        {
            if (_move.x > FlipXMarginOfError)
            {
                _spriteRenderer.flipX = false;
                return;
            }

            if (_move.x < -FlipXMarginOfError)
            {
                _spriteRenderer.flipX = true;
            }
        }

        private void ComputeJumpVelocity()
        {
            if (_hasJumpForceIncreasingStarted && _isGrounded)
            {
                _velocity.y = JumpTakeOffSpeed * JumpModifier;
                _hasJumpForceIncreasingStarted = false;
                return;
            }

            if (!_hasJumpForceIncreasingStopped)
            {
                return;
            }

            _hasJumpForceIncreasingStopped = false;

            if (_velocity.y > 0)
            {
                _velocity.y *= JumpDeceleration;
            }
        }

        private void UpdateInput()
        {
            if (!_isControlEnabled)
            {
                _move.x = 0;
                return;
            }

            _move.x = Input.GetAxis(HorizontalAxisName);

            if (_jumpState == JumpState.Grounded && Input.GetButtonDown(JumpButtonName))
            {
                _jumpState = JumpState.PrepareToJump;
                return;
            }

            if (Input.GetButtonUp(JumpButtonName))
            {
                _hasJumpForceIncreasingStopped = true;
            }
        }

        private void UpdateJumpState()
        {
            _hasJumpForceIncreasingStarted = false;
            switch (_jumpState)
            {
                case JumpState.PrepareToJump:
                    _jumpState = JumpState.Jumping;
                    _hasJumpForceIncreasingStarted = true;
                    _hasJumpForceIncreasingStopped = false;
                    break;
                case JumpState.Jumping:
                    if (!_isGrounded)
                    {
                        if (_jumpAudio != null)
                        {
                            _audioSource.PlayOneShot(_jumpAudio);
                        }

                        _jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (_isGrounded)
                    {
                        _jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    _jumpState = JumpState.Grounded;
                    break;
            }
        }
    }
}