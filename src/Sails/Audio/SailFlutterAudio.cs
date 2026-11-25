// Reuses native spatial flap audio, driven explicitly by procedural sail flutter.
using System;
using UnityEngine;

namespace MoreSailwindSails.Sails.Audio
{
    internal sealed class SailFlutterAudio : MonoBehaviour
    {
        [SerializeField]
        private Sail sail;

        [SerializeField]
        private AudioSource source;

        private float pitchCooldown;

        internal static SailFlutterAudio Configure(Sail sail)
        {
            var native = sail.GetComponentInChildren<SailFlapAudio>(includeInactive: true);
            if (!native || !native.flapAudio)
                throw new InvalidOperationException("Expected the donor sail's flap audio source.");
            // Staysails retain Awake's separate snap sound; spritsails remove the
            // native component when stripping their donor. Neither runs native Update.
            native.enabled = false;
            var audio = sail.gameObject.AddComponent<SailFlutterAudio>();
            audio.sail = sail;
            audio.source = native.flapAudio;
            audio.source.playOnAwake = false;
            audio.Silence();
            return audio;
        }

        private void Awake() => Silence();

        private void OnDisable() => Silence();

        internal void Silence()
        {
            if (!source)
                return;
            source.volume = 0;
            source.Stop();
            source.enabled = false;
        }

        internal void SetFlutter(float strength, float deployment, bool supported)
        {
            if (
                !isActiveAndEnabled
                || !supported
                || !sail
                || !source
                || !GameState.playing
                || GameState.currentShipyard
                || GameState.currentlyLoading
                || GameState.shipyardUpdatingOrder
                || (sail.damage && sail.damage.sunk)
                || Time.deltaTime <= 0
            )
            {
                Silence();
                return;
            }
            float volume = SailFlutterAudioResponse.Volume(
                strength: strength,
                deployment: deployment
            );
            if (volume <= 0.01f)
            {
                Silence();
                return;
            }
            source.transform.position = sail.windcenter.position;
            if (pitchCooldown <= 0)
            {
                pitchCooldown = UnityEngine.Random.Range(min: 1f, max: 4f);
                source.pitch = UnityEngine.Random.Range(min: 0.6f, max: 1.2f);
            }
            pitchCooldown -= Time.deltaTime;
            source.volume = volume;
            source.enabled = true;
            source.mute = false;
            if (!source.isPlaying)
                source.Play();
        }
    }
}
