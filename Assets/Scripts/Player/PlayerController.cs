using System;
using UnityEngine;
using SquirrelGame.Companion;

namespace SquirrelGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement & Friction")]
        [SerializeField] private float _moveSpeed = 3.6f;
        [SerializeField] private float _glideSpeed = 3.8f;
        [SerializeField] private float _rotationSpeed = 16f;
        [SerializeField] private float _acceleration = 50f;
        [SerializeField] private float _deceleration = 65f;

        [Header("Jump & Physics")]
        [SerializeField] private float _jumpHeight = 2.4f;
        [SerializeField] private float _normalGravity = -18f;

        [Header("Glide Hold Timing")]
        [Tooltip("Space basılı tutma süresi eşiği (sn). Bu süre dolmadan palamut taşıma başlamaz.")]
        [SerializeField] private float _glideHoldThreshold = 0.22f;
        [Tooltip("Zıplama sonrası yerden ayrılma tolerans süresi (sn). Bu süre boyunca yere temas kontrol edilmez.")]
        [SerializeField] private float _takeoffGraceDuration = 0.12f;

        [Header("Hand Anchor Socket")]
        [Tooltip("Local position of hands during jump-hold animation")]
        [SerializeField] private Vector3 _handAnchorLocal = new Vector3(0f, 0.61f, 0.06f);

        [Header("References")]
        [SerializeField] private PlayerAnimator _animator;
        [SerializeField] private AcornCompanion _companion;

        private CharacterController _controller;
        private PlayerState _currentState = PlayerState.Idle;

        private Vector3 _velocity;
        private float _currentHorizontalSpeed;
        private float _horizontalInput;
        private bool _jumpRequested;
        private bool _jumpHeld;
        private bool _canGlide;

        // Hold timer
        private float _jumpHoldTimer;

        // Takeoff grace: zıplama başlangıcında kısa süre isGrounded kontrolünü atla
        private float _takeoffGraceTimer;
        private bool _justJumped;

        public PlayerState CurrentState => _currentState;
        public Vector3 HandWorldPosition => transform.TransformPoint(_handAnchorLocal);
        public event Action<PlayerState> OnStateChanged;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (_animator == null) _animator = GetComponent<PlayerAnimator>();
            if (_animator == null) _animator = GetComponentInChildren<PlayerAnimator>();
        }

        public void SetCompanion(AcornCompanion companion)
        {
            _companion = companion;
            if (_companion != null)
            {
                _companion.SetTarget(this);
            }
        }

        private void Update()
        {
            if (_currentState == PlayerState.Victory) return;

            ReadInputs();
            HandleStateTransitions();
            ApplyMovement();
        }

        private void ReadInputs()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var gamepad = UnityEngine.InputSystem.Gamepad.current;

            float h = 0f;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
            }
            if (gamepad != null)
            {
                float stickX = gamepad.leftStick.x.ReadValue();
                if (Mathf.Abs(stickX) > 0.15f) h = stickX;
                if (gamepad.dpad.left.isPressed) h -= 1f;
                if (gamepad.dpad.right.isPressed) h += 1f;
            }

            if (Mathf.Abs(_mobileHorizontal) > 0.01f)
            {
                h = _mobileHorizontal;
            }

            _horizontalInput = Mathf.Clamp(h, -1f, 1f);

            bool jumpPressed = false;
            bool jumpHeld = false;

            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                    jumpPressed = true;
                if (keyboard.spaceKey.isPressed || keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                    jumpHeld = true;
            }

            if (gamepad != null)
            {
                if (gamepad.buttonSouth.wasPressedThisFrame) jumpPressed = true;
                if (gamepad.buttonSouth.isPressed) jumpHeld = true;
            }

            if (_mobileJumpRequested)
            {
                jumpPressed = true;
                _mobileJumpRequested = false;
            }
            if (_mobileJumpHeld)
            {
                jumpHeld = true;
            }

            if (jumpPressed) _jumpRequested = true;
            _jumpHeld = jumpHeld;
#else
            _horizontalInput = Input.GetAxisRaw("Horizontal");
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                _jumpRequested = true;

            _jumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
