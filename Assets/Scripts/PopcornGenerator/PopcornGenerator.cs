//#define POPCORN_GEN__LOG_PER_FRAME

using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

using Stopwatch = System.Diagnostics.Stopwatch;

namespace PopcornGenerator
{
    public class PopcornGenerator : MonoBehaviour
    {
        [SerializeField] private GameObject kernel = null;
        [SerializeField] private Collider kernelCollider = null;
        [SerializeField] private new Rigidbody rigidbody = null;
        [SerializeField] private Transform cuttingPlanesParent = null;
        [SerializeField] private Transform riggingPlanesParent = null;

        [SerializeField] private Material puffMaterialPrefab = null;

        [SerializeField] private UnityEvent onKernelExpansion = null;

        private Stopwatch stopWatch = null;

        public Lumpn.Threading.IThread UnityThread { get; set; }
        public Lumpn.Threading.IThread WorkerThread  { get; set; }

        public PopcornGeneratorProperties GeneratorProperties { get; set; }

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

            LogStageName($"Getting needed unity components");
            Transform kernelTransform = kernel.transform;
            MeshFilter kernelMeshFilter = kernel.GetComponent<MeshFilter>();
            MeshRenderer kernelMeshRenderer = kernel.GetComponent<MeshRenderer>();

            //yield return WorkerThread.Context;
            ++frames;
            yield return null;
            LogStageName($"Allocating mesh");
            Mesh mesh = new Mesh(kernelMeshFilter.mesh);

            ++frames;
            yield return null;

            LogStageName($"Allocating lists");
            int slicesCount = (int)Mathf.Pow(2, cuttingPlanes.Length);
            List<Slice> slices = new List<Slice>(slicesCount);
            List<RiggedSlice> riggedSlices = new List<RiggedSlice>(slicesCount);
            List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>(slicesCount);

            ++frames;
            yield return null;

            LogStageName($"Creating enumerators");
            //Utils.TransformVerticesToWorldSpace(mesh.Vertices, kernelTransform);
            var properties = GeneratorProperties;
            properties.stopwatch = new Stopwatch();
            GeneratorProperties = properties;

            IEnumerator slicerEnumerator = Slicer.SliceMesh(mesh, cuttingPlanes, slices, GeneratorProperties);
            IEnumerator pufferEnumerator = Puffer.Puff(slices, GeneratorProperties);
            IEnumerator riggerEnumerator = Rigger.RigSlices(slices, riggingPlanes, riggedSlices, GeneratorProperties);
            IEnumerator meshProcessingEnumerator = MeshProcessing.RiggedSlicesToSkinnedMeshRenderers(riggedSlices,
                                                                                                     kernelTransform,
                                                                                                     kernelMeshRenderer,
                                                                                                     puffMaterialPrefab,
                                                                                                     skinnedMeshRenderers,
                                                                                                     GeneratorProperties);
            IEnumerator colliderAdderEnumerator = ColliderAdder.AddColliders(riggedSlices, skinnedMeshRenderers,
                                                                             kernelCollider, kernelTransform, GeneratorProperties);
            IEnumerator animatorEnumerator = Animator.Animate(skinnedMeshRenderers, kernelTransform, GeneratorProperties);
            ++frames;
            yield return null;

            LogStageName($"Slicer start");
            while (slicerEnumerator.MoveNext())
            {
                ++frames;
                yield return null;
            }
            LogStageName($"Slicer done. Slices: {slices.Count}");
            LogStageName($"Puffer start");
            while (pufferEnumerator.MoveNext())
            {
                ++frames;
                yield return null;
            }
            LogStageName($"Puffer done");
            LogStageName($"Rigger start");
            while (riggerEnumerator.MoveNext())
            {
                ++frames;
                yield return null;
            }
            LogStageName($"Rigger done. Rigged slices: {riggedSlices.Count}");

            //yield return UnityThread.Context;

            LogStageName($"Mesh Processing start");
            while (meshProcessingEnumerator.MoveNext())
            {
                ++frames;
                yield return null;
            }
            LogStageName($"Mesh Processing done. Skinned mesh renderes: {skinnedMeshRenderers.Count}");

            LogStageName($"Adding colliders");
            while (colliderAdderEnumerator.MoveNext())
            {
                ++frames;
                yield return null;
            }
            
            ++frames;
            yield return null;
            LogStageName($"Added colliders");

            LogStageName($"Adding animations");
            while (animatorEnumerator.MoveNext())
            {
                ++frames;
                yield return null;
            }
            LogStageName($"Added animations");

            LogStageName($"Adding jump force");
            Jumper.MakePopcornJump(rigidbody, GeneratorProperties);
            LogStageName($"Added jump force");

            LogStageName($"Calling on kernel expansion events");
            onKernelExpansion.Invoke();
            LogStageName($"Called on kernel expansion events");

            #if false // export slices as obj's
            for (int skmIndex = 0; skmIndex < skinnedMeshRenderers.Count; ++skmIndex)
            {
                string path = $@"C:\Users\Vlad Marius\Facultate\Disertatie\PopcornGenerator\RuntimeExports\Slices\slice{skmIndex}_with_uv.obj";
                ObjExporter.WriteMesh(skinnedMeshRenderers[0].gameObject, path);
            }
            #endif

            #if false // draw normals for each vertex
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

            yield return null;

            HidePlanes();

#           if POPCORN_GEN__LOG_PER_FRAME
                print($"Number of frames: {frames}");
#           endif
        }

        public void MakePopcornSequential()
        {
            long prevElapsed;
            long currElapsed;
            IEnumerator popcornEnumerator = MakePopcornCoroutine();
            bool isEnumeratorMoving = true;
            stopWatch.Start();
            while (isEnumeratorMoving)
            {
                prevElapsed = stopWatch.ElapsedTicks / 10;
                isEnumeratorMoving = popcornEnumerator.MoveNext();
                currElapsed = stopWatch.ElapsedTicks / 10;

                long duration = currElapsed - prevElapsed;

                LogStageDuration(duration);
            }
            stopWatch.Stop();

#           if POPCORN_GEN__LOG_PER_FRAME
                print($"Total time miliseconds: {stopWatch.ElapsedTicks / 10:N}");
#           endif
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

        private static void LogStageDuration(long duration)
        {
#           if POPCORN_GEN__LOG_PER_FRAME
                string color = (duration < 1_000)
                                   ? "yellow"
                                   : (duration < 2_000)
                                     ? "lime"
                                     : "red";

                print($"Step microseconds: <color={color}>{duration:N}</color>");
#           endif
        }

        private static void LogStageName(string stageName)
        {
#           if POPCORN_GEN__LOG_PER_FRAME
                Debug.LogWarning($"<color=orange> {stageName} </color>");
#           endif
        }
    }
}
