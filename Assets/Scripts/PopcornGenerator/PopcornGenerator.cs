using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace PopcornGenerator
{
    public class PopcornGenerator : MonoBehaviour
    {
        [SerializeField] private GameObject kernel = null;
        [SerializeField] private Collider kernelCollider = null;
        [SerializeField] private Transform cuttingPlanesParent = null;
        [SerializeField] private Transform riggingPlanesParent = null;

        [SerializeField] private Material puffMaterialPrefab = null;

        [SerializeField] private UnityEvent onKernelExpansion = null;

        private Stopwatch stopWatch = null;

        public Lumpn.Threading.IThread UnityThread { get; set; }
        public Lumpn.Threading.IThread WorkerThread  { get; set; }

        public float MinExpansionTime { get; set; }
        public float MaxExpansionTime { get; set; }

        private Plane[] cuttingPlanes = null;
        private Plane[] riggingPlanes = null;

        private void Start()
        {
            CheckIfFieldsAreGood();
            stopWatch = new Stopwatch();

            cuttingPlanes = GenerateRandomCuttingPlanes();
            riggingPlanes = GenerateRiggingPlanes();
        }

        private void CheckIfFieldsAreGood()
        {
            if (kernel == null)
                throw new System.ArgumentNullException("kernel");
            if (!kernel.GetComponent<MeshFilter>())
                throw new System.ArgumentException("Provided GameObject does not have a MeshFilter component");

            if (cuttingPlanesParent == null)
                throw new System.ArgumentNullException("cuttingPlanes");
            if (cuttingPlanesParent.childCount == 0)
                throw new System.ArgumentException("Provided CuttingPlanesParent has no children");

            if (riggingPlanesParent == null)
                throw new System.ArgumentNullException("riggingPlanes");
            if (riggingPlanesParent.childCount == 0)
                throw new System.ArgumentException("Provided RiggingPlanesParent has no children");
        }

        public IEnumerator MakePopcornCoroutine()
        {
            int frames = 0;

            Transform kernelTransform = kernel.transform;
            MeshFilter kernelMeshFilter = kernel.GetComponent<MeshFilter>();
            MeshRenderer kernelMeshRenderer = kernel.GetComponent<MeshRenderer>();
            Mesh mesh = new Mesh(kernelMeshFilter.mesh);
            Material puffMaterial = puffMaterialPrefab;//Instantiate(puffMaterialPrefab);

            //yield return WorkerThread.Context;
            yield return null;

            int slicesCount = (int)Mathf.Pow(2, cuttingPlanes.Length);
            List<Slice> slices = new List<Slice>(slicesCount);
            List<RiggedSlice> riggedSlices = new List<RiggedSlice>(slicesCount);
            List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>(slicesCount);

            ++frames;
            yield return null;

            //Utils.TransformVerticesToWorldSpace(mesh.Vertices, kernelTransform);
            IEnumerator slicerEnumerator = Slicer.SliceMesh(mesh, cuttingPlanes, slices);
            IEnumerator pufferEnumerator = Puffer.Puff(slices);
            IEnumerator riggerEnumerator = Rigger.RigSlices(slices, riggingPlanes, riggedSlices);
            IEnumerator meshProcessingEnumerator = MeshProcessing.RiggedSlicesToSkinnedMeshRenderers(riggedSlices,
                                                                                                     kernelTransform,
                                                                                                     kernelMeshRenderer,
                                                                                                     puffMaterial,
                                                                                                     skinnedMeshRenderers);
            //print($"Slicer start");
            while (slicerEnumerator.MoveNext())
            {
                ++frames;
                yield return null;
            }
            //print($"Slicer done {slices.Count}");
            //print($"Puffer start");
            while (pufferEnumerator.MoveNext())
            {
                ++frames;
                yield return null;
            }
            //print($"Puffer done");
            //print($"Rigger start");
            while (riggerEnumerator.MoveNext())
            {
                ++frames;
                yield return null;
            }
            //print($"Rigger done {riggedSlices.Count}");

            //yield return UnityThread.Context;

            //print($"Mesh Processing start");
            while (meshProcessingEnumerator.MoveNext())
            {
                ++frames;
                yield return null;
            }
            //print($"Mesh Processing done {skinnedMeshRenderers.Count}");
            
            //print($"Adding colliders");
            ColliderAdder.AddColliders(riggedSlices, skinnedMeshRenderers, kernelCollider, kernelTransform);
            ++frames;
            yield return null;
            //print($"Added colliders");

            //print($"Adding animations");
            Animator.Animate(skinnedMeshRenderers, kernelTransform, MinExpansionTime, MaxExpansionTime);
            ++frames;
            yield return null;
            //print($"Added animations");

            for (int skmIndex = 0; skmIndex < skinnedMeshRenderers.Count; ++skmIndex)
            {
                string path = $@"C:\Users\Vlad Marius\Facultate\Disertatie\PopcornGenerator\RuntimeExports\Slices\slice{skmIndex}_with_uv.obj";
                ObjExporter.WriteMesh(skinnedMeshRenderers[0].gameObject, path);
            }

            #if false
            //foreach (var skm in skinnedMeshRenderers)
            {
                var skm = skinnedMeshRenderers[0];

                //skm.sharedMesh.RecalculateNormals();
                //skm.sharedMesh.RecalculateTangents();
                skm.sharedMesh.Optimize();
                //skm.sharedMesh.OptimizeIndexBuffers();
                //skm.sharedMesh.OptimizeReorderVertexBuffer();

                var vertices = skm.sharedMesh.vertices;
                var normals = skm.sharedMesh.normals;

                for (int i = 0; i < vertices.Length; ++i)
                {
                    var color = Random.ColorHSV();

                    var lineGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    lineGO.transform.localScale = 0.004F * Vector3.one;
                    lineGO.transform.position = skm.transform.TransformPoint(vertices[i] + normals[i] * 0.025F);
                    lineGO.transform.rotation = Quaternion.FromToRotation(Vector3.up, normals[i]);
                    lineGO.GetComponent<Renderer>().material.color = color;

                    var lineTipGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    lineTipGO.transform.localScale = new Vector3(0.005F, 0.001F, 0.005F);
                    lineTipGO.transform.position = skm.transform.TransformPoint(vertices[i] + normals[i] * 0.050F);
                    lineTipGO.transform.rotation = Quaternion.FromToRotation(Vector3.up, normals[i]);
                    lineTipGO.GetComponent<Renderer>().material.color = color;
                }
            }
            #endif

            Destroy(kernelMeshFilter);
            Destroy(kernelMeshRenderer);

            HidePlanes();
            onKernelExpansion.Invoke();

            print($"Number of frames: {frames}");
        }

        public void MakePopcornSequencial()
        {
            long prevElapsed;
            long currElapsed;
            IEnumerator popcornEnumerator = MakePopcornCoroutine();
            bool isEnumeratorMoving = true;
            stopWatch.Start();
            while (isEnumeratorMoving)
            {
                prevElapsed = stopWatch.ElapsedMilliseconds;
                //print($"Before enumerator move next");
                isEnumeratorMoving = popcornEnumerator.MoveNext();
                //print($"After enumerator move next");
                currElapsed = stopWatch.ElapsedMilliseconds;
                //print($"Step miliseconds: {currElapsed - prevElapsed}");
            }
            stopWatch.Stop();

            print($"Total time miliseconds: {stopWatch.ElapsedMilliseconds}");
        }

        private Plane[] GenerateCuttingPlanes()
        {
            return TransformsToPlanes(cuttingPlanesParent);
        }

        private Plane[] GenerateRiggingPlanes()
        {
            return TransformsToPlanes(riggingPlanesParent);
        }

        private Plane[] TransformsToPlanes(Transform planesParent)
        {
            Plane[] planes = new Plane[planesParent.childCount];
            for (int planeIndex = 0; planeIndex < planes.Length; ++planeIndex)
            {
                planes[planeIndex] = TransformToPlane(planesParent.GetChild(planeIndex));
                CreatePlaneGameObjects(planes[planeIndex], planesParent);
            }
            return planes;
        }

        private Plane TransformToPlane(Transform t)
        {
            Vector3 planeNormal = kernel.transform.InverseTransformDirection(t.up);
            Vector3 planeInPoint = kernel.transform.InverseTransformPoint(t.position);
            return new Plane(planeNormal, planeInPoint);
        }

        private Plane[] GenerateRandomCuttingPlanes()
        {
            int planesNum = Random.Range(2, 3);
            Plane[] planes = new Plane[planesNum];

            float minAngleBetween = 45.0F;
            float maxAngleBetween = 165.0F / planesNum;

            float angleJitter = Random.Range(0.0f, 180.0f);

            //Vector3 planesInPoint = kernel.transform.position;
            Vector3 planesInPoint = kernel.transform.localPosition;

            float totalAngle = angleJitter;

            for (int planeIndex = 0; planeIndex < planesNum; ++planeIndex)
            {
                Quaternion q = Quaternion.Euler(0.0F, totalAngle, 90.0F);
                Vector3 planeNormal = q * Vector3.up;
                planes[planeIndex] = new Plane(planeNormal, planesInPoint);

                float currentAngle = Random.Range(minAngleBetween, maxAngleBetween);
                totalAngle += currentAngle;

                CreatePlaneGameObjects(planes[planeIndex], cuttingPlanesParent);
            }
            return planes;
        }

        private void CreatePlaneGameObjects(Plane plane, Transform planeParent)
        {
            GameObject go = Instantiate(planeParent.GetChild(0).gameObject);
            go.SetActive(true);
            go.transform.parent = planeParent;

            Vector3 inPoint = plane.Normal * plane.Distance;
            go.transform.SetPositionAndRotation(kernel.transform.TransformPoint(inPoint),
                                                Quaternion.FromToRotation(Vector3.up, kernel.transform.TransformDirection(plane.Normal)));
            //go.transform.eulerAngles = new Vector3(0.0f, totalAngle, 90.0f);
        }

        private void HidePlanes()
        {
            cuttingPlanesParent.gameObject.SetActive(false);
            riggingPlanesParent.gameObject.SetActive(false);
        }
    }
}