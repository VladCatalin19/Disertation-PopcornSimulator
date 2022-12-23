using System.Collections.Generic;
using System.Collections;
using UnityEngine;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace PopcornGenerator
{
    internal static class Puffer
    {
        private static readonly AnimationCurve curve = new AnimationCurve(
            new Keyframe[]
            {
                new Keyframe(0.00000f, 0.00000f, 0.01878f, 0.01878f, 0.00000f, 0.12776f),
                new Keyframe(0.20000f, -0.35000f, -0.00690f, -0.00690f, 0.33333f, 0.33333f),
                new Keyframe(0.40000f, 0.40000f, 0.01188f, 0.01188f, 0.33333f, 0.33333f),
                new Keyframe(0.60000f, 0.60000f, -0.00160f, -0.00160f, 0.33333f, 0.33333f),
                new Keyframe(0.80000f, 1.3500f, -0.00090f, -0.00090f, 0.33333f, 0.33333f),
                new Keyframe(1.00000f, 1.00000f, 0.00000f, 0.00000f, 0.00000f, 0.00000f),
            }
        );

        public static IEnumerator Puff(List<Slice> slices, PopcornGeneratorProperties properties)
        {
            for (int sliceIndex = 0; sliceIndex < slices.Count; ++sliceIndex)
            {
                //yield return null;
                //Debug.LogError($"After initial yield");

                List<Vertex> kernelvertices = slices[sliceIndex].Mesh.Vertices;
                List<Triangle> kerneltriangles = slices[sliceIndex].Mesh.SubMeshTriangles[Mesh.kernelSubmeshIndex];
                Border border = slices[sliceIndex].Border;

                yield return null;
                //Debug.LogError($"After gathering data");

                //var (firstCurveIndices, secondCurveIndices) = GetCurvesIndices(kernelvertices, border);
                int curveIndicesCount = border.IndicesList.Count / 2;
                List<int> firstCurveIndices = new List<int>(curveIndicesCount);
                List<int> secondCurveIndices = new List<int>(curveIndicesCount);

                IEnumerator curveGenerator = GetCurvesIndices(kernelvertices, border, firstCurveIndices, secondCurveIndices,
                                                              properties.stopwatch, properties.microsecondsToYield);
                yield return null;
                //Debug.LogError($"After preparing to calculate curve indices");

                while (curveGenerator.MoveNext())
                {
                    yield return null;
                }

                //Debug.LogError($"After calculating curve indices");

#               if DEBUG && false
                    CreateCubesOnBorders(slices, sliceIndex, firstCurveIndices, secondCurveIndices);
#               endif

#               if DEBUG && false
                    PrintCurvesIndicesIfSecondHasLessThan5Indices(firstCurveIndices, secondCurveIndices);
#               endif

                int segments = Mathf.Min(firstCurveIndices.Count, secondCurveIndices.Count) - 2;
                int verticesPerSegment = 22; // TODO make configurable

                var puffVertices = new List<Vertex>(segments * verticesPerSegment);
                yield return null;
                IEnumerator puffVerticesGenerator = GeneratePuffVertices(kernelvertices, segments, verticesPerSegment,
                                                                         firstCurveIndices, secondCurveIndices, puffVertices,
                                                                         properties.stopwatch, properties.microsecondsToYield,
                                                                         properties.curveDirectionMaxMagnitude,
                                                                         properties.curveDirectionCutoffPercent);
                while (puffVerticesGenerator.MoveNext())
                {
                    yield return null;
                }
                //Debug.LogError($"After generating vertices");

                List<Triangle> puffTriangles = GeneratePuffTriangles(kernelvertices, segments, verticesPerSegment,
                                                                     puffVertices);
                yield return null;
                //Debug.LogError($"After generating triangles");
#               if false
                    yield return null;
                    RecalculateNormals(puffVertices, puffTriangles);
#               endif

                //GenerateUVCoordinates(puffVertices, puffTriangles);
                IEnumerator uvGenerator = GenerateUVCoordinates(puffVertices, puffTriangles, properties.stopwatch,
                                                                properties.microsecondsToYield);
                while (uvGenerator.MoveNext())
                {
                    yield return null;
                }
                //Debug.LogError($"After generating UV coordinates");

                IEnumerator flippingTriangles = FlipTrianglesIfFacingInwards(kernelvertices, puffVertices, puffTriangles,
                                                                             properties.stopwatch, properties.microsecondsToYield);
                while (flippingTriangles.MoveNext())
                {
                    yield return null;
                }
                //Debug.LogError($"After flipping triangles");

                AddBaseIndexToTriangleIndices(puffTriangles, kernelvertices.Count);
                //yield return null;
                //Debug.LogError($"After adding base index");

                AddVerticesAndTrianglesToMesh(puffVertices, puffTriangles, slices[sliceIndex].Mesh);
                yield return null;
                //Debug.LogError($"After adding base index, vertices and triangles to mesh");
            }
        }

        private static void PrintCurvesIndicesIfSecondHasLessThan5Indices(List<int> firstCurveIndices, List<int> secondCurveIndices)
        {
            if (secondCurveIndices.Count < 5)
            {
                System.Text.StringBuilder strb = new System.Text.StringBuilder("[ ");

                foreach (int index in firstCurveIndices)
                {
                    strb.Append(index).Append(", ");
                }

                strb.Append("]");
                Debug.LogWarning($"First curve indices: {strb}");
            }
        }

        private static void CreateCubesOnBorders(List<Slice> slices, int sliceIndex, List<int> firstCurveIndices, List<int> secondCurveIndices)
        {
            float scale = 0.015f;
            GameObject borderParent = new GameObject($"Slice {sliceIndex} First Border Indices");
            //borderParent.transform.SetParent(skinnedMeshRenderers[sliceIndex].transform, false);
            foreach (int borderIndex in firstCurveIndices)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Index {borderIndex}";
                go.transform.SetParent(borderParent.transform, false);
                go.transform.localPosition = slices[sliceIndex].Mesh.Vertices[borderIndex].Position;
                go.transform.localScale = scale * Vector3.one;
                go.GetComponent<Renderer>().material.color = Color.green;
            }

            borderParent = new GameObject($"Slice {sliceIndex} Second Border Indices");
            //borderParent.transform.SetParent(skinnedMeshRenderers[sliceIndex].transform, false);
            foreach (int borderIndex in secondCurveIndices)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Index {borderIndex}";
                go.transform.SetParent(borderParent.transform, false);
                go.transform.localPosition = slices[sliceIndex].Mesh.Vertices[borderIndex].Position;
                go.transform.localScale = scale * Vector3.one;
                go.GetComponent<Renderer>().material.color = Color.magenta;
            }
        }

        private static int MinYIndex(List<int> indices, List<Vertex> vertices)
        {
            int minIndex = -1;
            float minY = float.MaxValue;

            foreach (int index in indices)
            {
                float y = vertices[index].Position.y;
                if (y < minY)
                {
                    minY = y;
                    minIndex = index;
                }
            }

            return minIndex;
        }

        private static int MaxYIndex(List<int> indices, List<Vertex> vertices)
        {
            int maxIndex = -1;
            float maxY = float.MinValue;

            foreach (int index in indices)
            {
                float y = vertices[index].Position.y;
                if (maxY < y)
                {
                    maxY = y;
                    maxIndex = index;
                }
            }

            return maxIndex;
        }

        private static IEnumerator GetCurvesIndices(List<Vertex> vertices, Border border,
                                                    List<int> firstCurveIndices, List<int> secondCurveIndices,
                                                    Stopwatch sw, long microsecondsToYield)
        {
            int curveIndicesCount = border.IndicesList.Count / 2;

            bool[] visitedIndices = new bool[border.IndicesList.Count];
            int bottomIndex = MinYIndex(border.IntersectingIndices, vertices);
            int topIndex = MaxYIndex(border.IntersectingIndices, vertices);
            int currentIndex;

            firstCurveIndices.Add(topIndex);
            secondCurveIndices.Add(topIndex);
            
            yield return null;
            sw.Restart();

            for (int curveIndex = 0; curveIndex < 2; ++curveIndex)
            {
                List<int> curveIndices = curveIndex == 0 ? firstCurveIndices : secondCurveIndices;

                currentIndex = topIndex;
                int maxSteps = Mathf.RoundToInt(curveIndicesCount * 1.3f);
                int step = 0;
                while (currentIndex != bottomIndex && step++ < maxSteps)
                {
                    // Find closest unvisited index
                    float minSqrDist = float.MaxValue;
                    int closestUnvisitedIndex = -1;
                    int closestUnvisitedIndexInVisitedIndices = -1;
                    for (int index = 0; index < border.IndicesList.Count; ++index)
                    {
                        int borderIndex = border.IndicesList[index];

                        if (borderIndex != currentIndex && !visitedIndices[index])
                        {
                            float sqrDist = (vertices[borderIndex].Position - vertices[currentIndex].Position).sqrMagnitude;
                            if (sqrDist < minSqrDist)
                            {
                                minSqrDist = sqrDist;
                                closestUnvisitedIndex = borderIndex;
                                closestUnvisitedIndexInVisitedIndices = index;
                            }
                        }
                    }

                    if (sw.ElapsedTicks / 10 > microsecondsToYield)
                    {
                        yield return null;
                        sw.Restart();
                        //Debug.LogError($"After ElapsedTicks");
                    }

                    // Make sure top index is not visited so that in the next iteration it will be added
                    // in the list again
                    if (closestUnvisitedIndex != bottomIndex)
                    {
                        visitedIndices[closestUnvisitedIndexInVisitedIndices] = true;
                    }
                    curveIndices.Add(closestUnvisitedIndex);
                    currentIndex = closestUnvisitedIndex;
                }
                yield return null;
            }
        }

        #if false  // GetCurvesIndices sequencial
        private static (List<int>, List<int>) GetCurvesIndices(List<Vertex> vertices, Border border)
        {
            int curveIndicesCount = border.IndicesList.Count / 2;
            List<int> firstCurveIndices = new List<int>(curveIndicesCount);
            List<int> secondCurveIndices = new List<int>(curveIndicesCount);

            bool[] visitedIndices = new bool[border.IndicesList.Count];
            int bottomIndex = MinYIndex(border.IntersectingIndices, vertices);
            int topIndex = MaxYIndex(border.IntersectingIndices, vertices);
            int currentIndex;

            firstCurveIndices.Add(topIndex);
            secondCurveIndices.Add(topIndex);

            for (int curveIndex = 0; curveIndex < 2; ++curveIndex)
            {
                List<int> curveIndices = curveIndex == 0 ? firstCurveIndices : secondCurveIndices;

                currentIndex = topIndex;
                int maxSteps = Mathf.RoundToInt(curveIndicesCount * 1.3f);
                while (currentIndex != bottomIndex && maxSteps-- > 0)
                {
                    // Find closest unvisited index
                    float minSqrDist = float.MaxValue;
                    int closestUnvisitedIndex = -1;
                    int closestUnvisitedIndexInVisitedIndices = -1;
                    for (int index = 0; index < border.IndicesList.Count; ++index)
                    {
                        int borderIndex = border.IndicesList[index];

                        if (borderIndex != currentIndex && !visitedIndices[index])
                        {
                            float sqrDist = (vertices[borderIndex].Position - vertices[currentIndex].Position).sqrMagnitude;
                            if (sqrDist < minSqrDist)
                            {
                                minSqrDist = sqrDist;
                                closestUnvisitedIndex = borderIndex;
                                closestUnvisitedIndexInVisitedIndices = index;
                            }
                        }
                    }

                    // Make sure top index is not visited so that in the next iteration it will be added
                    // in the list again
                    if (closestUnvisitedIndex != bottomIndex)
                    {
                        visitedIndices[closestUnvisitedIndexInVisitedIndices] = true;
                    }
                    curveIndices.Add(closestUnvisitedIndex);
                    currentIndex = closestUnvisitedIndex;
                }
            }

            return (firstCurveIndices, secondCurveIndices);
        }
        #endif

        private static IEnumerator GeneratePuffVertices(List<Vertex> vertices,
                                                        int segments,
                                                        int verticesPerSegment,
                                                        List<int> firstCurveIndices,
                                                        List<int> secondCurveIndices,
                                                        List<Vertex> puffVertices,
                                                        Stopwatch sw, long microsecondsToYield,
                                                        float curveDirectionMaxMagnitude, float curveDirectionCutoffPercent)
        {
            sw.Restart();
            //float dirPercent = 0.9f;
            //float dirMaxMagniture = 3.0f;

            for (int lineIndex = 0; lineIndex < segments; ++lineIndex)
            {
                Vertex v0 = vertices[firstCurveIndices[lineIndex + 1]];
                Vertex v1 = vertices[secondCurveIndices[lineIndex + 1]];

                Vector3 dir = v1.Position - v0.Position;

                Vector3 dirToLook = -Vector3.Lerp(v0.Normal, v1.Normal, 0.5f).normalized;
                float lineT = (float)lineIndex / (segments - 1);

                puffVertices.Add(v0);

                for (int iteration = 0; iteration < verticesPerSegment - 2; ++iteration)
                {
                    float t = (float)iteration / (verticesPerSegment - 3);

                    float angle = t * Mathf.PI;

                    Vertex vInterp = Vertex.LerpUnclamped(v0, v1, t);
                    float positionT = Mathf.LerpUnclamped(curve.Evaluate(t), t, lineT);

                    vInterp.Position = Vector3.LerpUnclamped(v0.Position, v1.Position, positionT)
                        + Mathf.Sin(angle) * Mathf.Min(dir.magnitude, curveDirectionMaxMagnitude) * curveDirectionCutoffPercent * dirToLook;

                    puffVertices.Add(vInterp);

                    if (sw.ElapsedTicks / 10 > microsecondsToYield)
                    {
                        yield return null;
                        sw.Restart();
                    }
                }

                puffVertices.Add(v1);
            }
        }

        #if false // GeneratePuffVertices sequencial
        private static List<Vertex> GeneratePuffVertices(List<Vertex> vertices,
                                                         int segments,
                                                         int verticesPerSegment,
                                                         List<int> firstCurveIndices,
                                                         List<int> secondCurveIndices)
        {
            float dirPercent = 0.9f;
            float dirMaxMagniture = 3.0f;

            //int[,] interpolatedVertices = new int[segments, verticesPerSegment];
            List<Vertex> puffVertices = new List<Vertex>(segments * verticesPerSegment);

            for (int lineIndex = 0; lineIndex < segments; ++lineIndex)
            {
                Vertex v0 = vertices[firstCurveIndices[lineIndex + 1]];
                Vertex v1 = vertices[secondCurveIndices[lineIndex + 1]];

                Vector3 dir = v1.Position - v0.Position;
                //Debug.Log($"Slice {sliceIndex} dir.magnitude: {dir.magnitude}");

                Vector3 dirToLook = -Vector3.Lerp(v0.Normal, v1.Normal, 0.5f).normalized;//Vector3.Cross(dir.normalized, Vector3.up);//
                float lineT = (float)lineIndex / (segments - 1);
                //Quaternion q = Quaternion.FromToRotation(Vector3.up, dirToLook);

                puffVertices.Add(v0);

                for (int iteration = 0; iteration < verticesPerSegment - 2; ++iteration)
                {
                    float t = (float)iteration / (verticesPerSegment - 3);

                    float angle = t * Mathf.PI;

                    Vertex vInterp = Vertex.LerpUnclamped(v0, v1, t);
                    float positionT = Mathf.LerpUnclamped(curve.Evaluate(t), t, lineT);
                    vInterp.Position = Vector3.LerpUnclamped(v0.Position, v1.Position, positionT)
                        + (Mathf.Sin(angle) + 0.0f) * Mathf.Min(dir.magnitude, dirMaxMagniture) * dirPercent * dirToLook;

                    //interpolatedVertices[lineIndex, iteration] = vertices.Count;

                    puffVertices.Add(vInterp);
                }

                puffVertices.Add(v1);

                #if false
                    interpolatedVertices[interpolatedIndex] = Vertex.Lerp(
                        vertices[firstCurveIndices[interpolatedIndex + 1]],
                        vertices[secondCurveIndices[interpolatedIndex + 1]],
                        0.5f
                    );
                    Vector3 diff = vertices[firstCurveIndices[interpolatedIndex + 1]].Position - vertices[secondCurveIndices[interpolatedIndex + 1]].Position;
                    interpolatedVertices[interpolatedIndex].Position += diff.magnitude * 0.4f * Vector3.Cross(
                        diff.normalized,
                        Vector3.up
                    );
                    interpolatedVertices[interpolatedIndex].UV = Vector2.zero;
                    vertices.Add(interpolatedVertices[interpolatedIndex]);
                #endif
            }

            return puffVertices;
        }
        #endif

        private static bool ShouldFlipTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 meanNormal)
        {
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            return Vector3.Angle(normal, meanNormal) > 90.0F;
        }

        private static List<Triangle> GeneratePuffTriangles(List<Vertex> vertices,
                                                            int segments,
                                                            int verticesPerSegment,
                                                            List<Vertex> puffVertices)
        {
            int trianglesNumber = (segments - 1) * (verticesPerSegment - 1) * 2;
            List<Triangle> puffTriangles = new List<Triangle>(trianglesNumber);

            // Fill hole at the top of the slice. It is kind of hard to explain where
            // these holes form. If you are curious, disable this code and look at the
            // top part of the slice. There should be a small hole.
            for (int vertexIndex = 1; vertexIndex < verticesPerSegment - 1; ++vertexIndex)
            {
                puffTriangles.Add(new Triangle(0, vertexIndex, vertexIndex + 1));
            }

            // Hole at the bottom should not be visible, so we don't fill it

            for (int lineIndex = 0; lineIndex < segments - 1; ++lineIndex)
            {
                // v10  v11   v12            v1n-1  v1n 
                // x-----x-----x               x-----x  Line 1
                // |\    |\    |               |\    |
                // | \   | \   |               | \   |
                // |  \  |  \  |  ..... .....  |  \  | 
                // |   \ |   \ |               |   \ |
                // |    \|    \|               |    \|
                // x-----x-----x               x-----x  Line 0
                // v00  v01   v02            v0n-1  v0n

                //Vector3 v0Normal = puffVertices[lineIndex * verticesPerSegment].Normal.Value;
                //Vector3 vnNormal = puffVertices[(lineIndex + 1) * verticesPerSegment - 1].Normal.Value;;
                //Vector3 meanNormal = -Vector3.Lerp(v0Normal, vnNormal, 0.5f).normalized;

                for (int vertexIndex = 0; vertexIndex < verticesPerSegment - 1; ++vertexIndex)
                {
                    int i00 = lineIndex * verticesPerSegment + vertexIndex;
                    int i01 = lineIndex * verticesPerSegment + vertexIndex + 1;

                    int i10 = (lineIndex + 1) * verticesPerSegment + vertexIndex;
                    int i11 = (lineIndex + 1) * verticesPerSegment + vertexIndex + 1;

                    //Vector3 v00 = puffVertices[i00].Position;
                    //Vector3 v01 = puffVertices[i01].Position;
                    //Vector3 v10 = puffVertices[i10].Position;
                    //Vector3 v11 = puffVertices[i11].Position;

                    puffTriangles.Add(new Triangle(i00, i10, i01));
                    puffTriangles.Add(new Triangle(i10, i11, i01));

                    #if false && false && false
                    if (ShouldFlipTriangle(v00, v10, v01, meanNormal))
                    {
                        puffTriangles.Add(new Triangle(i00, i01, i10));
                    }
                    else
                    {
                        puffTriangles.Add(new Triangle(i00, i10, i01));
                    }

                    if (ShouldFlipTriangle(v10, v11, v01, meanNormal))
                    {
                        puffTriangles.Add(new Triangle(i10, i01, i11));
                    }
                    else
                    {
                        puffTriangles.Add(new Triangle(i10, i11, i01));
                    }
                    #endif
                }

                #if false && false
                int iLine0V0 = firstCurveIndices[lineIndex + 1];
                int iLine0VInterp = interpolatedVertices[lineIndex, 0];

                int iLine1V0 = firstCurveIndices[lineIndex + 2];
                int iLine1VInterp = interpolatedVertices[lineIndex + 1, 0];

                triangles.Add(new Triangle(iLine0V0, iLine1V0, iLine1VInterp));
                triangles.Add(new Triangle(iLine0V0, iLine1VInterp, iLine0VInterp));

                for (int interpIndex = 0; interpIndex < interpolatedVertices.GetLength(1) - 1; ++interpIndex)
                {
                    int iLine0VInterp0 = interpolatedVertices[lineIndex, interpIndex];
                    int iLine0VInterp1 = interpolatedVertices[lineIndex, interpIndex + 1];

                    int iLine1VInterp0 = interpolatedVertices[lineIndex + 1, interpIndex];
                    int iLine1VInterp1 = interpolatedVertices[lineIndex + 1, interpIndex + 1];

                    Vector3 n1R = (vertices[iLine0VInterp0].Normal.Value + vertices[iLine1VInterp0].Normal.Value + vertices[iLine1VInterp1].Normal.Value) / 3.0f;
                    Vector3 n2R = (vertices[iLine0VInterp0].Normal.Value + vertices[iLine1VInterp1].Normal.Value + vertices[iLine0VInterp1].Normal.Value) / 3.0f;

                    Vector3 n1C = Vector3.Cross((vertices[iLine1VInterp0].Position - vertices[iLine0VInterp0].Position).normalized, (vertices[iLine1VInterp1].Position - vertices[iLine0VInterp0].Position).normalized);
                    Vector3 n2C = Vector3.Cross((vertices[iLine1VInterp1].Position - vertices[iLine0VInterp0].Position).normalized, (vertices[iLine0VInterp1].Position - vertices[iLine0VInterp0].Position).normalized);

                    if (Vector3.Angle(n1R, n1C) > 90)
                    {
                        triangles.Add(new Triangle(iLine0VInterp0, iLine1VInterp0, iLine1VInterp1));
                    }
                    else
                    {
                        triangles.Add(new Triangle(iLine1VInterp1, iLine1VInterp0, iLine0VInterp0));
                        // FlipNormals
                        #if false
                            Vertex v = vertices[iLine1VInterp1];
                            v.Normal *= -1.0f;
                            vertices[iLine1VInterp1] = v;

                            v = vertices[iLine1VInterp0];
                            v.Normal *= -1.0f;
                            vertices[iLine1VInterp0] = v;

                            v = vertices[iLine0VInterp0];
                            v.Normal *= -1.0f;
                            vertices[iLine0VInterp0] = v;
                        #endif
                    }
                    if (Vector3.Angle(n2R, n2C) > 90)
                    {
                        triangles.Add(new Triangle(iLine0VInterp0, iLine1VInterp1, iLine0VInterp1));
                    }
                    else
                    {
                        triangles.Add(new Triangle(iLine0VInterp1, iLine1VInterp1, iLine0VInterp0));
                        // FlipNormals
                        #if false
                            Vertex v = vertices[iLine0VInterp1];
                            v.Normal *= -1.0f;
                            vertices[iLine0VInterp1] = v;

                            v = vertices[iLine1VInterp1];
                            v.Normal *= -1.0f;
                            vertices[iLine1VInterp1] = v;

                            v = vertices[iLine0VInterp0];
                            v.Normal *= -1.0f;
                            vertices[iLine0VInterp0] = v;
                        #endif
                    }
                }

                int iLine0VInterpEnd = interpolatedVertices[lineIndex, interpolatedVertices.GetLength(1) - 1];
                int iLine0V1 = secondCurveIndices[lineIndex + 1];

                int iLine1VInterpEnd = interpolatedVertices[lineIndex + 1, interpolatedVertices.GetLength(1) - 1];
                int iLine1V1 = secondCurveIndices[lineIndex + 2];


                triangles.Add(new Triangle(iLine0VInterpEnd, iLine1VInterpEnd, iLine1V1));
                triangles.Add(new Triangle(iLine0VInterpEnd, iLine1V1, iLine0V1));
                #endif
            }
            return puffTriangles;
        }

        // https://computergraphics.stackexchange.com/a/4032 - Smooth shading normals
        private static void RecalculateNormals(List<Vertex> vertices, List<Triangle> triangles)
        {
            for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
            {
                Vertex v = vertices[vertexIndex];
                v.Normal = Vector3.zero;
                vertices[vertexIndex] = v;
            }

            foreach (Triangle triangle in triangles)
            {
                int i0 = triangle.I0;
                int i1 = triangle.I1;
                int i2 = triangle.I2;

                Vertex v0 = vertices[i0];
                Vertex v1 = vertices[i1];
                Vertex v2 = vertices[i2];

                Vector3 normal = Vector3.Cross(v1.Position - v0.Position, v2.Position - v0.Position).normalized;
                v0.Normal = normal;
                v1.Normal = normal;
                v2.Normal = normal;

                vertices[i0] = v0;
                vertices[i1] = v1;
                vertices[i2] = v2;
            }

            for (int vertexIndex = 0; vertexIndex < vertices.Count; ++vertexIndex)
            {
                Vertex v = vertices[vertexIndex];
                v.Normal = v.Normal.normalized;
                vertices[vertexIndex] = v;
            }
        }

        private static IEnumerator GenerateUVCoordinates(List<Vertex> puffVertices, List<Triangle> puffTriangles, Stopwatch sw, long microsecondsToYield)
        {
            sw.Restart();
            Vector3 puffCenter = Vector3.zero;
            foreach (Vertex v in puffVertices)
            {
                puffCenter += v.Position;
            }
            puffCenter /= puffVertices.Count;

            Vector3 meanPuffNormal = Vector3.zero;
            foreach (Triangle t in puffTriangles)
            {
                Vector3 v0 = puffVertices[t.I0].Position;
                Vector3 v1 = puffVertices[t.I1].Position;
                Vector3 v2 = puffVertices[t.I2].Position;

                meanPuffNormal += Vector3.Cross(v1 - v0, v2 - v0);

                if (sw.ElapsedTicks / 10 > microsecondsToYield)
                {
                    yield return null;
                    sw.Restart();
                }
            }
            meanPuffNormal.Normalize();

            Vector3 closestDirection = Vector3.Angle(meanPuffNormal, Vector3.forward) < Vector3.Angle(meanPuffNormal, Vector3.back)
                                       ? Vector3.forward : Vector3.back;
            Quaternion verticesRotation = Quaternion.FromToRotation(meanPuffNormal, closestDirection);

            Bounds uvBounds = new Bounds();
            for (int puffVertexIndex = 0; puffVertexIndex < puffVertices.Count; ++puffVertexIndex)
            {
                Vertex vertex = puffVertices[puffVertexIndex];
                Vector3 position = vertex.Position;
                position -= puffCenter; // Translate vertex to origin
                position = Vector3.ProjectOnPlane(position, meanPuffNormal); // Project vertex on plane
                position = verticesRotation * position; // Rotate vertex

                Vector2 uv = new Vector2(position.x, position.y);
                vertex.UV = uv;
                puffVertices[puffVertexIndex] = vertex;

                uvBounds.Encapsulate((Vector3)uv);

                if (sw.ElapsedTicks / 10 > microsecondsToYield)
                {
                    yield return null;
                    sw.Restart();
                }
            }

            float factor = 0.95F / uvBounds.size.y;

            for (int puffVertexIndex = 0; puffVertexIndex < puffVertices.Count; ++puffVertexIndex)
            {
                Vertex vertex = puffVertices[puffVertexIndex];
                // the center of the whole uv coordinates is in (0, 0)
                vertex.UV *= factor;
                vertex.UV += new Vector2(0.5F, 0.5F);
                puffVertices[puffVertexIndex] = vertex;

                if (sw.ElapsedTicks / 10 > microsecondsToYield)
                {
                    yield return null;
                    sw.Restart();
                }
            }
        }

        #if false // GenerateUVCoordinates sequencial
        private static void GenerateUVCoordinates(List<Vertex> puffVertices, List<Triangle> puffTriangles)
        {
            Vector3 puffCenter = Vector3.zero;
            foreach (Vertex v in puffVertices)
            {
                puffCenter += v.Position;
            }
            puffCenter /= puffVertices.Count;

            Vector3 meanPuffNormal = Vector3.zero;
            foreach (Triangle t in puffTriangles)
            {
                Vector3 v0 = puffVertices[t.I0].Position;
                Vector3 v1 = puffVertices[t.I1].Position;
                Vector3 v2 = puffVertices[t.I2].Position;

                meanPuffNormal += Vector3.Cross(v1 - v0, v2 - v0);
            }
            meanPuffNormal.Normalize();

            Vector3 closestDirection = Vector3.Angle(meanPuffNormal, Vector3.forward) < Vector3.Angle(meanPuffNormal, Vector3.back)
                                       ? Vector3.forward : Vector3.back;
            Quaternion verticesRotation = Quaternion.FromToRotation(meanPuffNormal, closestDirection);

            Bounds uvBounds = new Bounds();
            for (int puffVertexIndex = 0; puffVertexIndex < puffVertices.Count; ++puffVertexIndex)
            {
                Vertex vertex = puffVertices[puffVertexIndex];
                Vector3 position = vertex.Position;
                position -= puffCenter; // Translate vertex to origin
                position = Vector3.ProjectOnPlane(position, meanPuffNormal); // Project vertex on plane
                position = verticesRotation * position; // Rotate vertex

                Vector2 uv = new Vector2(position.x, position.y);
                vertex.UV = uv;
                puffVertices[puffVertexIndex] = vertex;

                uvBounds.Encapsulate((Vector3)uv);
            }

            float factor = 0.95F / uvBounds.size.y;

            for (int puffVertexIndex = 0; puffVertexIndex < puffVertices.Count; ++puffVertexIndex)
            {
                Vertex vertex = puffVertices[puffVertexIndex];
                // the center of the whole uv coordinates is in (0, 0)
                vertex.UV *= factor;
                vertex.UV += new Vector2(0.5F, 0.5F);
                puffVertices[puffVertexIndex] = vertex;
            }
        }
        #endif

        private static IEnumerator FlipTrianglesIfFacingInwards(List<Vertex> kernelVertices, List<Vertex> puffVertices,
                                                                List<Triangle> puffTriangles, Stopwatch sw, long microsecondsToYield)
        {
            sw.Restart();
            Vector3 meanKernelNormal = Vector3.zero;
            foreach (Vertex v in kernelVertices)
            {
                meanKernelNormal += v.Normal;
            }
            meanKernelNormal.Normalize();

            Vector3 meanPuffNormal = Vector3.zero;
            foreach (Triangle t in puffTriangles)
            {
                Vector3 v0 = puffVertices[t.I0].Position;
                Vector3 v1 = puffVertices[t.I1].Position;
                Vector3 v2 = puffVertices[t.I2].Position;

                meanPuffNormal += Vector3.Cross(v1 - v0, v2 - v0);

                if (sw.ElapsedTicks / 10 > microsecondsToYield)
                {
                    yield return null;
                    sw.Restart();
                }
            }
            meanPuffNormal.Normalize();

            if (Vector3.Angle(meanKernelNormal, meanPuffNormal) < 90.0F)
            {
                // Flip triangles
                for (int triangleIndex = 0; triangleIndex < puffTriangles.Count; ++triangleIndex)
                {
                    Triangle triangle = puffTriangles[triangleIndex];
                    Triangle newTriangle = new Triangle(triangle.I0, triangle.I2, triangle.I1);
                    puffTriangles[triangleIndex] = newTriangle;

                    if (sw.ElapsedTicks / 10 > microsecondsToYield)
                    {
                        yield return null;
                        sw.Restart();
                    }
                }
            }
        }
        
        #if false // FlipTrianglesIfFacingInwards sequencial
        private static void FlipTrianglesIfFacingInwards(List<Vertex> kernelVertices, List<Vertex> puffVertices,
                                                         List<Triangle> puffTriangles)
        {
            Vector3 meanKernelNormal = Vector3.zero;
            foreach (Vertex v in kernelVertices)
            {
                meanKernelNormal += v.Normal;
            }
            meanKernelNormal.Normalize();

            Vector3 meanPuffNormal = Vector3.zero;
            foreach (Triangle t in puffTriangles)
            {
                Vector3 v0 = puffVertices[t.I0].Position;
                Vector3 v1 = puffVertices[t.I1].Position;
                Vector3 v2 = puffVertices[t.I2].Position;

                meanPuffNormal += Vector3.Cross(v1 - v0, v2 - v0);
            }
            meanPuffNormal.Normalize();

            if (Vector3.Angle(meanKernelNormal, meanPuffNormal) < 90.0F)
            {
                // Flip triangles
                for (int triangleIndex = 0; triangleIndex < puffTriangles.Count; ++triangleIndex)
                {
                    Triangle triangle = puffTriangles[triangleIndex];
                    Triangle newTriangle = new Triangle(triangle.I0, triangle.I2, triangle.I1);
                    puffTriangles[triangleIndex] = newTriangle;
                }
            }
        }
        #endif

        private static void AddBaseIndexToTriangleIndices(List<Triangle> triangles, int baseIndex)
        {
            for (int triangleIndex = 0; triangleIndex < triangles.Count; ++triangleIndex)
            {
                Triangle triangle = triangles[triangleIndex];
                Triangle newTriangle = new Triangle(triangle.I0 + baseIndex,
                                                    triangle.I1 + baseIndex,
                                                    triangle.I2 + baseIndex);
                triangles[triangleIndex] = newTriangle;
            }
        }

        private static void AddVerticesAndTrianglesToMesh(List<Vertex> vertices,
                                                          List<Triangle> triangles,
                                                          Mesh mesh)
        {
            foreach (Vertex vertex in vertices)
            {
                mesh.Vertices.Add(vertex);
            }
            mesh.SubMeshTriangles.Add(triangles);
        }
    }
}
