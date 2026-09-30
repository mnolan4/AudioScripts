using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Reports whether the listener is inside an AudioReverbZone and the zone's preset name.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Reverb Zone Readout")]
    [RequireComponent(typeof(AudioReverbZone))]
    public class ReverbZoneReadout : MonoBehaviour
    {
        public string presetName;
        [Range(0f, 1f)] public float wetAmount;
        public bool listenerInside;

        AudioReverbZone zone;

        void Awake()
        {
            zone = GetComponent<AudioReverbZone>();
            presetName = zone.reverbPreset.ToString();
        }

        void Update()
        {
            AudioListener listener = FindAnyObjectByType<AudioListener>();
            if (zone == null || listener == null)
            {
                listenerInside = false;
                wetAmount = 0f;
                return;
            }

            float distance = Vector3.Distance(listener.transform.position, transform.position);
            listenerInside = distance <= zone.maxDistance;
            if (distance <= zone.minDistance)
                wetAmount = 1f;
            else if (distance >= zone.maxDistance)
                wetAmount = 0f;
            else
                wetAmount = 1f - Mathf.InverseLerp(zone.minDistance, zone.maxDistance, distance);
        }
    }
}
