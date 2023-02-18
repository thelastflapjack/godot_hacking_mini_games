using Godot;
using System;
using System.Collections;
using System.Collections.Generic;


namespace HackingMiniGames.TerminalHack
{
    public class SelectionLine : HBoxContainer
    {
        public Audio TerminalAudio;

        private Label _textLabel;
        private Label _cursor;

        public override void _Ready()
        {
            _textLabel = GetNode<Label>("Text");
            _cursor = GetNode<Label>("Cursor");

            float blinkTime = 0.75f;
            SceneTreeTween cursorTween = CreateTween().SetLoops().BindNode(this);
            cursorTween.TweenProperty(_cursor, "modulate", new Color(1,1,1,0), blinkTime);
            cursorTween.TweenProperty(_cursor, "modulate", new Color(1,1,1,1), blinkTime);
            cursorTween.Play();
        }
        
        public void UpdateLine(String text)
        {
            _textLabel.Text = text.ToUpper();
            TerminalAudio.PlayTyping(0.15f * text.Length);
        }
    }
}

