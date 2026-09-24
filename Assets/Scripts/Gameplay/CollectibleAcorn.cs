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

        private Vector3 _startPosition;
        private bool _isCollected;

        public static event Action OnAnyAcornCollected;

        private void Start()
        {
            _startPosition = transform.position;
        }

        private void Update()
        {
            if (_isCollected) return;

            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);
            float newY = _startPosition.y + Mathf.Sin(Time.time * _floatFrequency) * _floatAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected) return;

            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                Collect();
            }
        }

        private void Collect()
        {
            _isCollected = true;
            OnAnyAcornCollected?.Invoke();

            if (_collectParticlePrefab != null)
            {
                Instantiate(_collectParticlePrefab, transform.position, Quaternion.identity);
            }

            // Visual feedback: shrink and destroy
            StartCoroutine(CollectAnimationRoutine());
        }

        private System.Collections.IEnumerator CollectAnimationRoutine()
        {
            float elapsed = 0f;
            float duration = 0.25f;
            Vector3 initScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(initScale, Vector3.zero, t);
                transform.position += Vector3.up * (2f * Time.deltaTime);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
