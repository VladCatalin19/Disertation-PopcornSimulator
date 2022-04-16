using System.Collections.Generic;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PopcornGenerator
{
    internal static class Rigger
    {
        public static IEnumerator RigSlices(List<Slice> slices, Plane[] riggingPlanes, List<RiggedSlice> riggedSlices)
        {
            RiggedSlice riggedSlice;

            for (int sliceIndex = 0; sliceIndex < slices.Count; ++sliceIndex)
            {
                int riggingZones = riggingPlanes.Length + 1;
                int approxIndicesPerZone = slices[sliceIndex].Mesh.Vertices.Count / riggingZones;
                riggedSlice = new RiggedSlice(slices[sliceIndex], riggingZones, approxIndicesPerZone);

                IEnumerator rigSlicesEnumerator = RigSlice(riggingPlanes, riggedSlice);
                while (rigSlicesEnumerator.MoveNext())
                {
                    yield return null;
                }
                riggedSlices.Add(riggedSlice);
                yield return null;
            }
        }

        private static IEnumerator RigSlice(Plane[] riggingPlanes, RiggedSlice riggedSlice)
        {
            IEnumerator verticesZonesEnumerator = CalculateVerticesRiggingZone(riggedSlice, riggingPlanes);
            IEnumerator bonePositionsEnumerator = CalculateRiggingZonesBonePositions(riggedSlice);
            IEnumerator boneWeightsEnumerator = CalculateRiggingZonesBoneWeights(riggedSlice);
            yield return null;

            while (verticesZonesEnumerator.MoveNext())
            {
                yield return null;
            }
            while (bonePositionsEnumerator.MoveNext())
            {
                yield return null;
            }
            while (boneWeightsEnumerator.MoveNext())
            {
                yield return null;
            }
        }

        private static IEnumerator CalculateVerticesRiggingZone(RiggedSlice riggedSlice, Plane[] riggingPlanes)
        {
            for (int vertexIndex = 0; vertexIndex < riggedSlice.Slice.Mesh.Vertices.Count; ++vertexIndex)
            {
                Vector3 vertexPosition = riggedSlice.Slice.Mesh.Vertices[vertexIndex].Position;
                int vertexZone = GetVertexPositionBetweenPlanes(vertexPosition, riggingPlanes);
                riggedSlice.RiggingZonesIndices[vertexZone].Add(vertexIndex);

                if (vertexIndex % 150 == 0)
                {
                    yield return null;
                }
            }
        }

        // TODO: Array of planes is sorted by y coordinate. Binary search could be used
        private static int GetVertexPositionBetweenPlanes(Vector3 position, Plane[] riggingPlanes)
        {
            if (!IsAbovePlane(position, riggingPlanes[0]))
            {
                return 0;
            }

            for (int riggingPlaneIndex = 1; riggingPlaneIndex < riggingPlanes.Length; ++riggingPlaneIndex)
            {
                if (IsAbovePlane(position, riggingPlanes[riggingPlaneIndex - 1])
                    && !IsAbovePlane(position, riggingPlanes[riggingPlaneIndex]))
                {
                    return riggingPlaneIndex;
                }
            }

            // Point is above all planes
            return riggingPlanes.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAbovePlane(Vector3 pos, Plane plane)
        {
            return plane.GetSide(pos) == Plane.Side.up;
        }

        private static IEnumerator CalculateRiggingZonesBonePositions(RiggedSlice riggedSlice)
        {
            for (int riggingZoneIndex = 0; riggingZoneIndex < riggedSlice.RiggingZonesIndices.Count; ++riggingZoneIndex)
            {
                #if true
                Vector3 actualCenter = CalculateZoneCenter(riggedSlice.Slice.Mesh.Vertices,
                                                           riggedSlice.RiggingZonesIndices[riggingZoneIndex]);
                Vector3 kernelPartCenter = CalculateRiggingZoneBonePosition(riggedSlice, riggingZoneIndex);
                riggedSlice.BonePositions[riggingZoneIndex] = Vector3.Lerp(actualCenter, kernelPartCenter, 0.5F);
                //riggedSlice.BonePositions[riggingZoneIndex] = CalculateZoneCenter(riggedSlice.Slice.Mesh.Vertices,
                //                                                                  riggedSlice.RiggingZonesIndices[riggingZoneIndex]);
                #elif true
                riggedSlice.BonePositions[riggingZoneIndex] = CalculateRiggingZoneBonePosition(riggedSlice, riggingZoneIndex);
                #else
                List<Vertex> meshVertices = riggedSlice.Slice.Mesh.Vertices;
                HashSet<int> zoneIndices = CalculateRiggingZoneKernelSubmeshIndices(riggedSlice, riggingZoneIndex);
                Vector3 zoneCenter = CalculateZoneCenter(meshVertices, zoneIndices);
                riggedSlice.BonePositions[riggingZoneIndex] = zoneCenter;
                #endif
                if (riggingZoneIndex % 150 == 0)
                {
                    yield return null;
                }
            }
        }


        private static Vector3 CalculateRiggingZoneBonePosition(RiggedSlice riggedSlice, int zoneIndex)
        {
            List<Vertex> meshVertices = riggedSlice.Slice.Mesh.Vertices;
            HashSet<int> zoneIndices = CalculateRiggingZoneKernelSubmeshIndices(riggedSlice, zoneIndex);

            Vector3 zoneCenter = CalculateZoneCenter(meshVertices, zoneIndices);
            Vector3 zoneMeanNormal = CalculateZoneMeanNormal(riggedSlice, meshVertices, zoneIndices);
            Quaternion zoneVerticesRotation = CalculateZoneVerticesRotation(zoneMeanNormal);
            Bounds zoneBounds = CalculateZoneBounds(meshVertices, zoneIndices, zoneCenter, zoneMeanNormal, zoneVerticesRotation);
            Vector3 referencePosition = CalculateZoneReferencePosition(zoneIndex, zoneBounds);
            int closestVertexIndex = CalculateClosestVertexToReference(meshVertices, zoneIndices, zoneCenter, zoneMeanNormal,
                                                                       zoneVerticesRotation, referencePosition);

            return meshVertices[closestVertexIndex].Position;
        }

#if false
        private static Vector3 CalculateRiggingZoneBonePosition_backup(RiggedSlice riggedSlice, int zoneIndex)
        {
            IList<Vertex> meshVertices = riggedSlice.Slice.Mesh.Vertices;
            IList<Triangle> meshTriangles = riggedSlice.Slice.Mesh.SubMeshTriangles[Mesh.kernelSubmeshIndex];

            // Create copy of vertices
            ISet<int> zoneIndices = riggedSlice.RiggingZonesIndices[zoneIndex];
            IList<Vector3> vertices = new List<Vector3>(); //new Vector3[zoneIndices.Count];
            for (int vertexIndex = 0; vertexIndex < zoneIndices.Count; ++vertexIndex)
            {
                // TODO ignore puffed vertices because we set the uv coordinates to 0 when we create those
                if (meshVertices[zoneIndices[vertexIndex]].UV.Value != Vector2.zero)
                {
                    vertices.Add(meshVertices[zoneIndices[vertexIndex]].Position);
                }
                //vertices[vertexIndex] = meshVertices[zoneIndices[vertexIndex]].Position;
            }

            // Select triangle in zone
            var indicesHashSet = new HashSet<int>(zoneIndices);
            int approxNumOfTriangles = meshTriangles.Count / riggedSlice.RiggingZonesIndices.Count;
            IList<Triangle> triangles = new List<Triangle>(approxNumOfTriangles);
            for (int triangleIndex = 0; triangleIndex < meshTriangles.Count; ++triangleIndex)
            {
                Triangle t = meshTriangles[triangleIndex];
                if (indicesHashSet.Contains(t.I0) && indicesHashSet.Contains(t.I1) && indicesHashSet.Contains(t.I2)
                    // TODO ignore puffed vertices because we set the uv coordinates to 0 when we create those
                    //&& meshVertices[t.I0].UV != Vector2.zero
                    //&& meshVertices[t.I1].UV != Vector2.zero
                    //&& meshVertices[t.I2].UV != Vector2.zero
                )
                {
                    triangles.Add(t);
                }
            }

            // Calculate center of gravity
            Vector3 center = Vector3.zero;
            for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
            {
                center += vertices[vertexIndex];
            }
            center /= vertices.Count;

            // Calculate mean normal
            Vector3 meanNormal = Vector3.zero;
            for (int triangleIndex = 0; triangleIndex < triangles.Count; ++triangleIndex)
            {
                Vector3 v0 = meshVertices[triangles[triangleIndex].I0].Position;
                Vector3 v1 = meshVertices[triangles[triangleIndex].I1].Position;
                Vector3 v2 = meshVertices[triangles[triangleIndex].I2].Position;

                meanNormal += Vector3.Cross(v1 - v0, v2 - v0);
            }
            meanNormal /= triangles.Count;

            // Translate vertices to origin
            for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
            {
                vertices[vertexIndex] -= center;
            }

            // Project vertices on plane
            for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
            {
                vertices[vertexIndex] = Vector3.ProjectOnPlane(vertices[vertexIndex], meanNormal);
            }

            // Rotate vertices
            Vector3 dir = Vector3.Angle(meanNormal, Vector3.forward) < Vector3.Angle(meanNormal, Vector3.back)
                ? Vector3.forward : Vector3.back;
            Quaternion q = Quaternion.FromToRotation(meanNormal, dir);
            for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
            {
                vertices[vertexIndex] = q * vertices[vertexIndex];
            }

            // Calculate bounds
            Bounds bounds = new Bounds();
            for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
            {
                bounds.Encapsulate(vertices[vertexIndex]);
            }

            Vector3 reference = new Vector3(
                bounds.center.x,
                zoneIndex == 0 ? bounds.center.y : bounds.min.y,
                bounds.center.z
            );

            // Find closest vertex to reference
            float sqrMinDinst = float.MaxValue;
            int closestVertexIndex = -1;
            for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
            {
                float sqrDist = (vertices[vertexIndex] - reference).sqrMagnitude;
                if (sqrDist < sqrMinDinst)
                {
                    sqrMinDinst = sqrDist;
                    closestVertexIndex = zoneIndices[vertexIndex];
                }
            }
            return meshVertices[closestVertexIndex].Position;

#if false
            // TODO use a mesh parametrisation instead of uv coordinates
            if (!riggedSlice.Slice.Mesh.HasUVs)
            {
                throw new System.NotImplementedException("Rigger does not support meshes without UV coordinates");
            }

            // Calculate uv coordinates bounds
            Vector2 minCoords = float.MaxValue * Vector2.one;
            Vector2 maxCoords = float.MinValue * Vector2.one;

            foreach (int vertexIndex in riggedSlice.RiggingZonesIndices[zoneIndex])
            {
                Vector2 uv = riggedSlice.Slice.Mesh.Vertices[vertexIndex].UV.Value;
                minCoords = Vector2.Min(minCoords, uv);
                maxCoords = Vector2.Max(maxCoords, uv);
            }

            // Get closest vertex to bottom center of bounds
            Vector2 closestUVCoords = new Vector2(0.5f * (minCoords.x + maxCoords.x), minCoords.y);
            float minSqrDistance = float.MaxValue;
            int closestIndex = -1;
            foreach (int vertexIndex in riggedSlice.RiggingZonesIndices[zoneIndex])
            {
                float sqrDistance = (riggedSlice.Slice.Mesh.Vertices[vertexIndex].UV.Value - closestUVCoords).sqrMagnitude;
                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    closestIndex = vertexIndex;
                }
            }

            return riggedSlice.Slice.Mesh.Vertices[closestIndex].Position;
#endif
        }
#endif
        private static HashSet<int> CalculateRiggingZoneKernelSubmeshIndices(RiggedSlice riggedSlice, int zoneIndex)
        {
            HashSet<int> indices = new HashSet<int>();
            HashSet<int> riggingZoneIndices = riggedSlice.RiggingZonesIndices[zoneIndex];
            List<Triangle> kernelSubmeshTriangles = riggedSlice.Slice.Mesh.SubMeshTriangles[Mesh.kernelSubmeshIndex];

            void AddIndexIfInRiggingSlice(int index)
            {
                if (riggingZoneIndices.Contains(index))
                {
                    indices.Add(index);
                }
            }

            foreach (Triangle t in kernelSubmeshTriangles)
            {
                AddIndexIfInRiggingSlice(t.I0);
                AddIndexIfInRiggingSlice(t.I1);
                AddIndexIfInRiggingSlice(t.I2);
            }

            return indices;
        }

        private static List<Vector3> CalculateRiggingZoneKernelSubmeshVertices(RiggedSlice riggedSlice, HashSet<int> indices)
        {
            var vertices = new List<Vector3>(indices.Count);
            var sliceVertices = riggedSlice.Slice.Mesh.Vertices;

            foreach (int index in indices)
            {
                vertices.Add(sliceVertices[index].Position);
            }

            return vertices;
        }

        private static List<Triangle> CalculateRiggignZoneKernelSubmeshTriangles(RiggedSlice riggedSlice, HashSet<int> indices)
        {
            var triangles = new List<Triangle>();
            var kernelSubmeshTriangles = riggedSlice.Slice.Mesh.SubMeshTriangles[Mesh.kernelSubmeshIndex];

            foreach (Triangle t in kernelSubmeshTriangles)
            {
                if (indices.Contains(t.I0) && indices.Contains(t.I1) && indices.Contains(t.I2))
                {
                    triangles.Add(t);
                }
            }

            return triangles;
        }

        private static Vector3 CalculateZoneCenter(List<Vertex> meshVertices, HashSet<int> zoneIndices)
        {
            Vector3 zoneCenter = Vector3.zero;
            foreach (int index in zoneIndices)
            {
                zoneCenter += meshVertices[index].Position;
            }
            zoneCenter /= zoneIndices.Count;
            return zoneCenter;
        }

        private static Vector3 CalculateZoneMeanNormal(RiggedSlice riggedSlice, List<Vertex> meshVertices, HashSet<int> zoneIndices)
        {
            Vector3 meanNormal = Vector3.zero;
            var kernelSubmeshTriangles = riggedSlice.Slice.Mesh.SubMeshTriangles[Mesh.kernelSubmeshIndex];

            foreach (Triangle t in kernelSubmeshTriangles)
            {
                bool isTriangleInZone = (zoneIndices.Contains(t.I0) && zoneIndices.Contains(t.I1) && zoneIndices.Contains(t.I2));
                if (isTriangleInZone)
                {
                    Vector3 v0 = meshVertices[t.I0].Position;
                    Vector3 v1 = meshVertices[t.I1].Position;
                    Vector3 v2 = meshVertices[t.I2].Position;

                    meanNormal += Vector3.Cross(v1 - v0, v2 - v0);
                }
            }
            meanNormal.Normalize();
            return meanNormal;
        }

        private static Quaternion CalculateZoneVerticesRotation(Vector3 zoneMeanNormal)
        {
            Vector3 closestDirection = Vector3.Angle(zoneMeanNormal, Vector3.forward) < Vector3.Angle(zoneMeanNormal, Vector3.back)
                                       ? Vector3.forward : Vector3.back;
            Quaternion verticesRotation = Quaternion.FromToRotation(zoneMeanNormal, closestDirection);
            return verticesRotation;
        }

        private static Bounds CalculateZoneBounds(List<Vertex> meshVertices, HashSet<int> zoneIndices, Vector3 zoneCenter,
                                                  Vector3 zoneMeanNormal, Quaternion verticesRotation)
        {
            var bounds = new Bounds();
            foreach (int index in zoneIndices)
            {
                Vector3 vertex = GetVertexProjectedOnXoYPlane(meshVertices, zoneCenter, zoneMeanNormal,
                                                              verticesRotation, index);
                bounds.Encapsulate(vertex);
            }

            return bounds;
        }

        private static Vector3 GetVertexProjectedOnXoYPlane(List<Vertex> meshVertices, Vector3 zoneCenter,
                                                            Vector3 zoneMeanNormal, Quaternion zoneVerticesRotation, int index)
        {
            Vector3 vertex = meshVertices[index].Position;
            vertex -= zoneCenter; // Translate vertex to origin
            vertex = Vector3.ProjectOnPlane(vertex, zoneMeanNormal); // Project vertex on plane
            vertex = zoneVerticesRotation * vertex; // Rotate vertex
            return vertex;
        }

        private static Vector3 CalculateZoneReferencePosition(int zoneIndex, Bounds zoneBounds)
        {
            return new Vector3(zoneBounds.center.x,
                               (zoneIndex == 0) ? zoneBounds.center.y : zoneBounds.min.y,
                               zoneBounds.center.z);
        }

        private static int CalculateClosestVertexToReference(List<Vertex> meshVertices, HashSet<int> zoneIndices,
                                                             Vector3 zoneCenter, Vector3 zoneMeanNormal,
                                                             Quaternion zoneVerticesRotation, Vector3 referencePosition)
        {
            float sqrMinDistance = float.MaxValue;
            int closestVertexIndex = -1;
            foreach (int index in zoneIndices)
            {
                Vector3 vertex = GetVertexProjectedOnXoYPlane(meshVertices, zoneCenter, zoneMeanNormal,
                                                              zoneVerticesRotation, index);

                float sqrDistance = (vertex - referencePosition).sqrMagnitude;
                if (sqrDistance < sqrMinDistance)
                {
                    sqrMinDistance = sqrDistance;
                    closestVertexIndex = index;
                }
            }

            return closestVertexIndex;
        }



        // TODO use smoothing
        private static IEnumerator CalculateRiggingZonesBoneWeights(RiggedSlice riggedSlice)
        {
            //Debug.Log($"riggedSlice.RiggingZonesIndices.Count: {riggedSlice.RiggingZonesIndices.Count}, riggedSlice.BonePositions.Length: {riggedSlice.BonePositions.Length}");
            for (int riggingZoneIndex = 0; riggingZoneIndex < riggedSlice.RiggingZonesIndices.Count; ++riggingZoneIndex)
            {
                foreach (int vertexIndex in riggedSlice.RiggingZonesIndices[riggingZoneIndex])
                {
#                   if false // not use smoothing
                    riggedSlice.BoneWeights[vertexIndex] = new BoneWeight()
                    {
                        boneIndex0 = riggingZoneIndex,
                        weight0 = 1.0f
                    };
#                   else

                    if (riggingZoneIndex == 0)
                    {
                        BoneWeight boneWeight0 = new BoneWeight()
                        {
                            boneIndex0 = 0,
                            weight0 = 1.0F,
                            boneIndex1 = 1,
                            weight1 = 0.0F,
                        };
                        riggedSlice.BoneWeights[vertexIndex] = boneWeight0;
                        continue;
                    }

                    if (vertexIndex == 500)
                    {
                    }

                    Vector3 vertexPosition = riggedSlice.Slice.Mesh.Vertices[vertexIndex].Position;

                    // Find closest 3 bones
                    int minIndex0 = -1, minIndex1 = -1, minIndex2 = -1;//, minIndex3 = -1;
                    float minSqrDist0 = float.MaxValue, minSqrDist1 = float.MaxValue;
                    float minSqrDist2 = float.MaxValue;//, minSqrDist3 = float.MaxValue;

                    for (int boneIndex = 0; boneIndex < riggedSlice.BonePositions.Length; ++boneIndex)
                    {
                        Vector3 bonePosition = riggedSlice.BonePositions[boneIndex];
                        float sqrDist = (vertexPosition - bonePosition).magnitude;

                        if (sqrDist < minSqrDist0)
                        {
                            //minSqrDist3 = minSqrDist2;
                            minSqrDist2 = minSqrDist1;
                            minSqrDist1 = minSqrDist0;
                            minSqrDist0 = sqrDist;

                            //minIndex3 = minIndex2;
                            minIndex2 = minIndex1;
                            minIndex1 = minIndex0;
                            minIndex0 = boneIndex;
                        }
                        else if (sqrDist < minSqrDist1)
                        {
                            //minSqrDist3 = minSqrDist2;
                            minSqrDist2 = minSqrDist1;
                            minSqrDist1 = sqrDist;

                            //minIndex3 = minIndex2;
                            minIndex2 = minIndex1;
                            minIndex1 = boneIndex;
                        }
                        else if (sqrDist < minSqrDist2)
                        {
                            //minSqrDist3 = minSqrDist2;
                            minSqrDist2 = sqrDist;

                            //minIndex3 = minIndex2;
                            minIndex2 = boneIndex;
                        }/*
                        else
                        {
                            minSqrDist3 = sqrDist;

                            minIndex3 = boneIndex;
                        }*/
                    }

                    //float totalDist = minSqrDist0 + minSqrDist1 + minSqrDist2;// + minSqrDist3;
                    float totalDist = 1.0F / minSqrDist0 + 1.0F / minSqrDist1 + 1.0F / minSqrDist2;

                    BoneWeight boneWeight = new BoneWeight()
                    {
                        boneIndex0 = minIndex0,
                        weight0 = (1.0F / minSqrDist0) / (totalDist),
                        boneIndex1 = minIndex1,
                        weight1 = (1.0F / minSqrDist1) / (totalDist),
                        boneIndex2 = minIndex2,
                        weight2 = (1.0F / minSqrDist2) / (totalDist),
                        //boneIndex3 = minIndex3,
                        //weight3 = minSqrDist3 / totalDist,
                    };

                    riggedSlice.BoneWeights[vertexIndex] = boneWeight;

                    if (vertexIndex == 500)
                    {
                        Debug.Log($"Bone weights sum1: {boneWeight.weight0 + boneWeight.weight1 + boneWeight.weight2 + boneWeight.weight3:F5}");
                        Debug.Log($"Zone: {riggingZoneIndex}, Closest bones: {minIndex0}, {minIndex1}, {minIndex2}");
                        //Debug.Log($"Bone weights sum2: {minSqrDist0 / totalDist + minSqrDist1 / totalDist + minSqrDist2 / totalDist /*+ minSqrDist3 / totalDist*/:F5}");
                    }
                #endif
                }
                yield return null;
            }
        }
    }
}
