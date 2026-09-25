using System;
using UnityEngine;
using SquirrelGame.Player;

namespace SquirrelGame.Gameplay
{
    public class CollectibleAcorn : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float _rotationSpeed = 90f;
        [SerializeField] private float _floatFrequency = 2.5f;
        [SerializeField] private float _floatAmplitude = 0.15f;

        [Header("Effects")]
        [SerializeField] private GameObject _collectParticlePrefab;

        // Cached at Start to avoid repeated transform reads
        private float _startY;
        private float _startX;
        private float _startZ;
        private float _floatAngularSpeed; // pre-multiplied with 2π so Mathf.Sin stays cheap

        public static event Action OnAnyAcornCollected;

        private void Start()
        {
            Vector3 pos = transform.position;
            _startY = pos.y;
            _startX = pos.x;
            _startZ = pos.z;
        }

        private void Update()
        {
            // Rotate in-place (no allocation)
            transform.Rotate(0f, _rotationSpeed * Time.deltaTime, 0f, Space.World);

            // Float — only Y changes, X/Z are cached constants → no transform.position read
            float newY = _startY + Mathf.Sin(Time.time * _floatFrequency) * _floatAmplitude;
            transform.position = new Vector3(_startX, newY, _startZ);
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                Collect();
            }
        }

        private void Collect()
        {
            // Disable Update immediately — no more per-frame cost
            enabled = false;

            OnAnyAcornCollected?.Invoke();

            if (_collectParticlePrefab != null)
            {
                Instantiate(_collectParticlePrefab, transform.position, Quaternion.identity);
            }

            StartCoroutine(CollectAnimationRoutine());
        }

        private System.Collections.IEnumerator CollectAnimationRoutine()
        {
            float elapsed = 0f;
            const float duration = 0.25f;
            const float invDuration = 1f / duration;
            Vector3 initScale = transform.localScale;
            Vector3 risePerSec = new Vector3(0f, 2f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed * invDuration;
                transform.localScale = Vector3.LerpUnclamped(initScale, Vector3.zero, t);
                transform.position += risePerSec * Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
