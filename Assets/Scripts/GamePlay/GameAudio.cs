using System.Collections.Generic;
using UnityEngine;

namespace GamePlay
{
    public enum AudioChannel
    {
        Music,
        World,
        UI,
    }

    public enum SoundCue
    {
        FootstepWood,
        FootstepStone,
        FootstepDirt,
        CustomerArrival,
        Served,
        CookingSuccess,
        CookingMiss,
        HuntHit,
        PanelOpen,
        PanelClose,
        Select,
        BoardChop,
    }

    public sealed class GameAudio : MonoBehaviour
    {
        private static GameAudio _instance;
        private readonly Dictionary<SoundCue, AudioClip[]> _clips = new Dictionary<SoundCue, AudioClip[]>();
        private readonly Dictionary<SoundCue, int> _lastVariant = new Dictionary<SoundCue, int>();
        private AudioSource _music;
        private AudioSource _world;
        private AudioSource _ui;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            _instance = null;
        }

        private static GameAudio GetOrCreate()
        {
            if (_instance == null)
            {
                GameObject root = new GameObject("Game Audio");
                _instance = root.AddComponent<GameAudio>();
                DontDestroyOnLoad(root);
                _instance.Initialize();
            }

            return _instance;
        }

        private void Initialize()
        {
            _music = CreateSource(AudioChannel.Music);
            _world = CreateSource(AudioChannel.World);
            _ui = CreateSource(AudioChannel.UI);
        }

        private AudioSource CreateSource(AudioChannel channel)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = GetVolume(channel);
            return source;
        }

        public static float GetVolume(AudioChannel channel)
        {
            float defaultVolume = channel == AudioChannel.Music ? 0.35f : 0.65f;
            return Mathf.Clamp01(PlayerPrefs.GetFloat(GetVolumeKey(channel), defaultVolume));
        }

        public static void SetVolume(AudioChannel channel, float volume)
        {
            volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(GetVolumeKey(channel), volume);
            if (_instance != null)
            {
                _instance.GetSource(channel).volume = volume;
            }
        }

        public static void StartMusic()
        {
            AudioSource music = GetOrCreate()._music;
            if (music.isPlaying)
            {
                return;
            }

            music.clip = Resources.Load<AudioClip>("Audio/Music/Tavern");
            if (music.clip != null)
            {
                music.loop = true;
                music.Play();
            }
        }

        public static void Play(SoundCue cue, float gain = 1f)
        {
            GameAudio audio = GetOrCreate();
            bool isUi = cue is SoundCue.PanelOpen or SoundCue.PanelClose or SoundCue.Select;
            AudioSource source = isUi ? audio._ui : audio._world;
            if (source.volume <= 0f)
            {
                return;
            }

            if (!audio._clips.TryGetValue(cue, out AudioClip[] clips))
            {
                string folder = isUi ? "UI" : "World";
                clips = Resources.LoadAll<AudioClip>($"Audio/{folder}/{cue}");
                audio._clips.Add(cue, clips);
            }

            if (clips.Length == 1)
            {
                source.PlayOneShot(clips[0], Mathf.Clamp01(gain));
            }
            else if (clips.Length > 1)
            {
                int variant;
                if (audio._lastVariant.TryGetValue(cue, out int previous))
                {
                    variant = Random.Range(0, clips.Length - 1);
                    if (variant >= previous)
                    {
                        variant++;
                    }
                }
                else
                {
                    variant = Random.Range(0, clips.Length);
                }

                audio._lastVariant[cue] = variant;
                source.PlayOneShot(clips[variant], Mathf.Clamp01(gain));
            }
        }

        private AudioSource GetSource(AudioChannel channel)
        {
            return channel switch
            {
                AudioChannel.Music => _music,
                AudioChannel.World => _world,
                AudioChannel.UI => _ui,
                _ => throw new System.ArgumentOutOfRangeException(nameof(channel)),
            };
        }

        private static string GetVolumeKey(AudioChannel channel) => $"ProjectBB.Audio.{channel}";
    }
}
