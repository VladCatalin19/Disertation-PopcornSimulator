using System.Collections.Generic;
using System.Collections;
using UnityEngine;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace PopcornGenerator
{
    internal static class Animator
    {
        public static IEnumerator Animate(List<SkinnedMeshRenderer> skinnedMeshRenderers, Transform centerOfKernel,
                                          PopcornGeneratorProperties properties)
        {
            List<Animation> animations = new List<Animation>(skinnedMeshRenderers.Count * skinnedMeshRenderers[0].bones.Length);
            
            yield return null;
            properties.stopwatch.Restart();

            foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
            {
                Transform[] bones = smr.bones;

                for (int boneIndex = 1; boneIndex < bones.Length; ++boneIndex)
                {
                    Animation a = AnimateBone(bones[boneIndex], bones[boneIndex - 1], centerOfKernel, boneIndex, bones.Length - 1,
                                              properties.minExpansionTime, properties.maxExpansionTime, properties.animationJitter);
                    animations.Add(a);

                    if (properties.stopwatch.ElapsedTicks / 10 > properties.microsecondsToYield)
                    {
                        yield return null;
                        properties.stopwatch.Restart();
                    }
                }
            }

            foreach (Animation a in animations)
            {
                a.Play("Expansion");
            }
        }

        private static Animation AnimateBone(Transform bone, Transform previousBone, Transform centerOfKernel,
                                             int depth, int maxDepth, float minDuration, float maxDuration, float animationJitter)
        {
            Animation animation = bone.gameObject.AddComponent<Animation>();
            QuaternionCurves curves = new QuaternionCurves();

            Quaternion localRotation = bone.localRotation;
            Vector3 localDirectionToPreviousBone = GetLocalDirectionToPreviousBone(bone, previousBone);
            Vector3 localDirectionToCenterOfKernel = GetLocalDirectionToCenterOfKernel(bone, centerOfKernel)
                                                     + GetJitterToLocalDirectionToPreviousBone(localDirectionToPreviousBone, animationJitter);

            Quaternion desiredLocalRotation = GetDesiredRotation(localRotation, localDirectionToPreviousBone,
                                                                 localDirectionToCenterOfKernel, depth, maxDepth);

            //DrawDebugLines(bone, localDirectionToPreviousBone, localDirectionToCenterOfKernel, desiredLocalRotation);

            curves.AllocateCurves();
            InitializeQuaternionCuves(curves, localRotation, desiredLocalRotation, minDuration, maxDuration);
            AnimationClip clip = CreateAnnimationClip(curves);
            animation.AddClip(clip, "Expansion");

            return animation;
        }

        private static Vector3 GetLocalDirectionToPreviousBone(Transform bone, Transform previousBone)
        {
            return bone.InverseTransformDirection((previousBone.position - bone.position).normalized);
        }

        private static Vector3 GetLocalDirectionToCenterOfKernel(Transform bone, Transform centerOfKernel)
        {
            return bone.InverseTransformDirection((centerOfKernel.position - bone.position).normalized);
        }

        private static Vector3 GetJitterToLocalDirectionToPreviousBone(Vector3 localDirectionToPreviousBone, float animationJitter)
        {
            Quaternion q = Quaternion.FromToRotation(Vector3.forward, localDirectionToPreviousBone);
            Vector3 jitter = q * (Vector3)Random.insideUnitCircle * animationJitter;
            return jitter;
        }

        private static Quaternion GetDesiredRotation(Quaternion localRotation, Vector3 localDirectionToPreviousBone,
                                                     Vector3 localDirectionToCenterOfKernel, int depth, int maxDepth)
        {
            Quaternion desiredLocalRotation = Quaternion.FromToRotation(localDirectionToPreviousBone,
                                                                        localDirectionToCenterOfKernel);
            float initialBoneRotationT = 0.075F * maxDepth;
            float finalBoneRotationT = 0.05F * maxDepth;
            float t = Mathf.Lerp(initialBoneRotationT, finalBoneRotationT, (float)depth / (maxDepth + 1));

            desiredLocalRotation = Quaternion.Slerp(localRotation, desiredLocalRotation, t);
            return desiredLocalRotation;
        }

        private static void InitializeQuaternionCuves(QuaternionCurves curves, Quaternion localRotation,
                                                      Quaternion desiredLocalRotation,
                                                      float minDuration, float maxDuration)
        {
            curves.CurveX.keys = GetKeyFrameArray(localRotation.x, desiredLocalRotation.x, Random.Range(minDuration, maxDuration));
            curves.CurveY.keys = GetKeyFrameArray(localRotation.y, desiredLocalRotation.y, Random.Range(minDuration, maxDuration));
            curves.CurveZ.keys = GetKeyFrameArray(localRotation.z, desiredLocalRotation.z, Random.Range(minDuration, maxDuration));
            curves.CurveW.keys = GetKeyFrameArray(localRotation.w, desiredLocalRotation.w, Random.Range(minDuration, maxDuration));
        }

        private static Keyframe[] GetKeyFrameArray(float initialValue, float finalValue, float duration)
        {
            return new Keyframe[]
            {
                new Keyframe(0, initialValue/*, 2.882769f, 2.882769f, 0f, 0.4358931f*/),
                new Keyframe(duration, finalValue/*, 2.882769f, 2.882769f, 0f, 0.4358931f*/),
            };
        }

        private static AnimationClip CreateAnnimationClip(QuaternionCurves curves)
        {
            AnimationClip clip = new AnimationClip { legacy = true,
                                                     wrapMode = WrapMode.Once };
            clip.EnsureQuaternionContinuity();
            clip.SetCurve("", typeof(Transform), "localRotation.x", curves.CurveX);
            clip.SetCurve("", typeof(Transform), "localRotation.y", curves.CurveY);
            clip.SetCurve("", typeof(Transform), "localRotation.z", curves.CurveZ);
            clip.SetCurve("", typeof(Transform), "localRotation.w", curves.CurveW);
            return clip;
        }

#       if DEBUG
            private static void DrawDebugLines(Transform bone, Vector3 localDirectionToPreviousBone,
                                               Vector3 localDirectionToCenterOfKernel, Quaternion desiredLocalRotation)
            {
                Vector3 localDirectionToPreviousBonePoint = 0.3F * bone.TransformDirection(localDirectionToPreviousBone).normalized;
                Vector3 localDirectionToCenterOfKernelPoint = 0.2F * bone.TransformDirection(localDirectionToCenterOfKernel).normalized;
                Debug.DrawLine(bone.position, localDirectionToPreviousBonePoint, Color.green, 3_600.0F);
                Debug.DrawLine(bone.position, localDirectionToCenterOfKernelPoint, Color.cyan, 3_600.0F);
                Debug.Log($"Desired local rotation: {desiredLocalRotation.eulerAngles:F5}");
            }
#       endif
    }
}
