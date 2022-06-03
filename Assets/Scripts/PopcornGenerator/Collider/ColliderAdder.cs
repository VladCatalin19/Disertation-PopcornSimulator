using System.Collections.Generic;
using System.Collections;
using UnityEngine;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace PopcornGenerator
{
    internal static class ColliderAdder
    {
        public static IEnumerator AddColliders(List<RiggedSlice> riggedSlices,
                                               List<SkinnedMeshRenderer> skinnedMeshRenderers,
                                               Collider kernelCollider,
                                               Transform kernelTransform,
                                               PopcornGeneratorProperties properties)
        {
            properties.stopwatch.Restart();
            List<Collider> colliders = new List<Collider>(riggedSlices.Count * riggedSlices[0].RiggingZonesIndices.Count);
            yield return null;
            properties.stopwatch.Restart();

            for (int sliceIndex = 0; sliceIndex < riggedSlices.Count; ++sliceIndex)
            {
                RiggedSlice riggedSlice = riggedSlices[sliceIndex];
                SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[sliceIndex];
                Slice slice = riggedSlice.Slice;

                Transform[] bones = skinnedMeshRenderer.bones;

                for (int riggingZoneIndex = 0; riggingZoneIndex < riggedSlice.RiggingZonesIndices.Count; ++riggingZoneIndex)
                {
                    if (properties.stopwatch.ElapsedTicks / 10 > properties.microsecondsToYield)
                    {
                        yield return null;
                        properties.stopwatch.Restart();
                    }

                    HashSet<int> zoneIndices = riggedSlice.RiggingZonesIndices[riggingZoneIndex];
                    Transform bone = bones[riggingZoneIndex];
                    Bounds zoneBounds = CalculateRiggedZoneBounds(slice, zoneIndices, bone, kernelTransform);

                    if (properties.stopwatch.ElapsedTicks / 10 > properties.microsecondsToYield)
                    {
                        yield return null;
                        properties.stopwatch.Restart();
                    }

                    BoxCollider boxCollider = bone.gameObject.AddComponent<BoxCollider>();
                    boxCollider.center = zoneBounds.center;
                    boxCollider.size = zoneBounds.size;
                    boxCollider.enabled = false;

                    colliders.Add(boxCollider);
                }
            }

            if (properties.stopwatch.ElapsedTicks / 10 > properties.microsecondsToYield)
            {
                yield return null;
                properties.stopwatch.Restart();
            }

            //kernelCollider.enabled = false;
            Object.Destroy(kernelCollider);
            foreach (Collider collider in colliders)
            {
                collider.enabled = true;
            }
        }

        private static Bounds CalculateRiggedZoneBounds(Slice slice, HashSet<int> zoneIndices, Transform bone,
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
