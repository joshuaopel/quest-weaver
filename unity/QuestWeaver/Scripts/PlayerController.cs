// Quest Weaver — minimal capsule player: WASD relative to the camera's yaw,
// gravity via CharacterController, camera follows behind.

using UnityEngine;

namespace QuestWeaver
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 5.5f;
        public Transform cam;          // assigned by the demo bootstrap
        public Vector3 camOffset = new Vector3(0, 7f, -8f);

        CharacterController cc;
        float vy;

        void Awake() { cc = GetComponent<CharacterController>(); }

        void Update()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 fwd = cam != null ? cam.forward : Vector3.forward;
            fwd.y = 0; fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);

            Vector3 move = (fwd * v + right * h);
            if (move.sqrMagnitude > 1f) move.Normalize();

            vy = cc.isGrounded ? -1f : vy - 20f * Time.deltaTime;
            cc.Move((move * moveSpeed + Vector3.up * vy) * Time.deltaTime);

            if (move.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(move), 12f * Time.deltaTime);
        }

        void LateUpdate()
        {
            if (cam == null) return;
            cam.position = Vector3.Lerp(cam.position, transform.position + camOffset, 8f * Time.deltaTime);
            cam.LookAt(transform.position + Vector3.up * 1.2f);
        }
    }

    /// <summary>Marks the player and carries their narrative profile.</summary>
    public class QuestPlayer : MonoBehaviour
    {
        public PlayerProfile profile = new PlayerProfile();
    }
}
