using UnityEngine;
using SquirrelGame.Player;

namespace SquirrelGame.Companion
{
    public class AcornCompanion : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private PlayerController _playerController;

        [Header("Socket Attachment")]
        [Tooltip("Local offset of the ring handle on the Acorn model")]
        [SerializeField] private Vector3 _handleLocalOffset = new Vector3(0f, 0.12f, 0f);
        [SerializeField] private float _carrySnapSpeed = 25f;

        [Header("Follow Settings (Free Flight)")]
        [SerializeField] private Vector3 _followOffset = new Vector3(-0.7f, 1.3f, 0.2f);
        [SerializeField] private float _followSmoothTime = 0.2f;
        [SerializeField] private float _hoverFrequency = 2.5f;
        [SerializeField] private float _hoverAmplitude = 0.08f;
        [Tooltip("Extra yaw angle towards the camera (-Z) while in idle animation")]
        [SerializeField] private float _idleCameraTurnAngle = 35f;

        private CompanionAnimator _animator;
        private Vector3 _currentVelocity;
        private bool _isCarrying;
        private float _hoverTimer;

        // Pre-cached to avoid per-frame Quaternion.Euler allocations in the hot LateUpdate path
        private static readonly Quaternion s_victoryRot    = Quaternion.Euler(0f, 180f, 0f);
        private static readonly Quaternion s_carryRotRight = Quaternion.Euler(5f,  90f, 0f);
        private static readonly Quaternion s_carryRotLeft  = Quaternion.Euler(5f, -90f, 0f);
        private float _followOffsetAbsX; // cached Mathf.Abs(_followOffset.x)

        public bool IsCarrying => _isCarrying;

        private void Awake()
        {
            _animator = GetComponent<CompanionAnimator>();
            AutoFindHandle();
            _followOffsetAbsX = Mathf.Abs(_followOffset.x);
        }

        private void AutoFindHandle()
        {
            var handle = transform.Find("acorn_handle");
            if (handle != null)
            {
                _handleLocalOffset = handle.localPosition;
            }
        }

        public void SetTarget(PlayerController player)
        {
            _playerController = player;
            _target = player != null ? player.transform : null;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
            if (target != null)
            {
                _playerController = target.GetComponent<PlayerController>();
            }
        }

        public void SetCarrying(bool carrying)
        {
            _isCarrying = carrying;
            if (_animator != null)
            {
                _animator.SetFlying(carrying);
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            if (_isCarrying)
            {
                // Milimetrik Soket Kilitleme:
                // Sincabın el pozisyonu ile palamudun altındaki halka (acorn_handle) birebir eşleştirilir.
                Quaternion targetRot = _target.forward.x >= 0 ? s_carryRotRight : s_carryRotLeft;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 20f);

                Vector3 handPos = _playerController != null
                    ? _playerController.HandWorldPosition
                    : _target.position + new Vector3(0f, 0.61f, 0.06f);

                Vector3 handleWorldOffset = transform.rotation * _handleLocalOffset;
                Vector3 targetAcornPos = handPos - handleWorldOffset;

                // Anlık ve pürüzsüz kilitlenme
                transform.position = Vector3.Lerp(transform.position, targetAcornPos, Time.deltaTime * _carrySnapSpeed);
            }
            else
            {
                _hoverTimer += Time.deltaTime * _hoverFrequency;
                float hoverY = Mathf.Sin(_hoverTimer) * _hoverAmplitude;

                bool isVictory = _playerController != null &&
                                 _playerController.CurrentState == PlayerState.Victory;

                float facingSign = _target.forward.x >= 0 ? 1f : -1f;
                Vector3 targetPos = _target.position + new Vector3(
                    -facingSign * _followOffsetAbsX,
                    _followOffset.y + hoverY,
                    _followOffset.z);

                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _currentVelocity, _followSmoothTime);

                bool isMoving = _currentVelocity.sqrMagnitude > 0.05f;

                Quaternion targetRot;
                if (isVictory)
                {
                    // Final: tam kameraya dönük — pre-cached, allocation yok
                    targetRot = s_victoryRot;
                }
                else
                {
                    float baseYaw = facingSign > 0 ? 90f : -90f;
                    float idleTurnOffset = isMoving ? 0f : (facingSign * _idleCameraTurnAngle);
                    float tiltX = hoverY * 15f;
                    targetRot = Quaternion.Euler(tiltX, baseYaw + idleTurnOffset, 0f);
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);

                if (_animator != null)
                {
                    _animator.SetFlying(isMoving);
                }
            }
        }
    }
}
