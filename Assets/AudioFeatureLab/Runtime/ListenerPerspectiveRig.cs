using UnityEngine;

namespace AudioFeatureLab
{
    public enum ListenerOffsetPreset
    {
        FirstPerson = 0,
        ThirdPerson = 1,
        Overhead = 2
    }

    /// <summary>
    /// Puts the AudioListener on an ear transform that can follow the camera or move on its own.
    /// Hearing follows the listener, not the camera.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Listener Perspective Rig")]
    public class ListenerPerspectiveRig : MonoBehaviour
    {
        public Transform view;
        public Transform ear;
        public Transform focus;
        public bool keyboardControl = true;
        public bool lockedToView = true;
        public ListenerOffsetPreset preset = ListenerOffsetPreset.FirstPerson;
        public Vector3 firstPersonOffset = Vector3.zero;
        public Vector3 thirdPersonOffset = new Vector3(0f, 0.35f, -2.4f);
        public Vector3 overheadOffset = new Vector3(0f, 7f, -0.4f);
        public float detachedSpeed = 4f;

        public Vector3 listenerPosition;
        public float distanceToFocus;
        public string perspectiveName = "FirstPerson";

        void Awake()
        {
            if (ear == null)
                return;
            AudioListener listener = ear.GetComponent<AudioListener>();
            if (listener == null)
                listener = ear.gameObject.AddComponent<AudioListener>();
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < listeners.Length; i++)
                listeners[i].enabled = listeners[i] == listener;
        }

        void Update()
        {
            if (ear == null || view == null)
                return;

            if (keyboardControl)
            {
                if (Input.GetKeyDown(KeyCode.L))
                    lockedToView = !lockedToView;
                if (Input.GetKeyDown(KeyCode.F))
                {
                    preset = ListenerOffsetPreset.FirstPerson;
                    lockedToView = true;
                }

                if (Input.GetKeyDown(KeyCode.T))
                {
                    preset = ListenerOffsetPreset.ThirdPerson;
                    lockedToView = true;
                }

                if (Input.GetKeyDown(KeyCode.O))
                {
                    preset = ListenerOffsetPreset.Overhead;
                    lockedToView = true;
                }
            }

            if (lockedToView)
            {
                if (ear.parent != view)
                    ear.SetParent(view, false);
                ear.localPosition = OffsetFor(preset);
                ear.localRotation = Quaternion.identity;
                perspectiveName = preset.ToString();
            }
            else
            {
                if (ear.parent != null)
                    ear.SetParent(null, true);
                Vector3 planarForward = view.forward;
                planarForward.y = 0f;
                if (planarForward.sqrMagnitude < 0.001f)
                    planarForward = Vector3.forward;
                planarForward.Normalize();
                Vector3 planarRight = view.right;
                planarRight.y = 0f;
                planarRight.Normalize();
                Vector3 move = Vector3.zero;
                if (Input.GetKey(KeyCode.G))
                    move -= planarRight;
                if (Input.GetKey(KeyCode.J))
                    move += planarRight;
                if (Input.GetKey(KeyCode.H))
                    move -= planarForward;
                if (Input.GetKey(KeyCode.Y))
                    move += planarForward;
                if (Input.GetKey(KeyCode.PageUp))
                    move += Vector3.up;
                if (Input.GetKey(KeyCode.PageDown))
                    move += Vector3.down;
                if (move.sqrMagnitude > 1f)
                    move.Normalize();
                ear.position += move * detachedSpeed * Time.deltaTime;
                perspectiveName = "Detached";
            }

            listenerPosition = ear.position;
            distanceToFocus = focus != null ? Vector3.Distance(ear.position, focus.position) : 0f;
        }

        Vector3 OffsetFor(ListenerOffsetPreset value)
        {
            switch (value)
            {
                case ListenerOffsetPreset.ThirdPerson:
                    return thirdPersonOffset;
                case ListenerOffsetPreset.Overhead:
                    return overheadOffset;
                default:
                    return firstPersonOffset;
            }
        }
    }
}