#endif
        }

        private float _mobileHorizontal;
        private bool _mobileJumpRequested;
        private bool _mobileJumpHeld;

        public void SetMobileHorizontal(float value) => _mobileHorizontal = value;
        public void MobileJumpDown() { _mobileJumpRequested = true; _mobileJumpHeld = true; }
        public void MobileJumpUp() { _mobileJumpHeld = false; }

        private void HandleStateTransitions()
        {
            // Takeoff grace timer: zıplama anında isGrounded hâlâ true dönebiliyor,
            // bu süre boyunca yerdeymiş gibi davranmayı engelliyoruz.
            if (_takeoffGraceTimer > 0f)
            {
                _takeoffGraceTimer -= Time.deltaTime;
            }

            bool isGrounded = _controller.isGrounded && _takeoffGraceTimer <= 0f;

            if (isGrounded && !_justJumped)
            {
                _canGlide = false;
                _jumpHoldTimer = 0f;

                if (_currentState == PlayerState.Glide)
                {
                    EndGlide();
                }

                if (_jumpRequested)
                {
                    _jumpRequested = false;
                    PerformJump();
                    return;
                }

                if (Mathf.Abs(_horizontalInput) > 0.05f)
                {
                    ChangeState(PlayerState.Run);
                }
                else
                {
                    ChangeState(PlayerState.Idle);
                }
            }
            else // Havada
            {
                _justJumped = false;

                // Hold sayacını güncelle
                if (_jumpHeld)
                {
                    _jumpHoldTimer += Time.deltaTime;
                }
                else
                {
                    _jumpHoldTimer = 0f;
                }

                // Süzülme tetikleme koşulları:
                // 1) Uzun basma: _glideHoldThreshold süresince basılı tutulduysa
                // 2) Havada tekrar basma: Havadayken space'e yeni basıldıysa
                bool holdTriggered = _jumpHeld && (_jumpHoldTimer >= _glideHoldThreshold);
                bool airPressTriggered = _jumpRequested && !_controller.isGrounded;

                if (_currentState != PlayerState.Glide && _canGlide && (holdTriggered || airPressTriggered) && _companion != null)
                {
                    StartGlide();
                }
                else if (_currentState == PlayerState.Glide && !_jumpHeld)
                {
                    EndGlide();
                }
                else if (_currentState != PlayerState.Glide && _velocity.y < 0f && _takeoffGraceTimer <= 0f)
                {
                    ChangeState(PlayerState.Fall);
                }
            }

            _jumpRequested = false;
        }

        private void PerformJump()
        {
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _normalGravity);
            _canGlide = true;
            _jumpHoldTimer = 0f;
            _takeoffGraceTimer = _takeoffGraceDuration;
            _justJumped = true;
            ChangeState(PlayerState.Jump);
            if (_animator != null) _animator.TriggerJump();
        }

        private void StartGlide()
        {
            ChangeState(PlayerState.Glide);
            _velocity.y = 0f;
            if (_animator != null) _animator.SetGliding(true);
            if (_companion != null) _companion.SetCarrying(true);
        }

        private void EndGlide()
        {
            _jumpHoldTimer = 0f;
            if (_animator != null) _animator.SetGliding(false);
            if (_companion != null) _companion.SetCarrying(false);
            if (!_controller.isGrounded)
            {
                ChangeState(PlayerState.Fall);
            }
        }

        private void ApplyMovement()
        {
            bool isGliding = (_currentState == PlayerState.Glide);

            float targetSpeed = 0f;
            if (Mathf.Abs(_horizontalInput) > 0.05f)
            {
                targetSpeed = _horizontalInput * (isGliding ? _glideSpeed : _moveSpeed);
            }

            float rate = (Mathf.Abs(targetSpeed) > 0.01f) ? _acceleration : _deceleration;
            _currentHorizontalSpeed = Mathf.MoveTowards(_currentHorizontalSpeed, targetSpeed, rate * Time.deltaTime);

            Vector3 move = new Vector3(_currentHorizontalSpeed, 0f, 0f);

            if (isGliding)
            {
                _velocity.y = 0f;
            }
            else if (_controller.isGrounded && _takeoffGraceTimer <= 0f)
            {
                _velocity.y = -6f;
            }
            else
            {
                _velocity.y += _normalGravity * Time.deltaTime;
            }

            Vector3 finalMotion = (move + new Vector3(0f, _velocity.y, 0f)) * Time.deltaTime;
            _controller.Move(finalMotion);

            Vector3 pos = transform.position;
            if (Mathf.Abs(pos.z) > 0.001f)
            {
                pos.z = 0f;
                transform.position = pos;
            }

            if (Mathf.Abs(_horizontalInput) > 0.05f)
            {
                float targetYaw = _horizontalInput > 0 ? 90f : -90f;
                Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * _rotationSpeed);
            }

            if (_animator != null)
            {
                _animator.SetSpeed(Mathf.Abs(_currentHorizontalSpeed));
                _animator.SetGrounded(_controller.isGrounded && _takeoffGraceTimer <= 0f);
            }
        }

        public void TriggerVictory()
        {
            ChangeState(PlayerState.Victory);
            _velocity = Vector3.zero;
            _currentHorizontalSpeed = 0f;
            EndGlide();
            if (_animator != null)
            {
                _animator.SetSpeed(0f);
                _animator.SetGrounded(true);
                _animator.TriggerVictory();
            }

            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private void ChangeState(PlayerState newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;
            OnStateChanged?.Invoke(_currentState);
        }
    }
}
