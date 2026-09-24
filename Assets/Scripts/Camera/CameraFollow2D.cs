using UnityEngine;

namespace SquirrelGame.Camera
{
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(2f, 2.2f, -8f);
        [SerializeField] private float _smoothTime = 0.2f;
        [SerializeField] private float _minY = -1f;

        private Vector3 _currentVelocity;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 targetPosition = _target.position + _offset;
            if (targetPosition.y < _minY)
            {
                targetPosition.y = _minY;
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, _smoothTime);
        }
    }
}
