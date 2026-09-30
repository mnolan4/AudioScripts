# User guide

This guide is for playing the lab and for changing it. The project root is the folder that contains `Assets` and `ProjectSettings`. Scripts are C# in the `AudioFeatureLab` namespace.

Click the Game view before using keys. The editor steals input when a Hierarchy or Inspector field is selected.

These keys work in every demo scene:

| Key | Action |
| --- | --- |
| M | Return to the main menu |
| P | Pause or resume all audio (`AudioListener.pause`) |
| [ and ] | Lower or raise `AudioListener.volume` by 0.1 |
| Esc | Unlock the mouse in scenes where you look around |
| Click | Lock the mouse for looking, in the walking scenes |

## 1. AudioSource, 2D and 3D

Scene: `01_AudioSource_2D3D`.

| Key | Action |
| --- | --- |
| WASD | Walk. Shift runs. |
| Mouse | Look after you click the view |
| B | Cycle the yellow source through 2D, 3D, and hybrid |
| R | Cycle rolloff: logarithmic, linear, custom |
| V | Toggle spread between 0 and 180 degrees |
| Space | Play a footstep at your feet |

What to listen for:

- The cyan **Radio 2D** source uses `spatialBlend = 0`. Walking does not pan it.
- The orange **Beacon 3D** source uses `spatialBlend = 1`, logarithmic rolloff, and min/max distance rings.
- The yellow **Hybrid Source** is driven by `SpatialAudioController`. Its public fields (`spatialBlend`, `dopplerLevel`, `spreadDegrees`, `minDistance`, `maxDistance`, `rolloff`, `panStereo`, `priority`) are copied onto the `AudioSource` every frame, so you can also edit them in the Inspector during Play.
- The moving cube uses `DopplerShuttle`. `velocityUpdateMode` is Dynamic, which tracks a transform that has no Rigidbody. Raise `dopplerLevel` on that component to exaggerate the pitch shift.
- The purple source uses `OcclusionLowPass`. `occlusion` goes toward 1 when an **AudioOcclusion** collider, here the wall, sits between the source and the listener. Walk past the wall and the low-pass opens. `cutoffHz` is the filter frequency.
- The green disc is an `AudioReverbZone` set to the Cave preset. `ReverbZoneReadout.wetAmount` is an estimate from distance; Unity applies the real reverb to the listener on its own.

### Change this scene

- Swap a clip on an `AudioSource` in the Inspector, or replace the WAV in `Assets/AudioFeatureLab/Clips` and keep the file name.
- To add another 3D source, duplicate **Beacon 3D**, assign a mono clip, and leave `DistanceRing` on it. Stereo clips are weak for spatialization; the builder forces mono on the 3D clips at import.
- Custom rolloff needs a curve on the `AudioSource`. Press R until the mode is Custom, then edit **Volume Rolloff** in the source Inspector. The controller will keep that mode selected until you press R again.
- Occlusion raycasts only the **AudioOcclusion** layer. Put walls, doors, and terrain on that layer. Leave the player, particles, collectibles, and enemies on other layers so they are not treated as acoustic barriers.

## 2. AudioListener perspective

Scene: `02_AudioListener_Perspective`.

The camera does not hear. The yellow sphere marks the `AudioListener`.

| Key | Action |
| --- | --- |
| WASD, mouse | Move and look. Q and E fly. |
| F | First person. The listener sits on the camera. |
| T | Third person. The listener sits behind the view. |
| O | Overhead. The listener sits above the view. |
| L | Detach the listener from the camera. |
| Y / G / H / J | Move a detached listener forward, left, back, and right. Arrow keys stay with the player. |
| Page Up / Page Down | Raise or lower a detached listener. |

`ListenerPerspectiveRig` writes `listenerPosition`, `distanceToFocus`, and `perspectiveName` every frame. `distanceToFocus` is the distance from the ear to the empty **Focus** object between the two sources.

Move only the camera while the ear is detached (L). The picture changes and the mix does not. Lock the ear again with F, T, or O and they move together.

### Change the offsets

Select **Listener Rig** and edit `firstPersonOffset`, `thirdPersonOffset`, or `overheadOffset`. Offsets are in the camera's local space: x is right, y is up, z is forward (negative z is behind the camera).

To follow a character instead of this camera, assign that transform to `view` and parent the ear the way the scene already does. Only one listener should be enabled. The rig turns the others off in `Awake`.

