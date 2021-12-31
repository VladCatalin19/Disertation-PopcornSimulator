using System.Collections.Generic;
using UnityEngine;

namespace PopcornGenerator
{
    internal struct Border
    {
        private readonly IList<int> indicesList;
        private readonly ISet<int> indicesSet;
        private readonly IList<int> intersectingPlanesIndices;

        public Border(IList<int> indicesList, ISet<int> indicesSet, IList<int> intersectingPlanesIndices)
        {
            this.indicesList = indicesList;
            this.indicesSet = indicesSet;
            this.intersectingPlanesIndices = intersectingPlanesIndices;
        }

        public IList<int> IndicesList { get => indicesList; }
        public ISet<int> IndicesSet { get => indicesSet; }
        public IList<int> IntersectingIndices { get => intersectingPlanesIndices; }

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
