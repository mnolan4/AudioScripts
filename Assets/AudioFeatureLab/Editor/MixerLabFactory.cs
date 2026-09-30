using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioFeatureLab.EditorTools
{
    static class MixerLabFactory
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static AudioMixer Create(string path)
        {
            Type controllerType = Require("UnityEditor.Audio.AudioMixerController");
            MethodInfo create = controllerType.GetMethod("CreateMixerControllerAtPath", Any);
            if (create == null)
                throw new MissingMethodException("AudioMixerController.CreateMixerControllerAtPath");

            object mixer = create.Invoke(null, new object[] { path });
            object master = controllerType.GetProperty("masterGroup", Any).GetValue(mixer);
            object music = AddGroup(mixer, master, "Music");
            object ambience = AddGroup(mixer, master, "Ambience");
            object sfx = AddGroup(mixer, master, "Sfx");
            object voice = AddGroup(mixer, master, "Voice");

            LowpassBinding lowpass = TryAddLowpass(mixer, master);
            object neutral = controllerType.GetProperty("TargetSnapshot", Any).GetValue(mixer);
            ((UnityEngine.Object)neutral).name = "Neutral";
            object dialogue = CloneSnapshot(mixer, neutral, "DialogueFocus");
            object underwater = CloneSnapshot(mixer, neutral, "Underwater");
            object outdoor = CloneSnapshot(mixer, neutral, "Outdoor");

            ApplyVolumes(mixer, neutral, master, music, ambience, sfx, voice, 0f, 0f, 0f, 0f, -4f);
            ApplyVolumes(mixer, dialogue, master, music, ambience, sfx, voice, 0f, -14f, -12f, -10f, 0f);
            ApplyVolumes(mixer, underwater, master, music, ambience, sfx, voice, -2f, -8f, -3f, -6f, -2f);
            ApplyVolumes(mixer, outdoor, master, music, ambience, sfx, voice, 0f, -3f, 2f, 0f, -4f);
            if (lowpass != null)
            {
                ApplyCutoff(underwater, lowpass, lowpass.closed);
                ApplyCutoff(neutral, lowpass, lowpass.open);
                ApplyCutoff(dialogue, lowpass, lowpass.open);
                ApplyCutoff(outdoor, lowpass, lowpass.open);
            }

            controllerType.GetProperty("startSnapshot", Any).SetValue(mixer, neutral);
            controllerType.GetProperty("TargetSnapshot", Any).SetValue(mixer, neutral);
            ExposeVolume(mixer, master, "MasterVolume");
            ExposeVolume(mixer, music, "MusicVolume");
            ExposeVolume(mixer, ambience, "AmbienceVolume");
            ExposeVolume(mixer, sfx, "SfxVolume");
            ExposeVolume(mixer, voice, "VoiceVolume");
            TrySetView(mixer, new[] { master, music, ambience, sfx, voice });

            EditorUtility.SetDirty((UnityEngine.Object)mixer);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            Debug.Log(lowpass == null
                ? "Demo mixer created with volume snapshots."
                : "Demo mixer created. " + lowpass.report);
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
        }

        static object AddGroup(object mixer, object parent, string name)
        {
            Type mixerType = mixer.GetType();
            object group = mixerType.GetMethod("CreateNewGroup", Any).Invoke(mixer, new object[] { name, false });
            mixerType.GetMethod("AddChildToParent", Any).Invoke(mixer, new object[] { group, parent });
            return group;
        }

        static object CloneSnapshot(object mixer, object source, string name)
        {
            Type mixerType = mixer.GetType();
            mixerType.GetProperty("TargetSnapshot", Any).SetValue(mixer, source);
            mixerType.GetMethod("CloneNewSnapshotFromTarget", Any).Invoke(mixer, new object[] { false });
            object clone = mixerType.GetProperty("TargetSnapshot", Any).GetValue(mixer);
            ((UnityEngine.Object)clone).name = name;
            mixerType.GetProperty("TargetSnapshot", Any).SetValue(mixer, source);
            return clone;
        }

        static void ApplyVolumes(
            object mixer,
            object snapshot,
            object master,
            object music,
            object ambience,
            object sfx,
            object voice,
            float masterDb,
            float musicDb,
            float ambienceDb,
            float sfxDb,
            float voiceDb)
        {
            SetVolume(mixer, snapshot, master, masterDb);
            SetVolume(mixer, snapshot, music, musicDb);
            SetVolume(mixer, snapshot, ambience, ambienceDb);
            SetVolume(mixer, snapshot, sfx, sfxDb);
            SetVolume(mixer, snapshot, voice, voiceDb);
        }

        static void SetVolume(object mixer, object snapshot, object group, float decibels)
        {
            MethodInfo method = group.GetType().GetMethod("SetValueForVolume", Any);
            method.Invoke(group, new[] { mixer, snapshot, decibels });
        }

        static void ExposeVolume(object mixer, object group, string parameterName)
        {
            Type exposedType = Require("UnityEditor.Audio.ExposedAudioParameter");
            object guid = group.GetType().GetMethod("GetGUIDForVolume", Any).Invoke(group, null);
            object parameter = Activator.CreateInstance(exposedType);
            exposedType.GetField("guid", Any).SetValue(parameter, guid);
            exposedType.GetField("name", Any).SetValue(parameter, parameterName);

            PropertyInfo property = mixer.GetType().GetProperty("exposedParameters", Any);
            Array current = (Array)property.GetValue(mixer);
            int count = current == null ? 0 : current.Length;
            Array next = Array.CreateInstance(exposedType, count + 1);
            for (int i = 0; i < count; i++)
                next.SetValue(current.GetValue(i), i);
            next.SetValue(parameter, count);
            property.SetValue(mixer, next);
        }

        static void TrySetView(object mixer, object[] groups)
        {
            try
            {
                Type viewType = Require("UnityEditor.Audio.MixerGroupView");
                Type guidType = typeof(GUID);
                Array guids = Array.CreateInstance(guidType, groups.Length);
                for (int i = 0; i < groups.Length; i++)
                {
                    object id = groups[i].GetType().GetProperty("groupID", Any).GetValue(groups[i]);
                    guids.SetValue(id, i);
                }

                object view = Activator.CreateInstance(viewType);
                viewType.GetField("name", Any).SetValue(view, "All");
                viewType.GetField("guids", Any).SetValue(view, guids);
                Array views = Array.CreateInstance(viewType, 1);
                views.SetValue(view, 0);
                mixer.GetType().GetProperty("views", Any).SetValue(mixer, views);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Mixer group view was left at the Unity default. " + exception.Message);
            }
        }

        sealed class LowpassBinding
        {
            public object parameterGuid;
            public float open;
            public float closed;
            public string report;
        }

        static LowpassBinding TryAddLowpass(object mixer, object master)
        {
            try
            {
                string[] names = EffectNames();
                Debug.Log("Mixer effects: " + string.Join(", ", names));
                string effectName = null;
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i] != null && names[i].IndexOf("Lowpass", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        effectName = names[i];
                        break;
                    }
                }

                if (effectName == null)
                    return null;

                Type effectType = Require("UnityEditor.Audio.AudioMixerEffectController");
                object effect = Activator.CreateInstance(effectType, effectName);
                effectType.GetMethod("PreallocateGUIDs", Any).Invoke(effect, null);
                AssetDatabase.AddObjectToAsset((UnityEngine.Object)effect, (UnityEngine.Object)mixer);
                master.GetType().GetMethod("InsertEffect", Any).Invoke(master, new[] { effect, 1 });

                string parameterName = null;
                float min = 0f;
                float max = 22000f;
                Array definitions = EffectParameters(effectName);
                if (definitions != null)
                {
                    for (int i = 0; i < definitions.Length; i++)
                    {
                        object definition = definitions.GetValue(i);
                        string candidate = FieldString(definition, "name");
                        float minRange = FieldFloat(definition, "minRange", 0f);
                        float maxRange = FieldFloat(definition, "maxRange", 22000f);
                        Debug.Log(effectName + " parameter " + candidate + " range " + minRange + " to " + maxRange);
                        if (parameterName == null
                            || candidate.IndexOf("Cutoff", StringComparison.OrdinalIgnoreCase) >= 0
                            || candidate.IndexOf("Freq", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            parameterName = candidate;
                            min = minRange;
                            max = maxRange;
                        }
                    }
                }

                if (string.IsNullOrEmpty(parameterName))
                    return null;

                object guid = effectType.GetMethod("GetGUIDForParameter", Any).Invoke(effect, new object[] { parameterName });
                float open = max;
                float closed = max > 1000f ? 700f : Mathf.Lerp(min, max, 0.12f);
                return new LowpassBinding
                {
                    parameterGuid = guid,
                    open = open,
                    closed = closed,
                    report = "Lowpass '" + effectName + "' parameter '" + parameterName + "' open " + open + " closed " + closed + "."
                };
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Lowpass effect was not added. Snapshots still differ by volume. " + exception);
                return null;
            }
        }

        static void ApplyCutoff(object snapshot, LowpassBinding binding, float value)
        {
            MethodInfo setValue = snapshot.GetType().GetMethod("SetValue", Any);
            setValue.Invoke(snapshot, new[] { binding.parameterGuid, value });
        }

        static string[] EffectNames()
        {
            Type definitions = Require("UnityEditor.Audio.MixerEffectDefinitions");
            object value = definitions.GetMethod("GetEffectList", Any).Invoke(null, null);
            if (value is string[] names)
                return names;
            if (value is Array array)
            {
                string[] copy = new string[array.Length];
                for (int i = 0; i < array.Length; i++)
                    copy[i] = array.GetValue(i).ToString();
                return copy;
            }

            return new string[0];
        }

        static Array EffectParameters(string effectName)
        {
            Type definitions = Require("UnityEditor.Audio.MixerEffectDefinitions");
            object value = definitions.GetMethod("GetEffectParameters", Any).Invoke(null, new object[] { effectName });
            return value as Array;
        }

        static string FieldString(object item, string fieldName)
        {
            FieldInfo field = item.GetType().GetField(fieldName, Any);
            object value = field != null ? field.GetValue(item) : null;
            return value != null ? value.ToString() : string.Empty;
        }

        static float FieldFloat(object item, string fieldName, float fallback)
        {
            FieldInfo field = item.GetType().GetField(fieldName, Any);
            if (field == null)
                return fallback;
            object value = field.GetValue(item);
            if (value is float number)
                return number;
            return fallback;
        }

        static Type Require(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }

            throw new InvalidOperationException("Could not find " + fullName);
        }
    }
}
