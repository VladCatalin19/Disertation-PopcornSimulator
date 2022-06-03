using UnityEngine;

namespace PopcornGenerator
{
    public class BurnerManager : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            // Note: collision.gameObject gives the rigid body's game object, whereas
            // collision.collider.gameObject gives the collided object's game object.
            GameObject colliderGO = collision.collider.gameObject;

            if ((colliderGO.layer & LayerMask.NameToLayer("Burning")) != 0)
            {
                Burner b = colliderGO.GetComponent<Burner>();
                b.StartBurning();
            }
        }
    }
}
