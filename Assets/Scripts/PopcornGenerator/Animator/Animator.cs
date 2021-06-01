using System.Runtime.CompilerServices;
using UnityEngine;

namespace PopcornGenerator
{
	internal static class Animator
	{
		public static void Animate(SkinnedMeshRenderer skinnedMeshRenderer)
		{
			Transform rootBone = skinnedMeshRenderer.transform;
			Transform[] bones = skinnedMeshRenderer.bones;

			for (int boneIndex = 1; boneIndex < bones.Length; ++boneIndex)
			{
				AnimateBone(bones[boneIndex], rootBone);
			}
		}
		
		private static void AnimateBone(Transform bone, Transform rootBone)
		{
			Animation animation = bone.gameObject.AddComponent<Animation>();
			AnimationCurve curveX = new AnimationCurve();
			AnimationCurve curveY = new AnimationCurve();
			AnimationCurve curveZ = new AnimationCurve();
			AnimationCurve curveW = new AnimationCurve();

			Quaternion localRotation = bone.localRotation;
			Vector3 dir = bone.InverseTransformDirection(rootBone.position - bone.position);
			Vector3 perp = Vector3.Cross(Vector3.Cross(dir, bone.up), dir).normalized;
			Quaternion desiredLocalRotation = Quaternion.FromToRotation(dir, perp);
			desiredLocalRotation = Quaternion.Slerp(
				desiredLocalRotation, localRotation, Constants.AnimatorInterpolationRatio
			);

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
			clip.wrapMode = WrapMode.PingPong;
			clip.EnsureQuaternionContinuity();
			
			
			animation.AddClip(clip, "Expansion");
			animation.Play("Expansion");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Keyframe[] GetKeyFrameArray(float initialValue, float finalValue)
		{
			return new Keyframe[]
			{
				new Keyframe(0, initialValue, 2.882769f, 2.882769f, 0f, 0.4358931f),
				new Keyframe(3, finalValue, 2.882769f, 2.882769f, 0f, 0.4358931f),
			};
		}
	}
}
