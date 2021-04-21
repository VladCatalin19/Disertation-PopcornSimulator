using UnityEngine;

namespace Popcorn.Rigger
{
	internal struct RiggerResult
	{
		private BoneWeight[] boneWeights;
		private Transform[] bones;
		private Matrix4x4[] bindPoses;

		public RiggerResult(BoneWeight[] boneWeights, Transform[] bones, Matrix4x4[] bindPoses)
		{
			this.boneWeights = boneWeights;
			this.bones = bones;
			this.bindPoses = bindPoses;
		}

		public BoneWeight[] BoneWeights { get => boneWeights; }
		public Transform[] Bones { get => bones; }
		public Matrix4x4[] BindPoses { get => bindPoses; }
	}
}
