using System.Collections.Generic;
using System.Collections;
using UnityEngine;

using Stopwatch = System.Diagnostics.Stopwatch;

#if DEBUG
using BoneTool.Script.Runtime;
#endif

namespace PopcornGenerator
{
    internal static class MeshProcessing
    {
        public static IEnumerator RiggedSlicesToSkinnedMeshRenderers(List<RiggedSlice> riggedSlices,
                                                                     Transform kernelTransform,
                                                                     MeshRenderer kernelMeshRenderer,
                                                                     Material puffMaterial,
                                                                     List<SkinnedMeshRenderer> skinnedMeshRenderers,
                                                                     PopcornGeneratorProperties properties)
        {
            properties.stopwatch.Restart();
            for (int riggedSliceIndex = 0; riggedSliceIndex < riggedSlices.Count; ++riggedSliceIndex)
            {
                RiggedSlice riggedSlice = riggedSlices[riggedSliceIndex];
                GameObject sliceGO = CreateSliceGameObject(kernelTransform, riggedSliceIndex);
                sliceGO.layer = LayerMask.NameToLayer("Popcorn");

                //Utils.TransformVerticesToLocalSpace(riggedSlice.Slice.Mesh.Vertices, kernelTransform);
                
                if (properties.stopwatch.ElapsedTicks / 10 > properties.microsecondsToYield)
                {
                    yield return null;
                    //Debug.LogError($"Zeroth");
                    properties.stopwatch.Restart();
                }

                // Only vertices are transformed to local space. BonePositions are still
                // in world space. This function uses world space coordinates.
                Transform[] bones = CreateRiggedSliceBonesTransform(riggedSlice, sliceGO.transform);

                if (properties.stopwatch.ElapsedTicks / 10 > properties.microsecondsToYield)
                {
                    yield return null;
                    //Debug.LogError($"First");
                    properties.stopwatch.Restart();
                }

                UnityEngine.Mesh unityMesh = CreateUnityMesh(riggedSlice, riggedSliceIndex, kernelTransform, bones);

                if (properties.stopwatch.ElapsedTicks / 10 > properties.microsecondsToYield)
                {
                    yield return null;
                    //Debug.LogError($"Second");
                    properties.stopwatch.Restart();
                }

                AddMeshFilterToSliceGO(sliceGO, unityMesh);

                if (properties.stopwatch.ElapsedTicks / 10 > properties.microsecondsToYield)
                {
                    yield return null;
                    //Debug.LogError($"Third");
                    properties.stopwatch.Restart();
                }

                var skm = AddSkinnedMeshRendererToSliceGO(sliceGO, unityMesh, kernelMeshRenderer.material, puffMaterial,
                                                          bones, riggedSlice.RiggingZonesIndices, riggedSlice.Slice.Mesh.Vertices);
                skinnedMeshRenderers.Add(skm);

                if (properties.stopwatch.ElapsedTicks / 10 > properties.microsecondsToYield)
                {
                    yield return null;
                    //Debug.LogError($"Forth");
                    properties.stopwatch.Restart();
                }

                //unityMesh.RecalculateNormals();

#               if DEBUG
                    AddBoneVisualizerToSliceGO(sliceGO, bones[0]);

                    //ShowSliceBorderIndices(sliceGO, riggedSlice.Slice, colors[riggedSliceIndex % colors.Length]);
                    //ShowSliceBorderIntersectingPlanesIndices(sliceGO, riggedSlice.Slice, colors[riggedSliceIndex % colors.Length]);
                    //ShowRiggedSliceBones(sliceGO, riggedSlice, colors[riggedSliceIndex % colors.Length]);
                    //ShowRiggedSliceZoneVertices(sliceGO, riggedSlice);

                    //ExportSliceAsObj(sliceGO, riggedSliceIndex, $@"{System.IO.Directory.GetCurrentDirectory()}\RuntimeExports\Slices");

                    if (properties.stopwatch.ElapsedTicks / 10 > properties.microsecondsToYield)
                    {
                        yield return null;
                        //Debug.LogError($"Fifth");
                        properties.stopwatch.Restart();
                    }
#               endif
            }
        }

