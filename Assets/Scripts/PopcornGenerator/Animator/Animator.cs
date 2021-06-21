using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PopcornGenerator
{
	internal static class Animator
	{
		public static void Animate(ICollection<SkinnedMeshRenderer> skinnedMeshRenderers)
		{
			foreach (SkinnedMeshRenderer smr in skinnedMeshRenderers)
			{
				Animate(smr);
			}
		}

		public static void Animate(SkinnedMeshRenderer skinnedMeshRenderer)
		{
			Transform rootBone = skinnedMeshRenderer.transform;
			Transform[] bones = skinnedMeshRenderer.bones;

			for (int boneIndex = 1; boneIndex < bones.Length; ++boneIndex)
			{
				AnimateBone(bones[boneIndex], rootBone, boneIndex, bones.Length - 1);
			}
		}
		
		// TODO: Add random to animation, slicing, rigging?
		// Center part
		// Curved white stuff

		private static void AnimateBone(Transform bone, Transform rootBone, int depth, int maxDepth)
		{
			Animation animation = bone.gameObject.AddComponent<Animation>();
			AnimationCurve curveX = new AnimationCurve();
			AnimationCurve curveY = new AnimationCurve();
			AnimationCurve curveZ = new AnimationCurve();
			AnimationCurve curveW = new AnimationCurve();

			Quaternion localRotation = bone.localRotation;
			Vector3 dir = bone.InverseTransformDirection(rootBone.position - bone.position);
			Vector3 perp = Vector3.Cross(Vector3.Cross(dir, bone.up), dir).normalized;

			Quaternion q = Quaternion.FromToRotation(Vector3.forward, dir);
			Vector3 jitter = q * (Vector3)Random.insideUnitCircle;
			perp += 0.3f * jitter;
			
			Quaternion desiredLocalRotation = Quaternion.FromToRotation(dir, perp);
			float t = Mathf.Lerp(Constants.AnimatorInterpolationRatio, 0.5f, (float)depth / (maxDepth + 1));
			//float t = Random.Range(0.3f, Constants.AnimatorInterpolationRatio);

			desiredLocalRotation = Quaternion.Slerp(localRotation, desiredLocalRotation, t);

			curveX.keys = GetKeyFrameArray(localRotation.x, desiredLocalRotation.x);
			curveY.keys = GetKeyFrameArray(localRotation.y, desiredLocalRotation.y);
			curveZ.keys = GetKeyFrameArray(localRotation.z, desiredLocalRotation.z);
			curveW.keys = GetKeyFrameArray(localRotation.w, desiredLocalRotation.w);

			AnimationClip clip = new AnimationClip();
			clip.SetCurve("", typeof(Transform), "localRotation.x", curveX);
			clip.SetCurve("", typeof(Transform), "localRotation.y", curveY);
			clip.SetCurve("", typeof(Transform), "localRotation.z", curveZ);
			clip.SetCurve("", typeof(Transform), "localRotation.w", curveW);
			clip.legacy = true;	
			clip.wrapMode = WrapMode.Once;
			clip.EnsureQuaternionContinuity();
			
			
			animation.AddClip(clip, "Expansion");
			animation.Play("Expansion");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Keyframe[] GetKeyFrameArray(float initialValue, float finalValue)
		{
			float endTime = Random.Range(0.5f, 4.5f);
			return new Keyframe[]
			{
				new Keyframe(0, initialValue/*, 2.882769f, 2.882769f, 0f, 0.4358931f*/),
				new Keyframe(endTime, finalValue/*, 2.882769f, 2.882769f, 0f, 0.4358931f*/),
			};
		}
	}
}
