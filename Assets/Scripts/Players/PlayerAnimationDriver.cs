using UnityEngine;

namespace UnholyTrinity.Players
{
    public class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float dampTime = 0.1f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        /// 0 = idle, 1 = run. Set it whenever movement starts or stops.
        public float TargetSpeed { get; set; }

        private void Update()
        {
            // Damped SetFloat only moves toward the target while it's called every frame.
            animator.SetFloat(SpeedHash, TargetSpeed, dampTime, Time.deltaTime);
        }
    }
}