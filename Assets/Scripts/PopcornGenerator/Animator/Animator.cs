using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator
{
    internal static class Animator
    {
        private struct QuaternionCurves
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

        public static void Animate(ICollection<SkinnedMeshRenderer> skinnedMeshRenderers, Transform centerOfKernel)
        {
            foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
            {
                Animate(smr, centerOfKernel);
            }
        }

        public static void Animate(SkinnedMeshRenderer skinnedMeshRenderer, Transform centerOfKernel)
        {
            Transform[] bones = skinnedMeshRenderer.bones;

            for (int boneIndex = 1; boneIndex < bones.Length; ++boneIndex)
            {
                AnimateBone(bones[boneIndex], bones[boneIndex - 1], centerOfKernel, boneIndex, bones.Length - 1);
            }
        }
        
        // TODO: Add random to animation, slicing, rigging?
        // Center part
        // Curved white stuff

        private static void AnimateBone(Transform bone, Transform previousBone, Transform centerOfKernel, int depth, int maxDepth)
        {
            Animation animation = bone.gameObject.AddComponent<Animation>();
            QuaternionCurves curves = new QuaternionCurves();

            Quaternion localRotation = bone.localRotation;
            Vector3 localDirectionToPreviousBone = GetLocalDirectionToPreviousBone(bone, previousBone);
            Vector3 localDirectionToCenterOfKernel = GetLocalDirectionToCenterOfKernel(bone, centerOfKernel)
                                                     + GetJitterToLocalDirectionToPreviousBone(localDirectionToPreviousBone);

            Quaternion desiredLocalRotation = GetDesiredRotation(localRotation, localDirectionToPreviousBone,
                                                                 localDirectionToCenterOfKernel, depth, maxDepth);

            //DrawDebugLines(bone, localDirectionToPreviousBone, localDirectionToCenterOfKernel, desiredLocalRotation);

            curves.AllocateCurves();
            InitializeQuaternionCuves(curves, localRotation, desiredLocalRotation);
            AnimationClip clip = CreateAnnimationClip(curves);
            StartAnnimation(animation, clip);
        }

        private static Vector3 GetLocalDirectionToPreviousBone(Transform bone, Transform previousBone)
        {
            return bone.InverseTransformDirection((previousBone.position - bone.position).normalized);
        }

        private static Vector3 GetLocalDirectionToCenterOfKernel(Transform bone, Transform centerOfKernel)
        {
            return bone.InverseTransformDirection((centerOfKernel.position - bone.position).normalized);
        }

        private static Vector3 GetJitterToLocalDirectionToPreviousBone(Vector3 localDirectionToPreviousBone)
        {
            Quaternion q = Quaternion.FromToRotation(Vector3.forward, localDirectionToPreviousBone);
            Vector3 jitter = q * (Vector3)Random.insideUnitCircle * 0.3F;
            return jitter;
        }

        private static Quaternion GetDesiredRotation(Quaternion localRotation, Vector3 localDirectionToPreviousBone,
                                                     Vector3 localDirectionToCenterOfKernel, int depth, int maxDepth)
        {
            Quaternion desiredLocalRotation = Quaternion.FromToRotation(localDirectionToPreviousBone,
                                                                        localDirectionToCenterOfKernel);
            float t = Mathf.Lerp(Constants.AnimatorInterpolationRatio, 0.5f, (float)depth / (maxDepth + 1));

            desiredLocalRotation = Quaternion.Slerp(localRotation, desiredLocalRotation, t);
            return desiredLocalRotation;
        }

        private static void DrawDebugLines(Transform bone, Vector3 localDirectionToPreviousBone,
                                           Vector3 localDirectionToCenterOfKernel, Quaternion desiredLocalRotation)
        {
            Vector3 localDirectionToPreviousBonePoint = 0.3F * bone.TransformDirection(localDirectionToPreviousBone).normalized;
            Vector3 localDirectionToCenterOfKernelPoint = 0.2F * bone.TransformDirection(localDirectionToCenterOfKernel).normalized;
            Debug.DrawLine(bone.position, localDirectionToPreviousBonePoint, Color.green, 3_600.0F);
            Debug.DrawLine(bone.position, localDirectionToCenterOfKernelPoint, Color.cyan, 3_600.0F);
            Debug.Log($"Desired local rotation: {desiredLocalRotation.eulerAngles:F5}");
        }

        private static void InitializeQuaternionCuves(QuaternionCurves curves, Quaternion localRotation,
                                                      Quaternion desiredLocalRotation)
        {
            curves.CurveX.keys = GetKeyFrameArray(localRotation.x, desiredLocalRotation.x);
            curves.CurveY.keys = GetKeyFrameArray(localRotation.y, desiredLocalRotation.y);
            curves.CurveZ.keys = GetKeyFrameArray(localRotation.z, desiredLocalRotation.z);
            curves.CurveW.keys = GetKeyFrameArray(localRotation.w, desiredLocalRotation.w);
        }

        private static Keyframe[] GetKeyFrameArray(float initialValue, float finalValue)
        {
            float endTime = Random.Range(0.5f, 4.5f);
            return new Keyframe[]
            {
                new Keyframe(0, initialValue/*, 2.882769f, 2.882769f, 0f, 0.4358931f*/),
                new Keyframe(endTime, finalValue/*, 2.882769f, 2.882769f, 0f, 0.4358931f*/),
            };
        }

        private static AnimationClip CreateAnnimationClip(QuaternionCurves curves)
        {
            AnimationClip clip = new AnimationClip();
            clip.SetCurve("", typeof(Transform), "localRotation.x", curves.CurveX);
            clip.SetCurve("", typeof(Transform), "localRotation.y", curves.CurveY);
            clip.SetCurve("", typeof(Transform), "localRotation.z", curves.CurveZ);
            clip.SetCurve("", typeof(Transform), "localRotation.w", curves.CurveW);
            clip.legacy = true;
            clip.wrapMode = WrapMode.Once;
            clip.EnsureQuaternionContinuity();
            return clip;
        }

        private static void StartAnnimation(Animation animation, AnimationClip clip)
        {
            animation.AddClip(clip, "Expansion");
            animation.Play("Expansion");
        }
    }
}
