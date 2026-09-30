using UnityEngine;

namespace AudioFeatureLab
{
    [AddComponentMenu("Audio Feature Lab/Look Move Controller")]
    public class LookMoveController : MonoBehaviour
    {
        public Transform view;
        public float walkSpeed = 4.5f;
        public float fastSpeed = 9f;
        public float lookSensitivity = 2.2f;
        public bool fly;
        public float minPitch = -70f;
        public float maxPitch = 70f;

        float pitch;
        float yaw;

        void Start()
        {
            if (view == null && Camera.main != null)
                view = Camera.main.transform;
            yaw = transform.eulerAngles.y;
            if (view != null)
            {
                pitch = view.localEulerAngles.x;
                if (pitch > 180f)
                    pitch -= 360f;
            }
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                yaw += Input.GetAxis("Mouse X") * lookSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                if (view != null)
                    view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            float speed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : walkSpeed;
            Vector3 move = transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical");
            if (fly)
            {
                if (Input.GetKey(KeyCode.E))
                    move += Vector3.up;
                if (Input.GetKey(KeyCode.Q))
                    move -= Vector3.up;
            }

            if (move.sqrMagnitude > 1f)
                move.Normalize();
            transform.position += move * speed * Time.deltaTime;
            if (!fly)
            {
                Vector3 grounded = transform.position;
                grounded.y = 0f;
                transform.position = grounded;
            }
        }
    }
}
