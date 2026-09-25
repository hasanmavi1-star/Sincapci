using UnityEngine;

namespace SquirrelGame.Camera
{
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(2f, 2.2f, -8f);
        [SerializeField] private float _smoothTime = 0.2f;
        [SerializeField] private float _minY = -1f;
        [Tooltip("SmoothDamp hız limiti (birim/sn). Ani deltaTime spike'larının kamerayı fırlatmasını önler.")]
        [SerializeField] private float _maxSpeed = 20f;

        private Vector3 _currentVelocity;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            float targetY = _target.position.y + _offset.y;
            if (targetY < _minY) targetY = _minY;

            Vector3 targetPosition = new Vector3(
                _target.position.x + _offset.x,
                targetY,
                _offset.z);           // Z sabit — allocation azaltır

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref _currentVelocity,
                _smoothTime,
                _maxSpeed);           // maxSpeed: deltaTime spike'larında kamerayı kilitleyen önlem
        }
    }
}