## 3. Input, output, and meters

Scene: `03_InputOutput_Meters`.

| Key | Action |
| --- | --- |
| Space | Start or stop the microphone |
| H | Analyze the music clip, or analyze the microphone |
| N | Next input device |
| K | Toggle monitor volume |

The bars animate as soon as you press Play because the analyzer starts on the music clip. Select **Microphone** and watch `peak`, `peakDecibels`, and `rms`. `peak` is a linear 0–1 level with a fast attack and a slower release. That is the public peak the other scripts can read.

Select **Band Analyzer**. The `bands` list is the set of ranges. Each element has:

| Field | Meaning |
| --- | --- |
| `name` | Label on the bar |
| `minHz`, `maxHz` | Range in hertz |
| `boost` | Extra sensitivity for quiet ranges |
| `level` | Public 0–1 output, smoothed |
| `energy` | Raw band energy before the 0–1 map |

Add a row with the + button on the list. Example: a vocal range of 300–3400 Hz named Voice. `BandBarVisualizer` rebuilds its cubes when the count changes. `GetLevel("Voice")` returns that row's `level`.

`fftSize` must be a power of two from 64 to 8192. For an `AudioSource`, the analyzer uses `GetSpectrumData` unless `useManualFft` is on. The microphone and the listener tap always use the lab's own FFT (`SimpleFft`), because those paths read sample buffers directly.

Monitor playback (`K`) sends the mic to the speakers. Use headphones. The meters read the capture buffer, so they still move when monitor volume is 0.

The panel on the right is `AudioDevicePanel`.

- Input names come from `Microphone.devices`.
- Output shows sample rate, DSP buffer size, speaker mode, driver capabilities, and real/virtual voice counts.
- Unity's public API does not list output device names. Changing the OS output device is done in the operating system. The panel can still call `AudioSettings.Reset` to change DSP buffer size (256, 512, 1024, 2048) or cycle Mono, Stereo, and Surround. Reset restarts the audio system.

`OutputLevelMeter` on **Analysis Clip** is the matching output-side meter for one source (`GetOutputData`). It is separate from the microphone peak.

### macOS microphone permission

If the device list is empty, allow the Unity editor (or the built player) to use the microphone in System Settings > Privacy & Security > Microphone, then press Play again.

## 4. Mixer groups and snapshots

Scene: `04_Mixer_Groups_Snapshots`.

The asset is `Assets/AudioFeatureLab/Mixers/DemoMixer.mixer`. Groups under Master:

| Group | Scene object | Exposed parameter |
| --- | --- | --- |
| Music | Music Bus | `MusicVolume` |
| Ambience | Ambience Bus | `AmbienceVolume` |
| Sfx | Sfx Bus | `SfxVolume` |
| Voice | Voice Bus | `VoiceVolume` |
| Master | (the parent) | `MasterVolume` |

Snapshots: **Neutral**, **DialogueFocus**, **Underwater**, **Outdoor**. Neutral is the startup snapshot.

| Key | Action |
| --- | --- |
| 1–9 | Crossfade snapshot A to snapshot B. 1 is A, 5 is the center, 9 is B. Keypad numbers do the same. |
| Y | Cycle snapshot A through the catalog |
| U | Cycle snapshot B through the catalog |
| Z X C V B | Select Master, Music, Ambience, Sfx, Voice |
| Up / Down | Change the selected exposed volume by 2 dB |
| Backspace | `ClearFloat` on every exposed parameter |

`SnapshotCrossfader.blend` moves over `transitionSeconds` (default 0.9 seconds). `weightA` and `weightB` are what `AudioMixer.TransitionToSnapshots` receives. They sum to 1.

Underwater is the obvious one: music and effects drop, and a low-pass on Master closes. DialogueFocus turns the voice group up and the other groups down.

### SetFloat and snapshots

Calling `AudioMixer.SetFloat` overrides a snapshot until `ClearFloat`. If the arrow keys seem to "stick" and 1–9 stop changing a group, press Backspace. `ExposedVolumeKeys.overrideActive` is true after a nudge.

### Change the mixer

