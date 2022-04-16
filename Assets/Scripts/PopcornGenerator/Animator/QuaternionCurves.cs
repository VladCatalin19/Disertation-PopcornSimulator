using UnityEngine;

namespace PopcornGenerator
{
    internal struct QuaternionCurves
    {
        private AnimationCurve curveX;
        private AnimationCurve curveY;
        private AnimationCurve curveZ;
        private AnimationCurve curveW;

        public void AllocateCurves()
        {
            curveX = new AnimationCurve();
            curveY = new AnimationCurve();
            curveZ = new AnimationCurve();
            curveW = new AnimationCurve();
        }

        public AnimationCurve CurveX { get => curveX; }
        public AnimationCurve CurveY { get => curveY; }
        public AnimationCurve CurveZ { get => curveZ; }
        public AnimationCurve CurveW { get => curveW; }
    }
}