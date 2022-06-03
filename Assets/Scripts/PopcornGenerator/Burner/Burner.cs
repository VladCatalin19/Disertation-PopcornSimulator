using UnityEngine;
using System.Collections;

namespace PopcornGenerator
{
    public class Burner : MonoBehaviour
    {
        private float burnTime = 0.0F;
        private Vector2 burnCenter = Vector2.zero;
        private float finalBurnRadius = 0.0F;
        private int burnPointIndex = 0;
        private Material puffMaterial = null;

        private string burnPointString = null;
        private string burnPointRadiusString = null;

        private float timeAfterBurningIsEnabled = 0.0F;
        
        private enum State
        {
            idle,
            isBurning,
            burnt,
        }

        private State state = State.idle;

        public float BurnTime
        {
            get => burnTime;
            internal set => burnTime = value;
        }

        public Vector2 BurnCenter
        {
            get => burnCenter;
            internal set => burnCenter = value;
        }

        public float FinalBurnRadius
        {
            get => finalBurnRadius;
            internal set => finalBurnRadius = value;
        }

        public int BurnPointIndex
        {
            get => burnPointIndex;
            internal set => burnPointIndex = value;
        }

        public Material PuffMaterial
        {
            get => puffMaterial;
            internal set => puffMaterial = value;
        }

        private void Awake()
        {
            timeAfterBurningIsEnabled = Time.time + 0.3F * Time.timeScale;
        }

        public void StartBurning()
        {
            if ((Time.time > timeAfterBurningIsEnabled) && (state == State.idle))
            {
                burnPointString = $"_BurnPoint{burnPointIndex}";
                burnPointRadiusString = $"_BurnRadius{burnPointIndex}";
                StartCoroutine(BurniningCoroutine());
            }
        }

        private IEnumerator BurniningCoroutine()
        {
            state = State.isBurning;
            puffMaterial.SetVector(burnPointString, burnCenter);
            puffMaterial.SetFloat(burnPointRadiusString, 0.0F);

            float radius = 0.0F;

            for (float time = 0.0F; time <= burnTime; time += Time.deltaTime)
            {
                float t = time / burnTime;
                radius = Mathf.Lerp(0.0F, finalBurnRadius, t);
                puffMaterial.SetFloat(burnPointRadiusString, radius);

                yield return null;
            }

            state = State.burnt;
        }
    }
}
