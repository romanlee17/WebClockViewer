using UnityEngine;

namespace WebClockViewer
{
    /// <summary>
    /// A damped spring driving one value toward its target, for the overshooting, settling motion
    /// Material 3 Expressive uses. Angle targets can be set the short way round.
    /// </summary>
    internal struct Spring
    {
        private const float Substep = 1.0f / 240.0f;
        private const float MaxDeltaTime = 0.1f;
        private const float SettleDistance = 0.001f;
        private const float SettleVelocity = 0.01f;

        private float _velocity;

        public Spring(float value)
        {
            Value = value;
            Target = value;
            _velocity = 0.0f;
        }

        public float Value { get; private set; }
        public float Target { get; private set; }
        public bool IsMoving => Value != Target || _velocity != 0.0f;

        public void Snap(float value)
        {
            Value = value;
            Target = value;
            _velocity = 0.0f;
        }

        public void SetTarget(float target)
        {
            Target = target;
        }

        /// <summary>
        /// Targets <paramref name="angle"/> in degrees along the shortest arc from the current
        /// target, so a hand going from 354° to 0° moves forward 6° instead of back 354°.
        /// </summary>
        public void SetAngleTarget(float angle)
        {
            Target += Mathf.DeltaAngle(Target, angle);
        }

        /// <summary>
        /// Advances the spring by <paramref name="deltaTime"/> and lands it on the target once it
        /// has all but stopped. Long frames are capped so a stalled tab does not fling the value.
        /// </summary>
        public void Update(float deltaTime, float frequency, float dampingRatio)
        {
            if (!IsMoving)
            {
                return;
            }

            deltaTime = Mathf.Min(deltaTime, MaxDeltaTime);
            float angularFrequency = 2.0f * Mathf.PI * frequency;
            int steps = Mathf.Max(1, Mathf.CeilToInt(deltaTime / Substep));
            float step = deltaTime / steps;

            for (int i = 0; i < steps; i++)
            {
                float acceleration = -angularFrequency * angularFrequency * (Value - Target)
                    - 2.0f * dampingRatio * angularFrequency * _velocity;
                _velocity += acceleration * step;
                Value += _velocity * step;
            }

            if (Mathf.Abs(Value - Target) <= SettleDistance && Mathf.Abs(_velocity) <= SettleVelocity)
            {
                Snap(Target);
            }
        }
    }
}
