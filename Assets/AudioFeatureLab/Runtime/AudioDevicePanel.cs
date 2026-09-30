using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Reads input devices and the active output configuration.
    /// Unity lists microphones by name. Output device names are not part of the public API;
    /// this panel shows the configuration you can read and reset: rate, speaker mode, and DSP buffer.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Audio Device Panel")]
    public class AudioDevicePanel : MonoBehaviour
    {
        public MicrophoneCapture capture;
        public bool showPanel = true;

        public int outputSampleRate;
        public int dspBufferSize;
        public string speakerMode = string.Empty;
        public string driverCapabilities = string.Empty;
        public int realVoices;
        public int virtualVoices;
        public double dspTime;
        public string activeInput = string.Empty;
        public string[] inputDevices = new string[0];

        readonly int[] bufferChoices = { 256, 512, 1024, 2048 };
        int bufferIndex = 2;

        void Update()
        {
            AudioConfiguration configuration = AudioSettings.GetConfiguration();
            outputSampleRate = AudioSettings.outputSampleRate;
            dspBufferSize = configuration.dspBufferSize;
            speakerMode = configuration.speakerMode.ToString();
            driverCapabilities = AudioSettings.driverCapabilities.ToString();
            realVoices = configuration.numRealVoices;
            virtualVoices = configuration.numVirtualVoices;
            dspTime = AudioSettings.dspTime;
            inputDevices = Microphone.devices ?? new string[0];
            activeInput = capture != null ? capture.activeDevice : string.Empty;
            for (int i = 0; i < bufferChoices.Length; i++)
            {
                if (bufferChoices[i] == dspBufferSize)
                    bufferIndex = i;
            }
        }

        void OnGUI()
        {
            if (!showPanel)
                return;
            float width = Mathf.Clamp(Screen.width * 0.34f, 420f, 560f);
            var panel = new Rect(Screen.width - width - 16f, 16f, width, Screen.height - 32f);
            LabGui.Fill(panel);
            GUILayout.BeginArea(new Rect(panel.x + 16f, panel.y + 16f, panel.width - 32f, panel.height - 32f));
            GUILayout.Label("Output", LabGui.Title);
            GUILayout.Label("Sample rate  " + outputSampleRate + " Hz", LabGui.Body);
            GUILayout.Label("DSP buffer  " + dspBufferSize + " samples", LabGui.Body);
            GUILayout.Label("Speaker mode  " + speakerMode, LabGui.Body);
            GUILayout.Label("Driver can do  " + driverCapabilities, LabGui.Body);
            GUILayout.Label("Voices  " + realVoices + " real / " + virtualVoices + " virtual", LabGui.Body);
            GUILayout.Label("DSP time  " + dspTime.ToString("0.00") + " s", LabGui.Body);
            GUILayout.Space(12f);
            if (GUILayout.Button("Smaller DSP buffer", LabGui.Button, GUILayout.Height(52f)))
                StepBuffer(-1);
            GUILayout.Space(6f);
            if (GUILayout.Button("Larger DSP buffer", LabGui.Button, GUILayout.Height(52f)))
                StepBuffer(1);
            GUILayout.Space(6f);
            if (GUILayout.Button("Cycle speaker mode", LabGui.Button, GUILayout.Height(52f)))
                CycleSpeakerMode();
            GUILayout.Space(14f);
            GUILayout.Label("Input devices", LabGui.Title);
            if (inputDevices.Length == 0)
                GUILayout.Label("None reported. Check OS microphone permission.", LabGui.Body);
            for (int i = 0; i < inputDevices.Length; i++)
            {
                string marker = inputDevices[i] == activeInput ? "  (active)" : string.Empty;
                GUILayout.Label((i + 1) + ". " + inputDevices[i] + marker, LabGui.Body);
            }

            GUILayout.EndArea();
        }

        void StepBuffer(int direction)
        {
            bufferIndex = Mathf.Clamp(bufferIndex + direction, 0, bufferChoices.Length - 1);
            AudioConfiguration configuration = AudioSettings.GetConfiguration();
            configuration.dspBufferSize = bufferChoices[bufferIndex];
            AudioSettings.Reset(configuration);
        }

        void CycleSpeakerMode()
        {
            AudioConfiguration configuration = AudioSettings.GetConfiguration();
            switch (configuration.speakerMode)
            {
                case AudioSpeakerMode.Mono:
                    configuration.speakerMode = AudioSpeakerMode.Stereo;
                    break;
                case AudioSpeakerMode.Stereo:
                    configuration.speakerMode = AudioSpeakerMode.Surround;
                    break;
                default:
                    configuration.speakerMode = AudioSpeakerMode.Mono;
                    break;
            }

            AudioSettings.Reset(configuration);
        }
    }
}
