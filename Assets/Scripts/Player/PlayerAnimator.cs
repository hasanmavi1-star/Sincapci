using UnityEngine;

namespace SquirrelGame.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        private Animator _animator;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsGlidingHash = Animator.StringToHash("IsGliding");
        private static readonly int JumpTriggerHash = Animator.StringToHash("JumpTrigger");
        private static readonly int VictoryTriggerHash = Animator.StringToHash("VictoryTrigger");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void SetSpeed(float speed)
        {
            if (_animator != null)
                _animator.SetFloat(SpeedHash, speed);
        }

        public void SetGrounded(bool isGrounded)
        {
            if (_animator != null)
                _animator.SetBool(IsGroundedHash, isGrounded);
        }

        public void SetGliding(bool isGliding)
        {
            if (_animator != null)
                _animator.SetBool(IsGlidingHash, isGliding);
        }

        public void TriggerJump()
        {
            if (_animator != null)
                _animator.SetTrigger(JumpTriggerHash);
        }

        public void TriggerVictory()
        {
            if (_animator != null)
                _animator.SetTrigger(VictoryTriggerHash);
        }
    }
}
