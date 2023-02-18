using Godot;
using System;
using System.Collections;
using System.Collections.Generic;


namespace HackingMiniGames.TerminalHack
{
    public class TerminalCharacter : Label
    {
        public bool IsReserved = false;
        
        private StyleBoxFlat _styleBoxFocused = ResourceLoader.Load<StyleBoxFlat>("res://terminal_hack/theme/focused_style.stylebox");
        private StyleBoxFlat _styleBoxUnFocused = ResourceLoader.Load<StyleBoxFlat>("res://terminal_hack/theme/unfocused_style.stylebox");

        private Color _fontColorFocused;
        private Color _fontColorUnFocused;
        

        public override void _Ready()
        {
            _fontColorFocused = _styleBoxUnFocused.BgColor;
            _fontColorUnFocused = _styleBoxFocused.BgColor;

            Connect("focus_entered", this, nameof(OnFocusEntered));
            Connect("focus_exited", this, nameof(OnFocusExited));
        }
        
        public void SetCharacter(String character)
        {
            Text = character.ToUpper();
        }

        public void Highlight(bool highlightOn)
        {
            if (highlightOn)
            {
                AddStyleboxOverride("normal", _styleBoxFocused);
                AddColorOverride("font_color", _fontColorFocused);
            }
            else if (!HasFocus())
            {
                AddStyleboxOverride("normal", _styleBoxUnFocused);
                AddColorOverride("font_color", _fontColorUnFocused);
            }
        }

        private void OnFocusEntered()
        {
            Highlight(true);
        }

        
        private void OnFocusExited()
        {
            Highlight(false);
        }
    }
}

