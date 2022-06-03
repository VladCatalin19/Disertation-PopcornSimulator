using UnityEngine;

namespace PopcornGenerator
{
    internal static class Jumper
    {
        public static void MakePopcornJump(Rigidbody rigidbody, PopcornGeneratorProperties properties)
        {
            //         up
            //    r       r
            //   x----^----x -> random point is inside this circle
            //    \   |h  /
            //     \  |  /
            //      \ | /
            //       \|/
            //        x origin
            //
            // the angle at the base is 2 * atan(r / h)
            // h is 1, thus angle = 2 * atan(r)
            // r = tan(angle / 2)

            float angle = properties.jumpForceConeAngle * Mathf.Deg2Rad;
            float circleRadius = Mathf.Tan(angle / 2.0F);

            float jumpForce = Random.Range(properties.minJumpForce, properties.maxJumpForce);

            Vector2 pointInCircle = Random.insideUnitCircle * circleRadius;
            Vector3 jumpDirection = new Vector3(pointInCircle.x, 1.0F, pointInCircle.y).normalized;

            rigidbody.AddForce(jumpDirection * jumpForce, ForceMode.Impulse);

            Vector3 angularVelocityDirection = Random.onUnitSphere;
            float angularVelocity = Random.Range(properties.minAngularVelocity, properties.maxAngularVelocity);

            rigidbody.AddTorque(angularVelocityDirection * angularVelocity, ForceMode.Impulse);
        }
    }
}
