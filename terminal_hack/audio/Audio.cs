using Godot;
using System;


namespace HackingMiniGames.TerminalHack
{
    public class Audio : Spatial
    {
        private AudioStreamPlayer _audioBackground;
        private AudioStreamPlayer _audioEnterKey;
        private AudioStreamPlayer _audioNavKey;
        private AudioStreamPlayer _audioTyping;
        private AudioStreamPlayer _audioBleepGood;
        private AudioStreamPlayer _audioBleepBad;
        private Timer _typingTimer;


        public override void _Ready()
        {
            _audioBackground = GetNode<AudioStreamPlayer>("StreamPlayerBackground");
            _audioEnterKey = GetNode<AudioStreamPlayer>("StreamPlayerEnterKey");
            _audioNavKey = GetNode<AudioStreamPlayer>("StreamPlayerNavKey");
            _audioTyping = GetNode<AudioStreamPlayer>("StreamPlayerTyping");
            _audioBleepGood = GetNode<AudioStreamPlayer>("StreamPlayerBleepGood");
            _audioBleepBad = GetNode<AudioStreamPlayer>("StreamPlayerBleepBad");
            _typingTimer = GetNode<Timer>("TypingTimer");
        }

        public void StartBackgroundLoop()
        {
            _audioBackground.Play();
        }

        public void PlayEnterKey()
        {
            _audioEnterKey.Play();
        }

        public void PlayNavKey()
        {
            _audioNavKey.Play();
        }

        public async void PlayTyping(float duration)
        {
            float loopLength = _audioTyping.Stream.GetLength();
            Random rand = new Random();
            float randStartTime = loopLength * (float)rand.NextDouble();

            _audioTyping.Play(randStartTime);
            _typingTimer.Start(duration);
            await ToSignal(_typingTimer, "timeout");
            _audioTyping.Stop();
        }

        public void PlayBleepGood()
        {
            _audioBleepGood.Play();
        }

        public void PlayBleepBad()
        {
            _audioBleepBad.Play();
        }
    }
}

