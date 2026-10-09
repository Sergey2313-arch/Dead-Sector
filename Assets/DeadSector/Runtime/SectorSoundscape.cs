using UnityEngine;

namespace DeadSector
{
    // Small placeholder SFX created locally with no external audio assets.
    // Imported player-provided audio can replace these clips in a later pass.
    public sealed class SectorSoundscape : MonoBehaviour
    {
        SectorPlayer player;
        SectorWeather weather;
        AudioSource effects;
        AudioSource ambience;
        AudioClip footstep;
        AudioClip construction;
        AudioClip chop;
        AudioClip hit;
        AudioClip gunshot;
        AudioClip rain;
        float nextFootstep;

        public void Configure(SectorPlayer target)
        {
            player = target;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 0f;
            effects.volume = .30f;
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.playOnAwake = false;
            ambience.spatialBlend = 0f;
            ambience.loop = true;
            ambience.volume = 0f;
            footstep = CreateClip("DeadSector_Step", .11f, 110f, .22f, false);
            construction = CreateClip("DeadSector_Hammer", .24f, 180f, .44f, false);
            chop = CreateClip("DeadSector_Axe", .19f, 96f, .40f, false);
            hit = CreateClip("DeadSector_Impact", .15f, 135f, .42f, false);
            gunshot = CreateClip("DeadSector_Gunshot", .30f, 69f, .68f, false);
            rain = CreateClip("DeadSector_Rain", 1.0f, 0f, .095f, true);
            ambience.clip = rain;
        }

        public void PlayBuild() => Play(construction);
        public void PlayChop() => Play(chop);
        public void PlayHit() => Play(hit);
        public void PlayGunshot() => Play(gunshot);

        void Play(AudioClip clip)
        {
            if (effects != null && clip != null)
                effects.PlayOneShot(clip);
        }

        void Update()
        {
            if (player == null || !player.Ready)
                return;
            if (weather == null)
                weather = FindFirstObjectByType<SectorWeather>();

            bool raining = weather != null &&
                weather.Weather == SectorWeatherKind.Rain;
            if (ambience != null)
            {
                if (raining && !ambience.isPlaying)
                    ambience.Play();
                ambience.volume = Mathf.MoveTowards(
                    ambience.volume, raining ? .16f : 0f,
                    Time.deltaTime * .1f);
                if (!raining && ambience.volume < .001f && ambience.isPlaying)
                    ambience.Stop();
            }

            if (!player.InputBlockedByUI && player.IsGrounded &&
                player.Speed > 1f && Time.time >= nextFootstep)
            {
                nextFootstep = Time.time +
                    (player.Speed > 5.5f ? .31f : .46f);
                if (effects != null && footstep != null)
                    effects.PlayOneShot(footstep,
                        player.IsCrouching ? .22f : .85f);
            }
        }

        static AudioClip CreateClip(
            string name, float seconds, float frequency,
            float amplitude, bool noiseOnly)
        {
            const int sampleRate = 22050;
            int samples = Mathf.CeilToInt(sampleRate * seconds);
            AudioClip clip = AudioClip.Create(
                name, samples, 1, sampleRate, false);
            float[] data = new float[samples];
            uint rng = 1299709u;
            for (int i = 0; i < samples; i++)
            {
                rng = unchecked(rng * 1664525u + 1013904223u);
                float white = ((rng >> 8) / 16777215f) * 2f - 1f;
                float t = i / (float)sampleRate;
                float envelope = noiseOnly
                    ? 1f : Mathf.Pow(1f - i / (float)samples, 2f);
                float tonal = Mathf.Sin(2f * Mathf.PI *
                    (frequency * t * (1f - .22f * t)));
                data[i] = Mathf.Clamp(
                    ((noiseOnly ? white : tonal * .55f + white * .45f)
                        * envelope * amplitude), -.95f, .95f);
            }
            clip.SetData(data, 0);
            return clip;
        }

        void OnDestroy()
        {
            if (footstep != null) Destroy(footstep);
            if (construction != null) Destroy(construction);
            if (chop != null) Destroy(chop);
            if (hit != null) Destroy(hit);
            if (gunshot != null) Destroy(gunshot);
            if (rain != null) Destroy(rain);
        }
    }
}
