using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Moves a looping source back and forth so Doppler shift is easy to hear.
    /// Dynamic velocity tracking follows a transform that has no Rigidbody.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Doppler Shuttle")]
    public class DopplerShuttle : MonoBehaviour
    {
        public Vector3 pointA = new Vector3(-10f, 1.2f, 0f);
        public Vector3 pointB = new Vector3(10f, 1.2f, 0f);
        public float speed = 16f;
        public AudioSource source;
        [Range(0f, 5f)] public float dopplerLevel = 2.5f;

        float travelled;
        int direction = 1;

        void Awake()
        {
            if (source == null)
                source = GetComponent<AudioSource>();
            if (source != null)
                source.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
        }

        void Update()
        {
            if (source != null)
            {
                source.dopplerLevel = dopplerLevel;
                source.spatialBlend = 1f;
            }

            float length = Vector3.Distance(pointA, pointB);
            if (length < 0.01f)
                return;
            travelled += direction * speed * Time.deltaTime;
            if (travelled >= length)
            {
                travelled = length;
                direction = -1;
            }
            else if (travelled <= 0f)
            {
                travelled = 0f;
                direction = 1;
            }

            transform.position = Vector3.Lerp(pointA, pointB, travelled / length);
        }
    }
}
