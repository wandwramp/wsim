using System;
using System.IO;
using System.Media;
using System.Reflection;
using System.Timers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RexSimulatorGui
{
    /// <summary>
    /// Encapsulates SoundPlayer, only allowing the loaded resource
    /// to play once at a time. Since we only have one sound in this
    /// program, we just make it work for that sound and nothing else.
    /// </summary>
    public class Quacker
    {
        // duck_quack.wav lasts 178ms according to Audacity
        private Timer timer = new Timer(178);
        private SoundPlayer player;

        private volatile bool isPlaying = false;
        public bool IsPlaying
        {
            get
            {
                return isPlaying;
            }
        }

        public Quacker(Stream resource)
        {
            player = new SoundPlayer(resource);
            player.Load();
            timer.Elapsed += StoppedPlaying;
        }

        private void StoppedPlaying(Object source, ElapsedEventArgs e)
        {
            isPlaying = false;
        }

        public void Quack()
        {
            if (!IsPlaying)
            {
                isPlaying = true;
                timer.Start();
                player.Play();
            }
        }
    }
}
