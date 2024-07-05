using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance;
        public AudioClip[] music;
        public AudioClip SonicSong;
        public bool playingSonic = false;
        AudioSource audioSource;
        private void Start()
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
            PlayRandomSong();
        }

        public void PlaySonicSong()
        {
            playingSonic = true;
            audioSource.clip = SonicSong;
            audioSource.Play();
            audioSource.time = 8;
        }

        public void StopSonic()
        {
            if (playingSonic)
            {
                playingSonic = false;
                audioSource.Stop();
            }
        }

        public void PlayRandomSong()
        {
            int index = UnityEngine.Random.Range(0, music.Length - 1);
            audioSource.clip = music[index];
            audioSource.Play();
        }

        public void FixedUpdate()
        {
            if (!audioSource.isPlaying)
            {
                PlayRandomSong();
            }
        }
    }
}
