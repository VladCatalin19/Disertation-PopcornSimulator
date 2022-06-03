using System.Threading;
using System.Collections;
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
        [Space]
        [Header("Kernel properties")]
        [SerializeField] private PopcornGeneratorProperties generatorProperties;
        //[SerializeField] private float minExpansionTime = 0.1F;
        //[SerializeField] private float maxExpansionTime = 0.3F;
        //[SerializeField] private long microsecondsToYield = 1_200L;
        //[SerializeField] private float curveDirectionMaxMagnitude = 0.9F;
        //[SerializeField] private float curveDirectionCutoffPercent = 3.0F;
        //[SerializeField] private float jumpForceConeAngle = 30.0F;
        //[SerializeField] private float minJumpForce = 3.0F;
        //[SerializeField] private float maxJumpForce = 5.0F;
        //[SerializeField] private float animationJitter = 0.3F;
        //[SerializeField] private float minAngularVelocity = 0.01F;
        //[SerializeField] private float maxAngularVelocity = 0.02F;
        [Space]
        [Header("Coroutines")]
        [SerializeField] private int coroutinesPerFrame = 10;
        [SerializeField] private int coroutinesDecrementLevel = 2;
        [SerializeField] private int minCoroutinesPerFramesAfterExpansionsFinished = 10;
        [SerializeField] private List<PopcornGenerator> popcornGenerators = null;



        private Transform[] kernelSpawnPositions = null;

        private int kernelsSpawned = 0;
        private bool didPopcorn = false;

        //private Lumpn.Threading.IThread unityThread = null;
        //private Lumpn.Threading.IThread[] workerThreads = null;

        private List<IEnumerator> popcornGeneratorsCoroutines = null;
        private int coroutinesFinished = 0;
        private int lastIndex = 0;


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
            
            //popcornGeneratorsCoroutines = new List<IEnumerator>(popcornGenerators.Count);

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
            bool shouldMakePopcornManaged = Input.GetKeyDown(KeyCode.P);
            bool shouldMakePopcorn = shouldMakePopcornNormally || shouldMakePopcornRandomly || shouldMakePopcornSequencial || shouldMakePopcornManaged;

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
                else if (shouldMakePopcornManaged)
                {
                    print($"Making popcorn managed");
                    popcornGeneratorsCoroutines = new List<IEnumerator>(popcornGenerators.Count);
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

                    popcornGenerator.GeneratorProperties = generatorProperties;
                    //Time.timeScale = 0.3F;

                    var middlePartExpander = popcornGenerator.GetComponentInChildren<TempMiddlePartExpander>();
                    if ((bool)middlePartExpander)
                    {
                        middlePartExpander.ExpansionTime = Random.Range(generatorProperties.minExpansionTime, generatorProperties.maxExpansionTime);
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
                        popcornGenerator.MakePopcornSequential();
                    }
                    else if (shouldMakePopcornManaged)
                    {
                        popcornGeneratorsCoroutines.Add(WaitAndMakePopcorn(popcornGenerator));
                    }
                }

                if (shouldMakePopcornManaged)
                {
                    StartCoroutine(MakePopcornManaged());
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

        private IEnumerator WaitAndMakePopcorn(PopcornGenerator popcornGenerator)
        {
            float minDelay = Mathf.Max(popcornGenerators.Count, 100) / 100.0F;
            float maxDelay = Mathf.Max(popcornGenerators.Count, 100) / 20.0F;
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

            IEnumerator coroutine = popcornGenerator.MakePopcornCoroutine();
            while (coroutine.MoveNext())
            {
                yield return null;
            }
        }

        private IEnumerator MakePopcornManaged()
        {
            while (popcornGeneratorsCoroutines.Count > 0)
            {
                int coroutinesToProcess = Mathf.Min(popcornGeneratorsCoroutines.Count, coroutinesPerFrame);

                while (coroutinesToProcess > 0)
                {
                    IEnumerator coroutine = popcornGeneratorsCoroutines[lastIndex];

                    if (!coroutine.MoveNext())
                    {
                        // Coroutine finished
                        popcornGeneratorsCoroutines.RemoveAt(lastIndex);

                        if (popcornGeneratorsCoroutines.Count != 0)
                        {
                            lastIndex %= popcornGeneratorsCoroutines.Count;
                        }

                        ++coroutinesFinished;
                        if ((coroutinesFinished % coroutinesDecrementLevel) == 0)
                        {
                            coroutinesPerFrame = Mathf.Max(minCoroutinesPerFramesAfterExpansionsFinished, coroutinesPerFrame - 1);
                            //print($"New coroutines per frame: {coroutinesPerFrame}");
                        }
                    }
                    else
                    {
                        lastIndex = (lastIndex + 1) % popcornGeneratorsCoroutines.Count;
                    }
                    --coroutinesToProcess;
                }

                yield return null;
            }
        }
    }
}
