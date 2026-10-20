using System;
using System.Collections.Generic;
using UnityEngine;

namespace TokyoExpress.Audio
{
    public enum SoundEffect
    {
        Tap,
        Swipe,
        Jump,
        Land,
        Crash,
        DeliverSuccess,
        DeliverFail,
        NitroBoost,
        BulletTime,
        CoinCollect,
        ButtonClick
    }

    /// <summary>
    /// Complete Audio Manager for Tokyo Express.
    /// Includes procedural audio synthesis fallback (AudioClip generator using math sine/noise)
    /// so the game is 100% playable with clear sound effects immediately without requiring external .wav assets!
    /// </summary>
    public class TokyoAudioManager : MonoBehaviour
    {
        public static TokyoAudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;

        [Header("Volume Settings")]
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float bgmVolume = 0.5f;

        private Dictionary<SoundEffect, AudioClip> proceduralClipCache = new Dictionary<SoundEffect, AudioClip>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                InitializeAudioSources();
                GenerateProceduralSounds();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudioSources()
        {
            if (sfxSource == null)
            {
                GameObject sfxGO = new GameObject("SFX_Source");
                sfxGO.transform.SetParent(transform);
                sfxSource = sfxGO.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (bgmSource == null)
            {
                GameObject bgmGO = new GameObject("BGM_Source");
                bgmGO.transform.SetParent(transform);
                bgmSource = bgmGO.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }
        }

        public void PlaySFX(SoundEffect effect, float pitch = 1f)
        {
            if (sfxSource == null) return;
            if (proceduralClipCache.TryGetValue(effect, out AudioClip clip))
            {
                sfxSource.pitch = pitch;
                sfxSource.PlayOneShot(clip, sfxVolume);
            }
        }

        #region Procedural Audio Synthesizer (Instant Plug & Play)
        private void GenerateProceduralSounds()
        {
            proceduralClipCache[SoundEffect.Tap] = CreateToneClip("Tap", 600f, 0.05f, 0.3f);
            proceduralClipCache[SoundEffect.Swipe] = CreateSweepClip("Swipe", 300f, 700f, 0.1f);
            proceduralClipCache[SoundEffect.Jump] = CreateSweepClip("Jump", 220f, 650f, 0.22f);
            proceduralClipCache[SoundEffect.Land] = CreateToneClip("Land", 120f, 0.1f, 0.4f);
            proceduralClipCache[SoundEffect.Crash] = CreateNoiseClip("Crash", 0.4f);
            proceduralClipCache[SoundEffect.DeliverSuccess] = CreateArpeggioClip("DeliverSuccess", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.4f);
            proceduralClipCache[SoundEffect.DeliverFail] = CreateSweepClip("DeliverFail", 350f, 120f, 0.45f);
            proceduralClipCache[SoundEffect.NitroBoost] = CreateSweepClip("NitroBoost", 200f, 900f, 0.35f);
            proceduralClipCache[SoundEffect.BulletTime] = CreateToneClip("BulletTime", 180f, 0.3f, 0.5f);
            proceduralClipCache[SoundEffect.CoinCollect] = CreateArpeggioClip("CoinCollect", new float[] { 987.77f, 1318.51f }, 0.15f);
            proceduralClipCache[SoundEffect.ButtonClick] = CreateToneClip("ButtonClick", 880f, 0.04f, 0.2f);
        }

        private AudioClip CreateToneClip(string name, float freq, float duration, float decay)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * (1f / decay));
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateSweepClip(string name, float startFreq, float endFreq, float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
                float phase = 2 * Mathf.PI * currentFreq * ((float)i / sampleRate);
                float env = 1f - t;
                data[i] = Mathf.Sin(phase) * env;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateNoiseClip(string name, float duration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            System.Random rand = new System.Random();
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Exp(-t * 6f);
                data[i] = ((float)rand.NextDouble() * 2f - 1f) * env;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip CreateArpeggioClip(string name, float[] notes, float totalDuration)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * totalDuration);
            float[] data = new float[samples];
            int noteCount = notes.Length;
            int samplesPerNote = samples / noteCount;

            for (int i = 0; i < samples; i++)
            {
                int noteIndex = Mathf.Clamp(i / samplesPerNote, 0, noteCount - 1);
                float freq = notes[noteIndex];
                float noteT = (float)(i % samplesPerNote) / sampleRate;
                float env = Mathf.Exp(-noteT * 8f);
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * noteT) * env;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
        #endregion
    }
}
