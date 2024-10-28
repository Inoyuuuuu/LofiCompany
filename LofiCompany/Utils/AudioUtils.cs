using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace LofiCompany.Utils
{
    internal class AudioUtils
    {
        internal static bool isFadingIn = false;
        internal static bool isFadingOut = false;

        private const float fadeAmount = 0.07f;
        private const float delayBetweenSteps = 0.2f;

        public static IEnumerator FadeOutMusicSource(AudioSource audioSource)
        {
            float initialVolume = audioSource.volume;
            while (audioSource.volume > 0.1f)
            {
                isFadingOut = true;
                audioSource.volume -= fadeAmount;
                yield return new WaitForSeconds(delayBetweenSteps);
            }
            audioSource.Stop();
            audioSource.volume = initialVolume;
            isFadingOut = false;
            yield break;
        }

        public static IEnumerator FadeInMusicSource(AudioSource audioSource, float initialVolume)
        {
            audioSource.volume = 0f;

            while (audioSource.volume < initialVolume)
            {
                isFadingIn = true;
                audioSource.volume += fadeAmount;
                yield return new WaitForSeconds(delayBetweenSteps);
            }
            audioSource.volume = initialVolume;
            isFadingIn = false;
            yield break;
        }
    }
}