        private static GameObject CreateSliceGameObject(Transform kernelTransform, int riggedSliceIndex)
        {
            GameObject sliceGO = new GameObject($"Slice {riggedSliceIndex}");
            sliceGO.transform.SetParent(kernelTransform, false);
            return sliceGO;
        }

        private static Transform[] CreateRiggedSliceBonesTransform(RiggedSlice riggedSlice, Transform rigParent)
        {
            Transform[] bones = new Transform[riggedSlice.BonePositions.Length];

            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                Transform bone = new GameObject($"Bone {boneIndex}").transform;
                Transform prevBone = boneIndex == 0 ? rigParent : bones[boneIndex - 1];

                bone.SetParent(prevBone, false);
                bone.position = rigParent.TransformPoint(riggedSlice.BonePositions[boneIndex]);
                bone.rotation = Quaternion.identity;//Quaternion.LookRotation((prevBone.position - bone.position).normalized);
                bones[boneIndex] = bone;
            }

            return bones;
        }

        private static UnityEngine.Mesh CreateUnityMesh(RiggedSlice riggedSlice, int riggedSliceIndex,
                                                        Transform kernelTransform, Transform[] bones)
        {
            UnityEngine.Mesh unityMesh = riggedSlice.Slice.Mesh.ToUnityMesh();
            unityMesh.name = $"Slice {riggedSliceIndex} mesh";
            unityMesh.boneWeights = riggedSlice.BoneWeights;
            unityMesh.bindposes = CreateRiggedSliceBindPoses(bones, kernelTransform);
            return unityMesh;
        }

        private static Matrix4x4[] CreateRiggedSliceBindPoses(Transform[] bones, Transform kernelTransform)
        {
            Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];

            for (int bindPoseIndex = 0; bindPoseIndex < bindPoses.Length; ++bindPoseIndex)
            {
                bindPoses[bindPoseIndex] = bones[bindPoseIndex].worldToLocalMatrix * kernelTransform.localToWorldMatrix;
            }

            return bindPoses;
        }

        private static MeshFilter AddMeshFilterToSliceGO(GameObject sliceGO, UnityEngine.Mesh unityMesh)
        {
            MeshFilter sliceMeshFilter = sliceGO.AddComponent<MeshFilter>();
            sliceMeshFilter.mesh = unityMesh;
            return sliceMeshFilter;
        }

        private static SkinnedMeshRenderer AddSkinnedMeshRendererToSliceGO(GameObject sliceGO,
                                                                           UnityEngine.Mesh unityMesh,
                                                                           Material kernelMaterial,
                                                                           Material puffMaterial,
                                                                           Transform[] bones,
                                                                           List<HashSet<int>> riggingZonesIndices,
                                                                           List<Vertex> sliceVertices)
        {
            SkinnedMeshRenderer sliceSkinnedMeshRenderer = sliceGO.AddComponent<SkinnedMeshRenderer>();
            Material puffMaterialClone = Object.Instantiate(puffMaterial);

            sliceSkinnedMeshRenderer.sharedMesh = unityMesh;
            sliceSkinnedMeshRenderer.materials = new Material[] { kernelMaterial, puffMaterialClone };
            sliceSkinnedMeshRenderer.rootBone = bones[0];
            sliceSkinnedMeshRenderer.bones = bones;

            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            //foreach (Transform bone in bones)
            {
                puffMaterialClone.SetVector($"_BurnPoint{boneIndex}", Vector4.zero);
                puffMaterialClone.SetFloat($"_BurnRadius{boneIndex}", 0.0F);

                Transform bone = bones[boneIndex];
                HashSet<int> indices = riggingZonesIndices[boneIndex];

                int randomIndicesIndex = Random.Range(0, indices.Count);
                int randomIndex = -1;
                foreach (int index in indices)
                {
                    if (randomIndicesIndex == 0)
                    {
                        randomIndex = index;
                        break;
                    }
                    --randomIndicesIndex;
                }

                Vector2 uv = sliceVertices[randomIndex].UV;


                bone.gameObject.layer = LayerMask.NameToLayer("Burning");
                Burner b = bone.gameObject.AddComponent<Burner>();

                b.BurnTime = 1.0F;
                b.BurnCenter = uv;
                b.FinalBurnRadius = 0.25F;
                b.BurnPointIndex = boneIndex;
                b.PuffMaterial = puffMaterialClone;

                //Debug.Log($"Burn point {boneIndex} position: {uv:F5}");
            }

            return sliceSkinnedMeshRenderer;
        }

