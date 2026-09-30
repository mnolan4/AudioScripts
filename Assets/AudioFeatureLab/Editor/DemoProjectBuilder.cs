using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AudioFeatureLab.EditorTools
{
    public static class DemoProjectBuilder
    {
        const string Root = "Assets/AudioFeatureLab";
        const string MixerPath = Root + "/Mixers/DemoMixer.mixer";
        const string SceneDir = Root + "/Scenes";

        [MenuItem("Audio Feature Lab/Build Demo Scenes")]
        public static void BuildAll()
        {
            BuildAllInternal();
        }

        public static void BuildAllBatch()
        {
            try
            {
                BuildAllInternal();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        static void BuildAllInternal()
        {
            AudioLabMathTests.RunOrThrow();
            PlayerSettings.productName = "Audio Feature Lab";
            PlayerSettings.companyName = "AudioFeatureLab";
            EnsureFolder("Assets/AudioFeatureLab");
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Mixers");
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Clips");

            Clips clips = LoadClips();
            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) != null)
                AssetDatabase.DeleteAsset(MixerPath);
            AudioMixer mixer = MixerLabFactory.Create(MixerPath);

            BuildMenu();
            BuildSources(clips);
            BuildListener(clips);
            BuildInput(clips);
            BuildMixer(clips, mixer);
            BuildCrossfade(clips);

            EditorBuildSettings.scenes = new[]
            {
                Scene("00_MainMenu"),
                Scene("01_AudioSource_2D3D"),
                Scene("02_AudioListener_Perspective"),
                Scene("03_InputOutput_Meters"),
                Scene("04_Mixer_Groups_Snapshots"),
                Scene("05_Crossfade_FFT_Music")
            };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(SceneDir + "/00_MainMenu.unity");
            Debug.Log("Audio feature lab scenes built.");
        }

        static EditorBuildSettingsScene Scene(string name)
        {
            return new EditorBuildSettingsScene(SceneDir + "/" + name + ".unity", true);
        }

        static void BuildMenu()
        {
            BeginScene();
            Camera.main.transform.position = new Vector3(0f, 1.6f, -6f);
            var menu = new GameObject("Menu").AddComponent<SceneMenu>();
            menu.headline = "Audio Feature Lab";
            menu.summary =
                "A playable tour of Unity's built-in audio system: AudioSource (2D and 3D), AudioListener placement, " +
                "microphone input, speaker output, and the Audio Mixer (groups and snapshots).\n\n" +
                "Click a row or press its number. In a demo, click the Game view before using keys. " +
                "M returns here. P pauses all audio. [ and ] change listener volume.\n\n" +
                "Unity " + Application.unityVersion + "\nC# / .NET (Unity player)\nSee Docs/USER_GUIDE.md to modify the lab.";
            menu.entries = new[]
            {
                Entry("1  AudioSource, 2D and 3D", "01_AudioSource_2D3D", KeyCode.Alpha1),
                Entry("2  AudioListener perspective", "02_AudioListener_Perspective", KeyCode.Alpha2),
                Entry("3  Input, output, and meters", "03_InputOutput_Meters", KeyCode.Alpha3),
                Entry("4  Mixer groups and snapshots", "04_Mixer_Groups_Snapshots", KeyCode.Alpha4),
                Entry("5  Crossfades, FFT, and music", "05_Crossfade_FFT_Music", KeyCode.Alpha5)
            };
            Save("00_MainMenu");
        }

        static void BuildSources(Clips clips)
        {
            BeginScene();
            Transform rig = MakePlayer(new Vector3(0f, 0f, -8f), false);
            Floor();
            Tone("Radio 2D", new Vector3(-6f, 1f, 3f), clips.Radio, new Color(0.2f, 0.75f, 0.85f), 0f, 0.45f, false);
            Tone("Beacon 3D", new Vector3(6f, 1f, 3f), clips.Beacon, new Color(0.95f, 0.45f, 0.15f), 1f, 0.7f, true);
            AudioSource hybrid = Tone("Hybrid Source", new Vector3(0f, 1.1f, 7f), clips.Drone, new Color(0.95f, 0.85f, 0.25f), 0.45f, 0.4f, true);
            var spatial = hybrid.gameObject.AddComponent<SpatialAudioController>();
            spatial.source = hybrid;
            spatial.mode = SpatialMode.Hybrid;

            var shuttle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shuttle.name = "Doppler Shuttle";
            shuttle.transform.localScale = new Vector3(0.7f, 0.45f, 1.1f);
            shuttle.GetComponent<Renderer>().sharedMaterial = Mat("Doppler", new Color(0.95f, 0.55f, 0.15f));
            shuttle.AddComponent<WorldLabel>().text = "Doppler";
            AudioSource shuttleSource = shuttle.AddComponent<AudioSource>();
            shuttleSource.clip = clips.Doppler;
            shuttleSource.loop = true;
            shuttleSource.playOnAwake = true;
            shuttleSource.volume = 0.42f;
            shuttleSource.spatialBlend = 1f;
            shuttleSource.minDistance = 2f;
            shuttleSource.maxDistance = 30f;
            var motion = shuttle.AddComponent<DopplerShuttle>();
            motion.source = shuttleSource;
            motion.pointA = new Vector3(-12f, 1.2f, -2f);
            motion.pointB = new Vector3(12f, 1.2f, -2f);

            EnsureLayer(OcclusionLowPass.LayerName);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Occlusion Wall";
            wall.layer = LayerMask.NameToLayer(OcclusionLowPass.LayerName);
            wall.transform.position = new Vector3(0f, 1.5f, 11f);
            wall.transform.localScale = new Vector3(8f, 3f, 0.45f);
            wall.GetComponent<Renderer>().sharedMaterial = Mat("Wall", new Color(0.35f, 0.38f, 0.42f));
            wall.AddComponent<WorldLabel>().text = "Wall";

            AudioSource hidden = Tone("Occluded Source", new Vector3(0f, 1.2f, 14f), clips.Beacon, new Color(0.62f, 0.35f, 0.9f), 1f, 0.65f, true);
            hidden.pitch = 0.5f;
            var occlusion = hidden.gameObject.AddComponent<OcclusionLowPass>();
            occlusion.source = hidden;
            occlusion.obstacles = OcclusionLowPass.Mask();

            AudioReverbZone zone = ReverbDisc(new Vector3(-11f, 0.05f, 2f));
            zone.gameObject.AddComponent<WorldLabel>().text = "Reverb Zone";

            var pool = rig.gameObject.AddComponent<OneShotPool>();
            pool.clip = clips.Footstep;
            pool.follow = rig;
            pool.spatialBlend = 1f;
            pool.volume = 0.9f;

            Guide(
                "AudioSource: 2D and 3D",
                "Walk with WASD. Click the view, then look with the mouse. Shift runs. Esc unlocks the cursor.\n\n" +
                "The cyan sphere is 2D (spatial blend 0). It stays in both speakers as you walk.\n" +
                "The orange beacon is 3D. It pans and falls off with distance. Green and orange rings are min and max distance.\n" +
                "The yellow hybrid source is the settings lab. B cycles 2D, 3D, and hybrid. R cycles rolloff. V toggles spread.\n" +
                "Select Hybrid Source and watch spatialBlend, dopplerLevel, spread, minDistance, maxDistance, and priority.\n\n" +
                "The moving cube is a Doppler shuttle. Stand still and let it pass. Its velocity update mode is Dynamic, so a moving transform (no Rigidbody) still shifts pitch.\n" +
                "The purple source sits behind the wall. The wall is on the AudioOcclusion layer. OcclusionLowPass raycasts only that layer, so the player and other objects are not barriers. Walk around the wall.\n" +
                "The green disc is an AudioReverbZone using the Cave preset. Step inside it.\n" +
                "Space plays a footstep from OneShotPool at your feet.\n\n" +
                Footer());
            Save("01_AudioSource_2D3D");
        }

        static void BuildListener(Clips clips)
        {
            BeginScene();
            Transform rig = MakePlayer(new Vector3(0f, 0f, -6f), true);
            Floor();
            Camera camera = Camera.main;
            AudioListener cameraListener = camera.GetComponent<AudioListener>();
            if (cameraListener != null)
                cameraListener.enabled = false;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Camera Body";
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(camera.transform, false);
            body.transform.localPosition = new Vector3(0f, -0.15f, 0.25f);
            body.transform.localScale = new Vector3(0.2f, 0.12f, 0.24f);
            body.GetComponent<Renderer>().sharedMaterial = Mat("CameraBody", new Color(0.75f, 0.78f, 0.82f));

            var earObject = new GameObject("Ear");
            earObject.transform.SetParent(camera.transform, false);
            earObject.AddComponent<AudioListener>();
            var earMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earMesh.name = "Ear Mesh";
            UnityEngine.Object.DestroyImmediate(earMesh.GetComponent<Collider>());
            earMesh.transform.SetParent(earObject.transform, false);
            earMesh.transform.localPosition = new Vector3(0.22f, -0.12f, 0.2f);
            earMesh.transform.localScale = Vector3.one * 0.16f;
            earMesh.GetComponent<Renderer>().sharedMaterial = Mat("Ear", new Color(0.95f, 0.82f, 0.2f));
            earObject.AddComponent<WorldLabel>().text = "Listener";

            Tone("Left Beacon", new Vector3(-5f, 1.2f, 5f), clips.Beacon, new Color(0.95f, 0.4f, 0.15f), 1f, 0.7f, true);
            Tone("Right Tone", new Vector3(5f, 1.2f, 5f), clips.Doppler, new Color(0.25f, 0.55f, 0.95f), 1f, 0.35f, true);
            var focus = new GameObject("Focus");
            focus.transform.position = new Vector3(0f, 1.2f, 5f);

            var rigComponent = rig.gameObject.AddComponent<ListenerPerspectiveRig>();
            rigComponent.view = camera.transform;
            rigComponent.ear = earObject.transform;
            rigComponent.focus = focus.transform;
            rigComponent.lockedToView = true;

            Guide(
                "AudioListener perspective",
                "Unity hears through the AudioListener, not through the camera. The gray box is the camera. The yellow sphere is the ear.\n\n" +
                "WASD and the mouse move the camera. Q and E fly up and down.\n" +
                "F puts the ear on the camera (first person).\n" +
                "T parks the ear behind the view (third person).\n" +
                "O lifts the ear overhead.\n" +
                "L detaches the ear. Y moves it forward, G left, H back, and J right, so the arrow keys can keep driving the player. Page Up and Page Down change its height. The camera can then move without moving what you hear.\n\n" +
                "Select Listener Rig. listenerPosition, distanceToFocus, and perspectiveName update while you play.\n" +
                "Both sources are fully 3D, so the balance changes when the ear moves and stays put when only the camera moves.\n\n" +
                Footer());
            Save("02_AudioListener_Perspective");
        }

        static void BuildInput(Clips clips)
        {
            BeginScene();
            Camera camera = Camera.main;
            camera.transform.position = new Vector3(0f, 2.1f, -8f);
            camera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            StyleCamera(camera);
            Floor();

            var musicObject = new GameObject("Analysis Clip");
            AudioSource music = musicObject.AddComponent<AudioSource>();
            music.clip = clips.MusicA;
            music.loop = true;
            music.playOnAwake = true;
            music.spatialBlend = 0f;
            music.volume = 0.55f;
            music.dopplerLevel = 0f;

            var micObject = new GameObject("Microphone");
            AudioSource micSource = micObject.AddComponent<AudioSource>();
            micSource.playOnAwake = false;
            micSource.spatialBlend = 0f;
            var capture = micObject.AddComponent<MicrophoneCapture>();
            capture.source = micSource;
            capture.toggleKey = KeyCode.Space;
            capture.monitorVolume = 0f;
            var peak = micObject.AddComponent<MicrophonePeakMeter>();
            peak.capture = capture;
            var devices = micObject.AddComponent<AudioDevicePanel>();
            devices.capture = capture;

            var analyzerObject = new GameObject("Band Analyzer");
            var analyzer = analyzerObject.AddComponent<FrequencyBandAnalyzer>();
            analyzer.input = AnalysisInput.AudioSource;
            analyzer.source = music;
            analyzer.microphone = capture;
            analyzer.keyboardToggle = true;
            analyzer.fftSize = 2048;

            var bars = new GameObject("Band Bars");
            bars.transform.position = new Vector3(1.6f, 0f, 0.5f);
            var visualizer = bars.AddComponent<BandBarVisualizer>();
            visualizer.analyzer = analyzer;

            var peakBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            peakBar.name = "Mic Peak Bar";
            UnityEngine.Object.DestroyImmediate(peakBar.GetComponent<Collider>());
            peakBar.transform.position = new Vector3(-2.2f, 0.2f, 0.5f);
            peakBar.transform.localScale = new Vector3(0.45f, 0.08f, 0.45f);
            peakBar.GetComponent<Renderer>().sharedMaterial = Mat("Peak", new Color(0.95f, 0.25f, 0.35f));
            peakBar.AddComponent<WorldLabel>().text = "Mic Peak";
            var level = peakBar.AddComponent<LevelBar>();
            level.microphone = peak;

            var output = musicObject.AddComponent<OutputLevelMeter>();
            output.source = music;

            Guide(
                "Input, output, and meters",
                "The music clip starts immediately so the bars have something to analyze. Space starts or stops the microphone. H switches the analyzer between that clip and the mic.\n" +
                "N cycles input devices. K toggles monitor volume. Leave the monitor off unless you are on headphones, or the mic will howl.\n\n" +
                "Select Microphone while playing. peak, peakDecibels, and rms are the public levels from the capture buffer.\n" +
                "Select Band Analyzer. Each row in bands has a public level. Change minHz, maxHz, or boost, or add a row. The colored bars read those levels.\n" +
                "Select Analysis Clip for OutputLevelMeter, which reads AudioSource.GetOutputData (the source after its own volume).\n\n" +
                "The panel on the right lists Microphone.devices and the active output configuration: sample rate, DSP buffer, speaker mode, and voice counts. " +
                "Unity does not offer a public list of output device names. The buttons call AudioSettings.Reset to change buffer size or speaker mode, which restarts audio.\n\n" +
                "Menu is M, not Space, in this scene.\n\n" +
                Footer(),
                480f);
            Save("03_InputOutput_Meters");
        }

        static void BuildMixer(Clips clips, AudioMixer mixer)
        {
            BeginScene();
            Camera camera = Camera.main;
            camera.transform.position = new Vector3(0f, 2.2f, -9f);
            camera.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
            StyleCamera(camera);
            Floor();

            AudioMixerGroup musicGroup = Group(mixer, "Music");
            AudioMixerGroup ambienceGroup = Group(mixer, "Ambience");
            AudioMixerGroup sfxGroup = Group(mixer, "Sfx");
            AudioMixerGroup voiceGroup = Group(mixer, "Voice");
            Bus("Music Bus", new Vector3(-4.5f, 1f, 1f), clips.MusicA, musicGroup, new Color(0.25f, 0.6f, 0.95f), 0.6f);
            Bus("Ambience Bus", new Vector3(-1.5f, 1f, 1f), clips.Ambience, ambienceGroup, new Color(0.3f, 0.75f, 0.45f), 0.45f);
            Bus("Sfx Bus", new Vector3(1.5f, 1f, 1f), clips.Sfx, sfxGroup, new Color(0.95f, 0.55f, 0.2f), 0.4f);
            Bus("Voice Bus", new Vector3(4.5f, 1f, 1f), clips.Voice, voiceGroup, new Color(0.85f, 0.4f, 0.7f), 0.55f);

            AudioMixerSnapshot neutral = Snapshot("Neutral");
            AudioMixerSnapshot dialogue = Snapshot("DialogueFocus");
            AudioMixerSnapshot underwater = Snapshot("Underwater");
            AudioMixerSnapshot outdoor = Snapshot("Outdoor");

            var crossfadeObject = new GameObject("Snapshot Crossfader");
            var crossfade = crossfadeObject.AddComponent<SnapshotCrossfader>();
            crossfade.mixer = mixer;
            crossfade.snapshotA = neutral;
            crossfade.snapshotB = underwater;
            crossfade.catalog = new[] { neutral, dialogue, underwater, outdoor };
            crossfade.transitionSeconds = 0.9f;

            var volumes = crossfadeObject.AddComponent<ExposedVolumeKeys>();
            volumes.mixer = mixer;

            Guide(
                "Mixer groups and snapshots",
                "Four looping sources feed four groups under Master: Music, Ambience, Sfx, and Voice. Open Window > Audio > Audio Mixer and select DemoMixer to see them.\n\n" +
                "Keys 1 through 9 crossfade snapshot A into snapshot B. 1 is entirely A, 9 is entirely B, 5 is even.\n" +
                "Y cycles snapshot A. U cycles snapshot B. The set is Neutral, DialogueFocus, Underwater, and Outdoor.\n" +
                "Underwater lowers music and effects and closes a low-pass on Master. DialogueFocus ducks music, ambience, and effects so the voice group leads.\n\n" +
                "Z X C V B select Master, Music, Ambience, Sfx, and Voice.\n" +
                "Up and Down nudge that exposed volume by 2 dB. SetFloat takes the parameter away from snapshots.\n" +
                "Backspace calls ClearFloat on every exposed parameter and returns control to the snapshots.\n\n" +
                "Select Snapshot Crossfader. blend, weightA, and weightB are the live mix.\n\n" +
                Footer());
            Save("04_Mixer_Groups_Snapshots");
        }

        static void BuildCrossfade(Clips clips)
        {
            BeginScene();
            Camera camera = Camera.main;
            camera.transform.position = new Vector3(0f, 2.2f, -8.5f);
            camera.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            StyleCamera(camera);
            Floor();
            var transport = new GameObject("Transport").AddComponent<SharedTransport>();

            AudioSource musicA = FlatSource("Music A", clips.MusicA, 0.7f);
            AudioSource musicB = FlatSource("Music B", clips.MusicB, 0.7f);
            var crossfade = new GameObject("Source Crossfader").AddComponent<AudioSourceCrossfader>();
            crossfade.sourceA = musicA;
            crossfade.sourceB = musicB;
            crossfade.transport = transport;
            crossfade.law = CrossfadeLaw.EqualPower;
            crossfade.transitionSeconds = 0.8f;

            AudioSource ambience = FlatSource("Ambience Stem", clips.Ambience, 0.35f);
            AudioSource drone = FlatSource("Drone Stem", clips.Drone, 0.28f);
            var stems = new GameObject("Stems").AddComponent<SyncedStemPlayer>();
            stems.transport = transport;
            stems.stems = new[]
            {
                new StemLayer { name = "Ambience", source = ambience, clip = clips.Ambience, gain = 0.35f, audible = true, toggleKey = KeyCode.Z },
                new StemLayer { name = "Drone", source = drone, clip = clips.Drone, gain = 0.28f, audible = false, toggleKey = KeyCode.X }
            };

            AudioSource pad = FlatSource("Fade Pad", clips.Radio, 0.35f);
            pad.playOnAwake = true;
            var fader = pad.gameObject.AddComponent<AudioFader>();
            fader.source = pad;
            fader.fadeOutKey = KeyCode.Comma;
            fader.fadeInKey = KeyCode.Period;
            fader.seconds = 1.4f;

            var listenerTap = camera.gameObject.AddComponent<ListenerSpectrumTap>();
            var analyzer = new GameObject("Band Analyzer").AddComponent<FrequencyBandAnalyzer>();
            analyzer.input = AnalysisInput.ListenerOutput;
            analyzer.listenerTap = listenerTap;
            analyzer.fftSize = 2048;
            var bars = new GameObject("Band Bars");
            bars.transform.position = new Vector3(1.8f, 0f, 1f);
            bars.AddComponent<BandBarVisualizer>().analyzer = analyzer;

            var peakBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            peakBar.name = "Output Peak";
            UnityEngine.Object.DestroyImmediate(peakBar.GetComponent<Collider>());
            peakBar.transform.position = new Vector3(-2.4f, 0.2f, 1f);
            peakBar.transform.localScale = new Vector3(0.45f, 0.08f, 0.45f);
            peakBar.GetComponent<Renderer>().sharedMaterial = Mat("OutputPeak", new Color(0.95f, 0.3f, 0.35f));
            peakBar.AddComponent<WorldLabel>().text = "Output Peak";
            peakBar.AddComponent<LevelBar>().listenerTap = listenerTap;

            var clock = new GameObject("Music Clock").AddComponent<MusicClock>();
            clock.transport = transport;
            clock.bpm = 120f;

            var poolObject = new GameObject("One Shots");
            var pool = poolObject.AddComponent<OneShotPool>();
            pool.clip = clips.Beep;
            pool.spatialBlend = 0f;
            pool.volume = 0.6f;
            pool.trigger = KeyCode.Space;

            Guide(
                "Crossfades, FFT, and music",
                "Keys 1 through 9 crossfade Music A into Music B. 1 is A, 9 is B. Both stay playing. The law is equal-power, so the middle is not twice as loud.\n" +
                "Select Source Crossfader and read blend, sourceAGain, and sourceBGain.\n\n" +
                "Z toggles the ambience stem. X toggles the drone. SharedTransport gives every scheduled source the same DSP start, and SyncedStemPlayer uses PlayScheduled.\n" +
                "Comma fades the pad out. Period fades it back in. That is AudioFader on one source, separate from the crossfade.\n\n" +
                "The bars analyze the listener mix through ListenerSpectrumTap and OnAudioFilterRead, so they follow what you hear, including the crossfade.\n" +
                "Select Listener Tap on the camera for the output peak. Select Band Analyzer to edit ranges. Each band's level is a public float.\n" +
                "Music Clock counts quarter notes at 120 BPM from that same DSP start. pulse peaks on each beat.\n" +
                "Space plays a pooled one-shot.\n\n" +
                Footer());
            Save("05_Crossfade_FFT_Music");
        }

        static string Footer()
        {
            return "M menu. P pauses every voice (AudioListener.pause). [ and ] change AudioListener.volume.\n" +
                   "Click the Game view so the editor does not take the keys.";
        }

        static SceneMenu.Entry Entry(string label, string scene, KeyCode key)
        {
            return new SceneMenu.Entry { label = label, sceneName = scene, key = key };
        }

        static void BeginScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.5f);
            Light light = UnityEngine.Object.FindAnyObjectByType<Light>();
            if (light != null)
            {
                light.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
                light.intensity = 1.15f;
            }
        }

        static void Save(string sceneName)
        {
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SceneDir + "/" + sceneName + ".unity");
        }

        static Transform MakePlayer(Vector3 position, bool fly)
        {
            Camera camera = Camera.main;
            StyleCamera(camera);
            var rig = new GameObject("Listener Rig");
            rig.transform.position = position;
            camera.transform.SetParent(rig.transform, false);
            camera.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camera.transform.localRotation = Quaternion.identity;
            var look = rig.AddComponent<LookMoveController>();
            look.view = camera.transform;
            look.fly = fly;
            return rig.transform;
        }

        static void StyleCamera(Camera camera)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
        }

        static void Floor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(5f, 1f, 5f);
            floor.GetComponent<Renderer>().sharedMaterial = Mat("Floor", new Color(0.16f, 0.17f, 0.19f));
        }

        static AudioSource Tone(string name, Vector3 position, AudioClip clip, Color color, float spatial, float volume, bool rings)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.7f;
            go.GetComponent<Renderer>().sharedMaterial = Mat(Sanitize(name), color);
            var label = go.AddComponent<WorldLabel>();
            label.text = name;
            label.color = color;
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = volume;
            source.spatialBlend = spatial;
            source.dopplerLevel = spatial > 0.5f ? 1f : 0f;
            source.minDistance = 1.8f;
            source.maxDistance = 24f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            if (rings)
                go.AddComponent<DistanceRing>();
            return source;
        }

        static void Bus(string name, Vector3 position, AudioClip clip, AudioMixerGroup group, Color color, float volume)
        {
            AudioSource source = Tone(name, position, clip, color, 0f, volume, false);
            source.outputAudioMixerGroup = group;
            var route = source.gameObject.AddComponent<MixerGroupOutput>();
            route.source = source;
            route.group = group;
        }

        static AudioSource FlatSource(string name, AudioClip clip, float volume)
        {
            var go = new GameObject(name);
            AudioSource source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.dopplerLevel = 0f;
            return source;
        }

        static AudioReverbZone ReverbDisc(Vector3 position)
        {
            var zoneObject = new GameObject("Reverb Zone");
            zoneObject.transform.position = position;
            AudioReverbZone zone = zoneObject.AddComponent<AudioReverbZone>();
            zone.reverbPreset = AudioReverbPreset.Cave;
            zone.minDistance = 2f;
            zone.maxDistance = 7f;
            zoneObject.AddComponent<ReverbZoneReadout>();

            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Reverb Disc";
            UnityEngine.Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(zoneObject.transform, false);
            disc.transform.localScale = new Vector3(zone.maxDistance * 2f, 0.02f, zone.maxDistance * 2f);
            Material material = Mat("Reverb", new Color(0.2f, 0.8f, 0.45f, 0.28f));
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            disc.GetComponent<Renderer>().sharedMaterial = material;
            return zone;
        }

        static void Guide(string title, string body, float width = 720f)
        {
            var guide = new GameObject("Guide");
            DemoOverlay overlay = guide.AddComponent<DemoOverlay>();
            overlay.title = title;
            overlay.body = body;
            overlay.width = width;
            guide.AddComponent<ReturnToMenu>();
            guide.AddComponent<AudioPauseKey>();
        }

        static AudioMixerGroup Group(AudioMixer mixer, string name)
        {
            AudioMixerGroup[] groups = mixer.FindMatchingGroups(name);
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] != null && groups[i].name == name)
                    return groups[i];
            }

            throw new InvalidOperationException("Missing mixer group " + name);
        }

        static AudioMixerSnapshot Snapshot(string name)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(MixerPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AudioMixerSnapshot snapshot && snapshot.name == name)
                    return snapshot;
            }

            throw new InvalidOperationException("Missing snapshot " + name);
        }

        static Clips LoadClips()
        {
            return new Clips
            {
                MusicA = Prepare(Root + "/Clips/music_a.wav", false),
                MusicB = Prepare(Root + "/Clips/music_b.wav", false),
                Ambience = Prepare(Root + "/Clips/ambience.wav", false),
                Radio = Prepare(Root + "/Clips/radio.wav", false),
                Voice = Prepare(Root + "/Clips/voice.wav", true),
                Sfx = Prepare(Root + "/Clips/sfx_loop.wav", true),
                Beacon = Prepare(Root + "/Clips/beacon.wav", true),
                Drone = Prepare(Root + "/Clips/drone.wav", true),
                Doppler = Prepare(Root + "/Clips/doppler_tone.wav", true),
                Beep = Prepare(Root + "/Clips/oneshot_beep.wav", true),
                Footstep = Prepare(Root + "/Clips/footstep.wav", true)
            };
        }

        static AudioClip Prepare(string path, bool forceMono)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
                throw new InvalidOperationException("Missing audio clip at " + path);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = forceMono;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
                throw new InvalidOperationException("Unity did not import " + path);
            return clip;
        }

        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        static Material Mat(string name, Color color)
        {
            if (Materials.TryGetValue(name, out Material cached) && cached != null)
            {
                cached.color = color;
                return cached;
            }

            string path = Root + "/Materials/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse") ?? Shader.Find("Sprites/Default");
                material = new Material(shader);
                material.color = color;
                if (material.HasProperty("_Glossiness"))
                    material.SetFloat("_Glossiness", 0.12f);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.color = color;
            }

            Materials[name] = material;
            return material;
        }

        static string Sanitize(string name)
        {
            return name.Replace(" ", string.Empty);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static void EnsureLayer(string layerName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
                throw new System.InvalidOperationException("TagManager is missing.");
            SerializedObject tags = new SerializedObject(assets[0]);
            SerializedProperty layers = tags.FindProperty("layers");
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                    return;
            }

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(slot.stringValue))
                    continue;
                slot.stringValue = layerName;
                tags.ApplyModifiedProperties();
                return;
            }

            throw new System.InvalidOperationException("No empty layer slot for " + layerName);
        }

        struct Clips
        {
            public AudioClip MusicA;
            public AudioClip MusicB;
            public AudioClip Ambience;
            public AudioClip Radio;
            public AudioClip Voice;
            public AudioClip Sfx;
            public AudioClip Beacon;
            public AudioClip Drone;
            public AudioClip Doppler;
            public AudioClip Beep;
            public AudioClip Footstep;
        }
    }
}
