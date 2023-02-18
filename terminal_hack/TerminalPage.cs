using Godot;
using System;
using System.Collections;
using System.Collections.Generic;


namespace HackingMiniGames.TerminalHack
{
    public class TerminalPage : Control
    {
        [Signal]
        public delegate void SwitchPageRequest(String pageName);
        [Signal]
        public delegate void StartHackRequest(Game.Difficulty difficulty);

        [Export]
        private NodePath _initialFocusTarget = null;

        protected Audio _terminalAudio;
        private Color _characterColor;
        private Color _fadeColor;
        private SceneTreeTween _fadeTween;

        protected bool _isActive = false;

        public override void _Ready()
        {
            base._Ready();
            _characterColor = Modulate;
            _fadeColor = new Color(_characterColor.r, _characterColor.g, _characterColor.b, 0);
            Modulate = _fadeColor;
        }

        public void Activate(bool activate)
        {
            if (activate)
            {
                GetNode<Control>(_initialFocusTarget).GrabFocus();
            }
            Fade(activate);
            _isActive = activate;
        }

        virtual public void SetTerminalAudio(Audio audio)
        {
            _terminalAudio = audio;
        }

        private void Fade(bool fadeIn)
        {
            if (_fadeTween != null && _fadeTween.IsRunning())
            {
                _fadeTween.Kill();
            }
            _fadeTween = CreateTween();
            float fadeTimeIn = 0.5f;
            float fadeTimeOut = 3.0f;
            _fadeTween.SetTrans(Tween.TransitionType.Expo);
            if (fadeIn)
            {
                _fadeTween.TweenProperty(this, "modulate", _characterColor, fadeTimeIn).From(_fadeColor).SetEase(Tween.EaseType.In);
            }
            else
            {
                _fadeTween.TweenProperty(this, "modulate", _fadeColor, fadeTimeOut).From(_characterColor).SetEase(Tween.EaseType.Out);
            }
            _fadeTween.Play();
        }
    }
}

