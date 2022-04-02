using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PopcornGenerator
{
    public class PopcornGeneratorManager : MonoBehaviour
    {
        [SerializeField] private GameObject kernelPrefab = null;
        [Space]
        [SerializeField] private Transform kernelSpawnPositionsParend = null;
        [SerializeField] private Transform kernelsParent = null;  
        [Space]
        [SerializeField] private Text numberOfKernelsText = null;

        [SerializeField] private List<PopcornGenerator> popcornGenerators = null;
        private Transform[] kernelSpawnPositions = null;

        private int kernelsSpawned = 0;
        private bool didPopcorn = false;

        //private Lumpn.Threading.IThread unityThread = null;
        //private Lumpn.Threading.IThread[] workerThreads = null;


        private void Awake()
        {
            kernelSpawnPositions = new Transform[kernelSpawnPositionsParend.childCount];
            for (int spawnPositionIndex = 0; spawnPositionIndex < kernelSpawnPositionsParend.childCount; ++spawnPositionIndex)
            {
                kernelSpawnPositions[spawnPositionIndex] = kernelSpawnPositionsParend.GetChild(spawnPositionIndex);
            }

            //popcornGenerators = new PopcornGenerator[kernelsParent.childCount];
            //for (int childIndex = 0; childIndex < kernelsParent.childCount; ++childIndex)
            //{
            //    popcornGenerators[childIndex] = kernelsParent.GetChild(childIndex).GetComponent<PopcornGenerator>();
            //}

            if (popcornGenerators == null)
            {
                popcornGenerators = new List<PopcornGenerator>();
            }

            //unityThread = Lumpn.Threading.ThreadUtils.StartUnityThread("PopcornGeneratorManager", kernelsParent.childCount, this);

            //workerThreads = new Lumpn.Threading.IThread[kernelsParent.childCount];
            //for (int threadIndex = 0; threadIndex < workerThreads.Length; ++threadIndex)
            //{
            //    workerThreads[threadIndex] = Lumpn.Threading.ThreadUtils.StartWorkerThread("worker", threadIndex.ToString(),
            //                                                                               System.Threading.ThreadPriority.Normal, 1);
            //}
        }

        private void Update()
        {
            bool shouldSpawnKernels = Input.GetButtonDown("KernelSpawn");
            bool shouldMakePopcornSequencial = Input.GetButtonDown("SequencialPop");
            bool shouldMakePopcornNormally = Input.GetButtonDown("NormalPop");
            bool shouldMakePopcornRandomly = Input.GetButtonDown("RandomPop");
            bool shouldMakePopcorn = shouldMakePopcornNormally || shouldMakePopcornRandomly || shouldMakePopcornSequencial;

            if (shouldSpawnKernels)
            {
                foreach (Transform kernelPosition in kernelSpawnPositions)
                {
                    Quaternion rotation = Random.rotationUniform;
                    Vector3 positionJitter = new Vector3()
                    {
                        x = Random.Range(-3.0F, 3.0F),
                        y = Random.Range(-3.0F, 3.0F),
                        z = Random.Range(-3.0F, 3.0F)
                    };

                    GameObject kernelCopy = Instantiate(kernelPrefab);
                    kernelCopy.name = $"Kernel {kernelsSpawned}";
                    kernelCopy.transform.parent = kernelsParent;
                    kernelCopy.transform.SetPositionAndRotation(kernelPosition.position + positionJitter, rotation);

                    popcornGenerators.Add(kernelCopy.GetComponent<PopcornGenerator>());

                    ++kernelsSpawned;
                }
                if ((bool)numberOfKernelsText)
                {
                    numberOfKernelsText.text = kernelsSpawned.ToString();
                }
            }

            if (shouldMakePopcorn && !didPopcorn)
            {
                if (shouldMakePopcornNormally)
                {
                    print($"Making popcorn normally");
                }
                else if (shouldMakePopcornRandomly)
                {
                    print($"Making popcorn randomly");
                }
                else if (shouldMakePopcornSequencial)
                {
                    print($"Making popcorn sequencial");
                }

                //MakePopcornSequencial();
                //print($"Making popcorn!");
                //foreach (PopcornGenerator popcornGenerator in popcornGenerators)
                for (int popcornGeneratorIndex = 0; popcornGeneratorIndex < popcornGenerators.Count; ++popcornGeneratorIndex)
                {
                    //ThreadPool.QueueUserWorkItem(popcornGenerator.MakePopcornWaitCallback);
                    //StartCoroutine(popcornGenerator.MakePopcornCoroutine());

                    var popcornGenerator = popcornGenerators[popcornGeneratorIndex];
                    //popcornGenerator.UnityThread = unityThread;
                    //popcornGenerator.WorkerThread = workerThreads[popcornGeneratorIndex];

                    popcornGenerator.MinExpansionTime = 2.1F;
                    popcornGenerator.MaxExpansionTime = 2.5F;

                    var middlePartExpander = popcornGenerator.GetComponentInChildren<TempMiddlePartExpander>();
                    if ((bool)middlePartExpander)
                    {
                        middlePartExpander.ExpansionTime = Random.Range(popcornGenerator.MinExpansionTime, popcornGenerator.MaxExpansionTime);
                    }

                    if (shouldMakePopcornNormally)
                    {
                        StartCoroutine(popcornGenerator.MakePopcornCoroutine());
                    }
                    else if (shouldMakePopcornRandomly)
                    {
                        StartCoroutine(WaitAndMakePopcorn(popcornGenerator));
                    }
                    else if (shouldMakePopcornSequencial)
                    {
                        popcornGenerator.MakePopcornSequencial();
                    }
                }

                didPopcorn = true;
            }
        }

        private void OnDestroy()
        {
            //Lumpn.Threading.ThreadUtils.StopThread(unityThread);
            //foreach (var thread in workerThreads)
            //{
            //    Lumpn.Threading.ThreadUtils.StopThread(thread);
            //}
        }

        private System.Collections.IEnumerator WaitAndMakePopcorn(PopcornGenerator popcornGenerator)
        {
            yield return new WaitForSeconds(Random.Range(0.1F, 10.0F));
            StartCoroutine(popcornGenerator.MakePopcornCoroutine());
        }
    }
}
