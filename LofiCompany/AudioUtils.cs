﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace LofiCompany
{
    internal class AudioUtils
    {
        private const float fadeAmount = 0.07f;
        private const float delayBetweenSteps = 0.2f;

        public static IEnumerator FadeOutMusicSource(AudioSource audioSource)
        {
            LofiCompany.Logger.LogDebug("fading out: " + audioSource.name);

            float initialVolume = audioSource.volume;
            while (audioSource.volume > 0.1f)
            {
                audioSource.volume -= fadeAmount;
                yield return new WaitForSeconds(delayBetweenSteps);
            }
            audioSource.Stop();
            audioSource.volume = initialVolume;
            yield break;
        }

        public static IEnumerator FadeInMusicSource(AudioSource audioSource, float initialVolume)
        {
            LofiCompany.Logger.LogDebug("fading in: " + audioSource.name);
            audioSource.volume = 0f;

            while (audioSource.volume < initialVolume)
            {
                audioSource.volume += fadeAmount;
                yield return new WaitForSeconds(delayBetweenSteps);
            }
            audioSource.volume = initialVolume;
            yield break;
        }
    }
}
