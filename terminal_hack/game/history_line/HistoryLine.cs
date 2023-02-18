using Godot;
using System;
using System.Collections;
using System.Collections.Generic;


namespace HackingMiniGames.TerminalHack
{
    public class HistoryLine : HBoxContainer
    {
        private Label textLabel;

        public override void _Ready()
        {
            textLabel = GetNode<Label>("Text");
        }
        
        public String Text
        {
            set { textLabel.Text = value.ToUpper(); }
            get { return textLabel.Text; }
        }
    }
}

