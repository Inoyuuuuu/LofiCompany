using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LofiCompany.Utils
{
    internal class AudioSourceAndInitVolume
    {
        public AudioSource audioSource { get; set; }
        public float initVolume { get; private set; }

        public AudioSourceAndInitVolume(AudioSource audioSource, float initVolume)
        {
            this.audioSource = audioSource;
            this.initVolume = initVolume;
        }

    }
}
