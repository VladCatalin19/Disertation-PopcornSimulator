using System.Collections.Generic;
using UnityEngine;

namespace Popcorn.Animator
{
	public static class Animator
	{
		public static void Animate(GameObject gameObject)
		{
			if (!gameObject) throw new System.ArgumentNullException("gameObject");

			SkinnedMeshRenderer skinnedMeshRenderer = gameObject.GetComponent<SkinnedMeshRenderer>();
			if (!skinnedMeshRenderer)
				throw new System.ArgumentException("Provided Game Object does not have a Skinned Mesh Renderer attachet to it");
			
			if (skinnedMeshRenderer.bones == null || skinnedMeshRenderer.bones.Length == 0)
				throw new System.ArgumentException("Provided Skinned Mesh Renderer does not have bones");

			AnimateSkinnedMesh(skinnedMeshRenderer);
		}

		private static void AnimateSkinnedMesh(SkinnedMeshRenderer skinnedMeshRenderer)
		{
			AnimateRig(skinnedMeshRenderer.rootBone);		
		}

		private static void AnimateRig(Transform rootBone, int depth = 0)
		{
			foreach (Transform bone in rootBone)
			{
				AnimateBone(bone, rootBone, depth + 1);
				AnimateRig(bone, depth + 1);
			}
		}

		private static void AnimateBone(Transform bone, Transform rootBone, int depth)
		{
			Animation animation = bone.gameObject.AddComponent<Animation>();
			AnimationCurve curveX = new AnimationCurve();
			AnimationCurve curveY = new AnimationCurve();
			AnimationCurve curveZ = new AnimationCurve();
			AnimationCurve curveW = new AnimationCurve();

			Quaternion localRotation = bone.localRotation;
			Vector3 dir = bone.InverseTransformDirection(bone.position - rootBone.position);
			Quaternion q = Quaternion.Inverse(Quaternion.FromToRotation(-dir, dir)); //bone.localRotation;
			q = Quaternion.Slerp(q, localRotation, depth == 1 ? 1.0f : 0.8f);

			curveX.keys = new Keyframe[] { new Keyframe(0, localRotation.x, 2.882769f, 2.882769f, 0f, 0.4358931f), new Keyframe(3, q.x, 0.03406568f, 0.03406568f, 1f, 0f)};
			curveY.keys = new Keyframe[] { new Keyframe(0, localRotation.y, 2.882769f, 2.882769f, 0f, 0.4358931f), new Keyframe(3, q.y, 0.03406568f, 0.03406568f, 1f, 0f)};
			curveZ.keys = new Keyframe[] { new Keyframe(0, localRotation.z, 2.882769f, 2.882769f, 0f, 0.4358931f), new Keyframe(3, q.z, 0.03406568f, 0.03406568f, 1f, 0f)};
			curveW.keys = new Keyframe[] { new Keyframe(0, localRotation.w, 2.882769f, 2.882769f, 0f, 0.4358931f), new Keyframe(3, q.w, 0.03406568f, 0.03406568f, 1f, 0f)};

			AnimationClip clip = new AnimationClip();
			clip.SetCurve("", typeof(Transform), "localRotation.x", curveX);
			clip.SetCurve("", typeof(Transform), "localRotation.y", curveY);
			clip.SetCurve("", typeof(Transform), "localRotation.z", curveZ);
			clip.SetCurve("", typeof(Transform), "localRotation.w", curveW);
			clip.legacy = true;	
			clip.wrapMode = WrapMode.Loop;
			clip.EnsureQuaternionContinuity();
			
			
			animation.AddClip(clip, "Expansion");
			animation.Play("Expansion");	
		}
	}
}
