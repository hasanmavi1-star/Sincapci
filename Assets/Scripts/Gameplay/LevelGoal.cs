using System;
using UnityEngine;
using SquirrelGame.Player;

namespace SquirrelGame.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class LevelGoal : MonoBehaviour
    {
        public static event Action OnGoalReached;

        private bool _isReached;

        private void OnTriggerEnter(Collider other)
        {
            if (_isReached) return;

            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                _isReached = true;
                player.TriggerVictory();
                OnGoalReached?.Invoke();
            }
        }
    }
}
