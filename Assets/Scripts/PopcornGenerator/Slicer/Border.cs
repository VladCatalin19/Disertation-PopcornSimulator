using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator
{
    internal struct Border
    {
        private readonly List<int> indicesList;
        private readonly HashSet<int> indicesSet;
        private readonly List<int> intersectingPlanesIndices;

        public Border(List<int> indicesList, HashSet<int> indicesSet, List<int> intersectingPlanesIndices)
        {
            this.indicesList = indicesList;
            this.indicesSet = indicesSet;
            this.intersectingPlanesIndices = intersectingPlanesIndices;
        }

        public List<int> IndicesList { get => indicesList; }
        public HashSet<int> IndicesSet { get => indicesSet; }
        public List<int> IntersectingIndices { get => intersectingPlanesIndices; }

        public void AddIndex(int index)
        {
            indicesList.Add(index);
            indicesSet.Add(index);
        }

        public void AddIntersectingPlanesIndex(int index)
        {
            intersectingPlanesIndices.Add(index);
        }
    }
}
