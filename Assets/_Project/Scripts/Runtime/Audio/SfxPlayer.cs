using System.Collections.Generic;
using UnityEngine;

namespace PhoneRepair.Audio
{
    /// <summary>Game sound effects by meaning, not by file.</summary>
    public enum SfxId
    {
        Tick = 0,     // screwdriver ratchet, every quarter turn
        Pop = 1,      // something comes free
        Thunk = 2,    // a part lands on the mat
        Click = 3,    // snaps/plugs/tightens home
        Unplug = 4,   // connector flips up
        Nope = 5,     // not allowed yet
        Whoosh = 6,   // part flying
        Select = 7,   // light UI/grab tick
    }

    /// <summary>
    /// Plays 2D one-shots from a small voice pool. Clips can be assigned per id; anything left
    /// empty falls back to a procedurally generated placeholder so the game always has feedback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SfxPlayer : MonoBehaviour
    {
        [System.Serializable]
        public struct ClipOverride
        {
            public SfxId id;
            public AudioClip clip;
        }

        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.6f;
        [SerializeField, Range(1, 16)] private int voices = 6;
        [Tooltip("Real sound files replace the generated placeholders here.")]
        [SerializeField] private List<ClipOverride> overrides = new();

        private AudioSource[] sources;
        private Dictionary<SfxId, AudioClip> clips;
        private int nextVoice;

        private void Awake()
        {
            sources = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                sources[i] = source;
            }

            clips = ProceduralSfx.CreateAll();
            foreach (ClipOverride o in overrides)
            {
                if (o.clip != null)
                {
                    clips[o.id] = o.clip;
                }
            }
        }

        public void Play(SfxId id, float pitch = 1f, float volume = 1f)
        {
            if (sources == null || clips == null || !clips.TryGetValue(id, out AudioClip clip) || clip == null)
            {
                return;
            }

            AudioSource source = sources[nextVoice];
            nextVoice = (nextVoice + 1) % sources.Length;
            source.pitch = Mathf.Clamp(pitch, 0.25f, 3f);
            source.PlayOneShot(clip, Mathf.Clamp01(volume) * masterVolume);
        }
    }
}
