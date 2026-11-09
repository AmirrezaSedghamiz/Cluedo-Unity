using UnityEngine;
using TomorrowsCrime.Core;

namespace TomorrowsCrime.PhysicsEngine
{
    public enum PhysicsMaterialType
    {
        Metal,
        Wood,
        Stone,
        Glass,
        Paper
    }

    /// <summary>
    /// Interactive physical prop component for Tomorrow's Crime.
    /// Manages Rigidbody physics, collision impact audio, and physical grab/throw states.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsProp : MonoBehaviour, IInteractable
    {
        [Header("Prop Identity")]
        [SerializeField] private string propDisplayName = "Object";
        [SerializeField] private PhysicsMaterialType materialType = PhysicsMaterialType.Metal;

        [Header("Physics Tuning")]
        [SerializeField] private float throwForceMultiplier = 1.0f;
        [SerializeField] private float minImpactSoundSpeed = 0.8f;

        private Rigidbody _rigidbody;
        private AudioSource _audioSource;
        private bool _isHeld = false;

        public Rigidbody Rigidbody => _rigidbody;
        public string DisplayName => propDisplayName;
        public string PropDisplayName
        {
            get => propDisplayName;
            set => propDisplayName = value;
        }
        public PhysicsMaterialType MaterialType
        {
            get => materialType;
            set => materialType = value;
        }
        public bool IsHeld => _isHeld;
        public float ThrowForceMultiplier => throwForceMultiplier;

        // IInteractable implementation
        public string InteractionPrompt => _isHeld ? "Drop [E] | Throw [Left Click]" : $"Pick Up {propDisplayName} [E]";
        public bool CanInteract => true;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1.0f;
                _audioSource.minDistance = 1.0f;
                _audioSource.maxDistance = 15.0f;
                _audioSource.playOnAwake = false;
            }

            // Default physical mass and damping
            if (_rigidbody.mass <= 0.001f) _rigidbody.mass = 1.5f;
            _rigidbody.linearDamping = 0.15f;
            _rigidbody.angularDamping = 0.5f;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        public void SetHeld(bool held)
        {
            _isHeld = held;
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = !held;
                _rigidbody.linearDamping = held ? 6.0f : 0.15f;
                _rigidbody.angularDamping = held ? 6.0f : 0.5f;
            }
        }

        public void Interact(GameObject interactor)
        {
            if (interactor == null) return;

            var grabber = interactor.GetComponent<PlayerPhysicsGrabber>();
            if (grabber != null)
            {
                if (_isHeld)
                {
                    grabber.ReleaseHeldProp();
                }
                else
                {
                    grabber.GrabProp(this);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Activate dynamic physics if impacted by moving character or projectile
            if (_rigidbody != null && _rigidbody.isKinematic && collision.relativeVelocity.sqrMagnitude > 1.0f)
            {
                _rigidbody.isKinematic = false;
            }

            if (_isHeld) return;

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed >= minImpactSoundSpeed)
            {
                PlayImpactAudio(impactSpeed);
            }
        }

        private void PlayImpactAudio(float speed)
        {
            if (_audioSource == null) return;

            // Generate procedural physical impact click/clang
            float volume = Mathf.Clamp01(speed / 6.0f) * 0.75f;
            float pitch = Mathf.Clamp(1.0f + (speed * 0.05f) - (_rigidbody.mass * 0.1f), 0.6f, 1.8f);

            _audioSource.pitch = pitch;
            _audioSource.volume = volume;

            // Procedural audio clip synthesis for physical impact
            int sampleRate = 44100;
            int length = (int)(sampleRate * 0.12f);
            var clip = AudioClip.Create("Impact_Sound", length, 1, sampleRate, false);
            float[] samples = new float[length];

            float freq = materialType == PhysicsMaterialType.Metal ? 820f :
                         materialType == PhysicsMaterialType.Wood ? 240f :
                         materialType == PhysicsMaterialType.Glass ? 1200f : 400f;

            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * (materialType == PhysicsMaterialType.Metal ? 35f : 60f));
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * decay;
                samples[i] = wave * (materialType == PhysicsMaterialType.Wood ? (wave + Random.Range(-0.1f, 0.1f)) : wave);
            }

            clip.SetData(samples, 0);
            _audioSource.PlayOneShot(clip);
        }
    }
}
