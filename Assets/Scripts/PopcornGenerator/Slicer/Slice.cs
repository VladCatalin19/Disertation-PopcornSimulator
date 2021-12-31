using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator
{
    internal struct Slice
    {
        private readonly Mesh mesh;
        private readonly Border border;

        public Slice(Mesh mesh)
        {
            this.mesh = mesh;
            border = new Border(new List<int>(), new HashSet<int>(), new List<int>());
        }

        public Mesh Mesh { get => mesh; }
        public Border Border { get => border; }
    }
}
