using UnityEngine;

namespace LostRelic
{
    public class AudioService : MonoBehaviour
    {
        private static AudioService _instance;
        private AudioSource _bgmSource;
        private AudioSource _sfxSource;

        public static AudioService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("LostRelicAudio");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<AudioService>();
                }

                return _instance;
            }
        }

        private void Awake()
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
            _bgmSource.volume = 0.8f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.volume = 1f;
        }

        public static void PlayBgm(AudioClip clip)
        {
            var audio = Instance;
            if (clip == null || audio._bgmSource.clip == clip)
            {
                return;
            }

            audio._bgmSource.clip = clip;
            audio._bgmSource.Play();
        }

        public static void StopBgm()
        {
            if (_instance != null)
            {
                _instance._bgmSource.Stop();
            }
        }

        public static void PlayOneShot(AudioClip clip)
        {
            if (_instance == null || clip == null)
            {
                return;
            }

            _instance._sfxSource.PlayOneShot(clip);
        }

        public static void SetBgmVolume(float volume)
        {
            if (_instance != null)
            {
                _instance._bgmSource.volume = Mathf.Clamp01(volume);
            }
        }
    }
}
