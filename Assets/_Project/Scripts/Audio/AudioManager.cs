using UnityEngine;
using TomorrowsCrime.Core;
using TomorrowsCrime.Settings;
using TomorrowsCrime.Simulation;

namespace TomorrowsCrime.Audio
{
    /// <summary>
    /// Central audio controller for Tomorrow's Crime.
    /// Manages procedural sound effects, atmospheric clock ticking, chime playback,
    /// and implements ILoopResetReceiver for dynamic time-loop audio transitions.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class AudioManager : MonoBehaviour, ILoopResetReceiver
    {
        private static AudioManager _instance;
        public static AudioManager Instance => _instance;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource tickSource;

        [Header("Settings")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.75f;

        // Cached Procedural AudioClips
        private AudioClip _clipFootstepWood;
        private AudioClip _clipFootstepSprint;
        private AudioClip _clipFootstepCarpet;
        private AudioClip _clipClockTick;
        private AudioClip _clipClockTock;
        private AudioClip _clipBellChime;
        private AudioClip _clipPaperRustle;
        private AudioClip _clipEvidenceChime;
        private AudioClip _clipDoorCreak;
        private AudioClip _clipLockClick;
        private AudioClip _clipVictory;
        private AudioClip _clipDefeat;
        private AudioClip _clipLoopRewind;

        // Clock Ticking State
        private float _tickTimer = 0f;
        private bool _tickTockState = false;
        private bool _isTickingActive = true;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            EnsureAudioSources();
            SynthesizeAllClips();
        }

        private void Start()
        {
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.RegisterResetReceiver(this);
            }
        }

        private void OnDestroy()
        {
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.UnregisterResetReceiver(this);
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void EnsureAudioSources()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f; // 2D crisp interface sound
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f;
            }

            if (tickSource == null)
            {
                tickSource = gameObject.AddComponent<AudioSource>();
                tickSource.playOnAwake = false;
                tickSource.spatialBlend = 0f;
            }
        }

        private void SynthesizeAllClips()
        {
            _clipFootstepWood = ProceduralAudioEngine.CreateFootstepClip(false, false);
            _clipFootstepSprint = ProceduralAudioEngine.CreateFootstepClip(true, false);
            _clipFootstepCarpet = ProceduralAudioEngine.CreateFootstepClip(false, true);
            _clipClockTick = ProceduralAudioEngine.CreateClockTickClip(false);
            _clipClockTock = ProceduralAudioEngine.CreateClockTickClip(true);
            _clipBellChime = ProceduralAudioEngine.CreateMidnightBellChime();
            _clipPaperRustle = ProceduralAudioEngine.CreatePaperRustleClip();
            _clipEvidenceChime = ProceduralAudioEngine.CreateEvidenceDiscoveryChime();
            _clipDoorCreak = ProceduralAudioEngine.CreateDoorCreakClip();
            _clipLockClick = ProceduralAudioEngine.CreateLockClickClip();
            _clipVictory = ProceduralAudioEngine.CreateVictoryStinger();
            _clipDefeat = ProceduralAudioEngine.CreateDefeatStinger();
            _clipLoopRewind = ProceduralAudioEngine.CreateLoopRewindSweep();
        }

        private void Update()
        {
            if (!_isTickingActive) return;

            // Update rhythmic clock tick once per second
            _tickTimer += Time.deltaTime;
            if (_tickTimer >= 1.0f)
            {
                _tickTimer -= 1.0f;
                _tickTockState = !_tickTockState;

                // Louder ticks when time is running low in the loop (< 45s)
                float remaining = SimulationManager.Instance != null && SimulationManager.Instance.Clock != null
                    ? SimulationManager.Instance.Clock.RemainingSeconds
                    : 300f;

                float tickVol = remaining < 45f ? 0.35f : 0.12f;
                PlaySoundOneShot(_tickTockState ? _clipClockTick : _clipClockTock, tickVol * sfxVolume);
            }
        }

        private void PlaySoundOneShot(AudioClip clip, float volumeScale = 1.0f)
        {
            if (clip == null || sfxSource == null) return;
            float finalVol = volumeScale * masterVolume;
            sfxSource.PlayOneShot(clip, finalVol);
        }

        public void PlayFootstep(bool isSprinting, bool isCarpet)
        {
            AudioClip clip = isCarpet ? _clipFootstepCarpet : (isSprinting ? _clipFootstepSprint : _clipFootstepWood);
            PlaySoundOneShot(clip, (isSprinting ? 0.45f : 0.30f) * sfxVolume);
        }

        public void PlayClockChime()
        {
            PlaySoundOneShot(_clipBellChime, 0.90f * sfxVolume);
        }

        public void PlayPaperRustle()
        {
            PlaySoundOneShot(_clipPaperRustle, 0.50f * sfxVolume);
        }

        public void PlayEvidenceDiscovery()
        {
            PlaySoundOneShot(_clipEvidenceChime, 0.85f * sfxVolume);
        }

        public void PlayDoorCreak()
        {
            PlaySoundOneShot(_clipDoorCreak, 0.60f * sfxVolume);
        }

        public void PlayLockClick()
        {
            PlaySoundOneShot(_clipLockClick, 0.70f * sfxVolume);
        }

        public void PlayVictory()
        {
            if (musicSource != null && _clipVictory != null)
            {
                musicSource.Stop();
                musicSource.clip = _clipVictory;
                musicSource.volume = musicVolume * masterVolume;
                musicSource.Play();
            }
        }

        public void PlayDefeat()
        {
            if (musicSource != null && _clipDefeat != null)
            {
                musicSource.Stop();
                musicSource.clip = _clipDefeat;
                musicSource.volume = musicVolume * masterVolume;
                musicSource.Play();
            }
        }

        public void PlayLoopRewind()
        {
            PlaySoundOneShot(_clipLoopRewind, 0.85f * sfxVolume);
        }

        public void SetTickingEnabled(bool enabled)
        {
            _isTickingActive = enabled;
        }

        public void OnLoopReset()
        {
            PlayLoopRewind();
            // Stagger bell chime slightly for dramatic loop turnover
            Invoke(nameof(PlayClockChime), 0.35f);
        }
    }
}
