using UnityEngine;

namespace SquirrelGame.Companion
{
    [RequireComponent(typeof(Animator))]
    public class CompanionAnimator : MonoBehaviour
    {
        private Animator _animator;

        private static readonly int IsFlyingHash = Animator.StringToHash("IsFlying");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void SetFlying(bool isFlying)
        {
            if (_animator != null)
                _animator.SetBool(IsFlyingHash, isFlying);
        }
    }
}
