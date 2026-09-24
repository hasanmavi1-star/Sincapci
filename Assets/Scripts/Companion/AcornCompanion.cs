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

        private CompanionAnimator _animator;
        private Vector3 _currentVelocity;
        private bool _isCarrying;
        private float _hoverTimer;

        public bool IsCarrying => _isCarrying;

        private void Awake()
        {
            _animator = GetComponent<CompanionAnimator>();
            AutoFindHandle();
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
                float targetFacing = _target.forward.x >= 0 ? 90f : -90f;
                Quaternion targetRot = Quaternion.Euler(5f, targetFacing, 0f);
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

                float facingSign = _target.forward.x >= 0 ? 1f : -1f;
                Vector3 dynamicOffset = new Vector3(-facingSign * Mathf.Abs(_followOffset.x), _followOffset.y + hoverY, _followOffset.z);
                Vector3 targetPos = _target.position + dynamicOffset;

                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _currentVelocity, _followSmoothTime);

                float targetYaw = facingSign > 0 ? 90f : -90f;
                Quaternion targetRot = Quaternion.Euler(hoverY * 15f, targetYaw, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);

                if (_animator != null)
                {
                    bool isMoving = _currentVelocity.sqrMagnitude > 0.05f;
                    _animator.SetFlying(isMoving);
                }
            }
        }
    }
}