#       if DEBUG
            private static readonly Color[] colors = new Color[]
            {
                Color.white, Color.black, Color.red, Color.cyan,
                Color.magenta, Color.blue, Color.green, Color.grey
            };

            public static void AddBoneVisualizerToSliceGO(GameObject sliceGO, Transform rootBone)
            {
                BoneVisualiser bv = sliceGO.AddComponent<BoneVisualiser>();
                bv.RootNode = rootBone;
                bv.BoneColor = Color.magenta;
                bv.PopulateChildren();
            }

            public static void ShowSliceBorderIndices(GameObject sliceGO, Slice slice, Color indicesColor)
            {
                GameObject borderParent = new GameObject("Border Indices");
                borderParent.transform.SetParent(sliceGO.transform, false);

                foreach (int borderIndex in slice.Border.IndicesList)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = $"Index {borderIndex}";
                    go.transform.SetParent(borderParent.transform, false);
                    go.transform.localPosition = slice.Mesh.Vertices[borderIndex].Position;
                    go.transform.localScale = 0.01f * Vector3.one;
                    go.GetComponent<Renderer>().material.color = indicesColor;
                }
            }

            public static void ShowSliceBorderIntersectingPlanesIndices(GameObject sliceGO, Slice slice, Color indicesColor)
            {
                GameObject borderIntersectingParent = new GameObject("Border Intersecting Planes Indices");
                borderIntersectingParent.transform.SetParent(sliceGO.transform, false);

                foreach (int borderIntersectingIndex in slice.Border.IntersectingIndices)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.name = $"Index {borderIntersectingIndex}";
                    go.transform.SetParent(borderIntersectingParent.transform, false);
                    go.transform.localPosition = slice.Mesh.Vertices[borderIntersectingIndex].Position;
                    go.transform.localScale = 0.02f * Vector3.one;
                    go.GetComponent<Renderer>().material.color = indicesColor;
                }
            }

            public static void ShowRiggedSliceBones(GameObject sliceGO, RiggedSlice riggedSlice, Color bonesColor)
            {
                GameObject bonesParent = new GameObject("Bones");
                bonesParent.transform.SetParent(sliceGO.transform, false);

                for (int bonePositionIndex = 0; bonePositionIndex < riggedSlice.BonePositions.Length; ++bonePositionIndex)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.name = $"Bone {bonePositionIndex}";
                    go.transform.SetParent(bonesParent.transform, false);
                    go.transform.position = riggedSlice.BonePositions[bonePositionIndex];
                    go.transform.localScale = 0.3f * Vector3.one;
                    go.GetComponent<Renderer>().material.color = bonesColor;
                }
            }

            public static void ShowRiggedSliceZoneVertices(GameObject sliceGO, RiggedSlice riggedSlice)
            {
                int randColorStartIndex = Random.Range(0, colors.Length);
                GameObject verticesParent = new GameObject("Vertices");
                verticesParent.transform.SetParent(sliceGO.transform, false);

                for (int zoneIndex = 0; zoneIndex < riggedSlice.RiggingZonesIndices.Count; ++zoneIndex)
                {
                    Color color = colors[(randColorStartIndex + zoneIndex) % colors.Length];
                    GameObject zoneParent = new GameObject($"Zone {zoneIndex}");
                    zoneParent.transform.SetParent(verticesParent.transform, false);

                    foreach (int index in riggedSlice.RiggingZonesIndices[zoneIndex])
                    {
                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.name = $"Index {index}";
                        go.transform.SetParent(zoneParent.transform, false);
                        go.transform.localPosition = riggedSlice.Slice.Mesh.Vertices[index].Position;
                        go.transform.localScale = 0.05f * Vector3.one;
                        go.GetComponent<Renderer>().material.color = color;
                    }
                }
            }

            public static void ExportSliceAsObj(GameObject sliceGO, int riggedSliceIndex, string directory)
            {
                ObjExporter.WriteMesh(sliceGO, $@"{directory}\slice{riggedSliceIndex}.obj");
            }
#       endif
    }
    
}
