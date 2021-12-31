using UnityEngine;

namespace PopcornGenerator
{
    public class BoneRotator : MonoBehaviour
    {
        private Transform rootBone = null;
        private Transform[] bones = null;

        public Transform RootBone
        {
            get => rootBone;
            set => rootBone = value;
        }

        public Transform[] Bones
        {
            get => bones;
            set => bones = value;
        }

        private void Awake()
        {
        
        }

        private void LateUpdate()
        {
        
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
    }
}
