using UnityEngine;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace PopcornGenerator
{
    [System.Serializable]
    public struct PopcornGeneratorProperties
    {
        [Header("General")]
        [HideInInspector] public Stopwatch stopwatch;
        public float minExpansionTime;// = 0.1F;
        public float maxExpansionTime;// = 0.3F;
        public long microsecondsToYield;// = 1_200L;
        [Header("Puffer")]
        public float curveDirectionMaxMagnitude;// = 0.9F;
        public float curveDirectionCutoffPercent;// = 3.0F;
        [Header("Animator")]
        public float animationJitter;// = 0.3F;
        [Header("Jumper")]
        public float jumpForceConeAngle;// = 30.0F;
        public float minJumpForce;// = 3.0F;
        public float maxJumpForce;// = 5.0F;
        public float minAngularVelocity;// = 0.01F;
        public float maxAngularVelocity;// = 0.02F;
    }
}
