using System.Collections.Generic;
using UnityEngine;

using IRiggingZonesIndicesList = System.Collections.Generic.IList<int>;
using RiggingZonesIndicesList = System.Collections.Generic.List<int>;

namespace PopcornGenerator
{
    internal struct RiggedSlice
    {
        private readonly Slice slice;
        private readonly IList<IRiggingZonesIndicesList> riggingZonesIndices;
        private readonly Vector3[] bonePositions;
        private readonly BoneWeight[] boneWeights;

        public RiggedSlice(Slice slice, int riggingZones = 0, int approxIndicesPerZone = 0)
        {
            this.slice = slice;
            riggingZonesIndices = new List<IRiggingZonesIndicesList>(riggingZones);

            for (int riggingZoneIndex = 0; riggingZoneIndex < riggingZones; ++riggingZoneIndex)
            {
                riggingZonesIndices.Add(new RiggingZonesIndicesList(approxIndicesPerZone));
            }

            bonePositions = new Vector3[riggingZones];
            boneWeights = new BoneWeight[slice.Mesh.Vertices.Count];
        }

        public Slice Slice { get => slice; }
        public IList<IRiggingZonesIndicesList> RiggingZonesIndices { get => riggingZonesIndices; }
        public Vector3[] BonePositions { get => bonePositions; }
        public BoneWeight[] BoneWeights { get => boneWeights; }
    }
}