- To send a new source to a group, add `MixerGroupOutput` and assign the group, or set `AudioSource.outputAudioMixerGroup` yourself. `AudioMixer.FindMatchingGroups("Music")` finds a group by name at runtime.
- To expose a different parameter, right-click it in the Audio Mixer inspector and choose **Expose to script**, then add that name to the `parameters` array on `ExposedVolumeKeys`.
- To add a snapshot, duplicate one in the Audio Mixer window, set group volumes on it, and add it to the `catalog` array on **Snapshot Crossfader**. Assign it to snapshot A or B if it should be one side of the 1–9 crossfade.
- Rebuild from scratch with **Audio Feature Lab > Build Demo Scenes**. That deletes and recreates `DemoMixer`, so hand-edits to that asset are replaced. Duplicate the mixer first if you want to keep them.

The builder creates the mixer through Unity's editor mixer types (they are internal), then the scenes use only the public `AudioMixer`, `AudioMixerGroup`, and `AudioMixerSnapshot` API.

## 5. Crossfades, FFT, and music

Scene: `05_Crossfade_FFT_Music`.

| Key | Action |
| --- | --- |
| 1–9 | Crossfade Music A to Music B |
| Z | Toggle the ambience stem |
| X | Toggle the drone stem |
| Comma | Fade the pad out |
| Period | Fade the pad in |
| Space | Play a one-shot from the pool |

`AudioSourceCrossfader` keeps both sources playing and sets their volumes. The default law is equal-power: at the center each gain is about 0.707, so the sum is not louder than either side alone. Switch `law` to Linear if you want a straight ramp. `masterGain` scales both after the law. `sourceAGain` and `sourceBGain` are the public gains.

`SharedTransport` records one future `AudioSettings.dspTime` in `Awake`. The crossfader and `SyncedStemPlayer` call `PlayScheduled` with that time, so the loops share a downbeat. `MusicClock` uses the same time at 120 BPM, which matches the generated music loops (eight eighth-notes over four seconds). `beat`, `bar`, `beatInBar`, and `pulse` are the public clock values.

The colored bars are not reading one clip. `ListenerSpectrumTap` is on the camera, next to the `AudioListener`. `OnAudioFilterRead` copies the mixed output. `FrequencyBandAnalyzer.input` is `ListenerOutput`. The red bar is that tap's `peak` (the output peak of the mix). Editing bands works the same way as in scene 3.

`AudioFader` on **Fade Pad** is a single-source fade. It does not share the crossfader, so comma and period do not move the 1–9 blend.

### Change the crossfade

- Replace `sourceA` or `sourceB`, or their clips. Leave both looping.
- `transitionSeconds` is how long a full 0-to-1 move takes. A jump from key 1 to key 9 uses the whole time. A jump from 4 to 5 uses one eighth of it.
- To crossfade snapshots instead, use `SnapshotCrossfader` as in scene 4. The key reader is `BlendKeyboard`: Alpha 1 and Keypad 1 are blend 0, Alpha 9 and Keypad 9 are blend 1.
- To add a stem, add an `AudioSource`, a `StemLayer` on **Stems**, and a `toggleKey`. Give it the same `SharedTransport`.

## Rebuilding clips and scenes

Regenerate the tones:

```bash
python3 Tools/generate_clips.py
```

Then in Unity, run **Audio Feature Lab > Build Demo Scenes**.

The menu imports each clip as PCM, decompress on load, and forces mono on the sources that are used in 3D. If you bring in your own music, drop it in `Assets/AudioFeatureLab/Clips` and assign it in the Inspector, or change the file names in `DemoProjectBuilder.LoadClips` and rebuild.

## Script map for common edits

| Goal | Where |
| --- | --- |
| Add a frequency range | `FrequencyBandAnalyzer.bands` on **Band Analyzer**, or `SpectrumBands.CreateDefaults` for the starting set |
| Change 1–9 behavior | `BlendKeyboard` and `transitionSeconds` on the crossfader |
| Change mic ballistics | `attackSeconds` and `releaseSeconds` on `MicrophonePeakMeter` |
| Hear a different bus | `MixerGroupOutput.group` |
| Move the ears | `ListenerPerspectiveRig` offsets |
| Schedule music to a grid | `MusicClock.bpm` and `SharedTransport.leadInSeconds` |
| Pool a different one-shot | `OneShotPool.clip`, `spatialBlend`, `pitchRange` |

Public output fields are plain fields so they show in the Inspector during Play and other components can read them. They are written by the script that owns them. Treat `peak`, `level`, `blend`, and `occlusion` as outputs unless you are deliberately driving `blend` from your own code (`SetBlend`).
