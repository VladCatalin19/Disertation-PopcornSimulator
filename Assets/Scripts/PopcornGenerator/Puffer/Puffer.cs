using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

		public static void Puff(IList<Slice> slices)
		{
			for (int sliceIndex = 0; sliceIndex < slices.Count; ++sliceIndex)
			{
				IList<Vertex> vertices = slices[sliceIndex].Mesh.Vertices;
				IList<Triangle> triangles = slices[sliceIndex].Mesh.Triangles;
				Border border = slices[sliceIndex].Border;
				int curveIndicesCount = border.IndicesList.Count / 2;
				IList<int> firstCurveIndices = new List<int>(curveIndicesCount);
				IList<int> secondCurveIndices = new List<int>(curveIndicesCount);
				bool[] visitedIndices = new bool[border.IndicesList.Count];
				int bottomIndex = MinYIndex(border.IntersectingIndices, vertices);
				int topIndex = MaxYIndex(border.IntersectingIndices, vertices);
				int currentIndex = topIndex;

				firstCurveIndices.Add(topIndex);
				secondCurveIndices.Add(topIndex);

				for (int curveIndex = 0; curveIndex < 2; ++curveIndex)
				{
					IList<int> curveIndices = curveIndex == 0 ? firstCurveIndices : secondCurveIndices;

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

				#if false
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
				#endif

				Debug.Log($"Slice {sliceIndex} First Curve: {firstCurveIndices.Count} Second Curve: {secondCurveIndices.Count} Bottom Index: {bottomIndex} Top Index: {topIndex}");

				#if DEBUG
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
				#endif

				int verticesPerSegment = 25;
				float dirPercent = 0.6f;
				float dirMaxMagniture = 0.3f;
				int[,] interpolatedVertices = new int[Mathf.Min(firstCurveIndices.Count, secondCurveIndices.Count) - 2, verticesPerSegment];

				for (int lineIndex = 0; lineIndex < interpolatedVertices.GetLength(0); ++lineIndex)
				{
					Vertex v0 = vertices[firstCurveIndices[lineIndex + 1]];
					Vertex v1 = vertices[secondCurveIndices[lineIndex + 1]];

					Vector3 dir = v1.Position - v0.Position;
					//Debug.Log($"Slice {sliceIndex} dir.magnitude: {dir.magnitude}");

					Vector3 dirToLook = -(Vector3.Lerp(v0.Normal.Value, v1.Normal.Value, 0.5f)).normalized;//Vector3.Cross(dir.normalized, Vector3.up);//
					float lineT = (float)lineIndex / (interpolatedVertices.GetLength(0) - 1);
					//Quaternion q = Quaternion.FromToRotation(Vector3.up, dirToLook);

					for (int iteration = 0; iteration < verticesPerSegment; ++iteration)
					{
						float t = (float)iteration / (verticesPerSegment - 1);

						float angle = t * Mathf.PI;

						Vertex vInterp = Vertex.LerpUnclamped(v0, v1, t);
						float positionT = Mathf.LerpUnclamped(curve.Evaluate(t), t, lineT);
						vInterp.Position = Vector3.LerpUnclamped(v0.Position, v1.Position, positionT)
							+ (Mathf.Sin(angle) + 0.0f) * Mathf.Min(dir.magnitude, dirMaxMagniture) * dirPercent * dirToLook;
						vInterp.UV = Vector2.zero;

						interpolatedVertices[lineIndex, iteration] = vertices.Count;

						vertices.Add(vInterp);
					}

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

				for (int lineIndex = 0; lineIndex < interpolatedVertices.GetLength(0) - 1; ++lineIndex)
				{
					// v0 vInterp0 vInterp1  vInterpn-1  v1
					// x-----x-----x               x-----x  Line 0
					// |\    |\    |               |\    |
					// | \   | \   |               | \   |
					// |  \  |  \  |  ..... .....  |  \  | 
					// |   \ |   \ |               |   \ |
					// |    \|    \|               |    \|
					// x-----x-----x               x-----x  Line 1
					// v0 vInterp0 vInterp1  vInterpn-1  v1

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
				}
			}

		}
		private static int MinYIndex(IList<int> indices, IList<Vertex> vertices)
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

		private static int MaxYIndex(IList<int> indices, IList<Vertex> vertices)
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
	}
}
