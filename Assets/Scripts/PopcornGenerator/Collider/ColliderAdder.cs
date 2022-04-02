using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator
{
    internal static class ColliderAdder
    {
        public static void AddColliders(IList<RiggedSlice> riggedSlices,
                                        IList<SkinnedMeshRenderer> skinnedMeshRenderers,
                                        Collider kernelCollider,
                                        Transform kernelTransform)
        {
            IList<Collider> colliders = new List<Collider>(riggedSlices.Count * riggedSlices[0].RiggingZonesIndices.Count);

            for (int sliceIndex = 0; sliceIndex < riggedSlices.Count; ++sliceIndex)
            {
                RiggedSlice riggedSlice = riggedSlices[sliceIndex];
                SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[sliceIndex];
                Slice slice = riggedSlice.Slice;

                Transform[] bones = skinnedMeshRenderer.bones;

                for (int riggingZoneIndex = 0; riggingZoneIndex < riggedSlice.RiggingZonesIndices.Count; ++riggingZoneIndex)
                {
                    ISet<int> zoneIndices = riggedSlice.RiggingZonesIndices[riggingZoneIndex];
                    Transform bone = bones[riggingZoneIndex];
                    Bounds zoneBounds = CalculateRiggedZoneBounds(slice, zoneIndices, bone, kernelTransform);

                    BoxCollider boxCollider = bone.gameObject.AddComponent<BoxCollider>();
                    boxCollider.center = zoneBounds.center;
                    boxCollider.size = zoneBounds.size;
                    boxCollider.enabled = false;

                    colliders.Add(boxCollider);
                }
            }

            kernelCollider.enabled = false;
            foreach (Collider collider in colliders)
            {
                collider.enabled = true;
            }
        }

        private static Bounds CalculateRiggedZoneBounds(Slice slice, ISet<int> zoneIndices, Transform bone,
                                                        Transform kernelTransform)
        {
            Bounds bounds = new Bounds();
            foreach (int index in zoneIndices)
            {
                Vector3 kernelSpacePosition = slice.Mesh.Vertices[index].Position;
                Vector3 worldPosition = kernelTransform.TransformPoint(kernelSpacePosition);
                Vector3 boneSpacePosition = bone.InverseTransformPoint(worldPosition);
                bounds.Encapsulate(boneSpacePosition);
            }
            return bounds;
        }
    }
}
