using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator
{
    internal struct RiggedSlice
    {
        private readonly Slice slice;
        private readonly IList<ISet<int>> riggingZonesIndices;
        private readonly Vector3[] bonePositions;
        private readonly BoneWeight[] boneWeights;

        public RiggedSlice(Slice slice, int riggingZones = 0, int approxIndicesPerZone = 0)
        {
            this.slice = slice;
            riggingZonesIndices = new List<ISet<int>>(riggingZones);

            for (int riggingZoneIndex = 0; riggingZoneIndex < riggingZones; ++riggingZoneIndex)
            {
                // TODO this is stupid. Why can I not create a hashset with an initial capacity
                //      even if the documentation says it is possible???
                riggingZonesIndices.Add(new HashSet<int>(new List<int>(approxIndicesPerZone)));
            }

            bonePositions = new Vector3[riggingZones];
            boneWeights = new BoneWeight[slice.Mesh.Vertices.Count];
        }

        public Slice Slice { get => slice; }
        public IList<ISet<int>> RiggingZonesIndices { get => riggingZonesIndices; }
        public Vector3[] BonePositions { get => bonePositions; }
        public BoneWeight[] BoneWeights { get => boneWeights; }
    }
}
