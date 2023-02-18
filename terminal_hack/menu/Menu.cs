using Godot;
using System;
using System.Collections;
using System.Collections.Generic;


namespace HackingMiniGames.TerminalHack
{
    public class Menu : TerminalPage
    {
        
        public override void _Ready()
        {
            base._Ready();
        }


        private void HackButtonPressed(Game.Difficulty difficulty)
        {
            _terminalAudio.PlayEnterKey();
            EmitSignal(nameof(StartHackRequest), difficulty);
        }


        private void OnBtnVeryEasyPressed()
        {
            HackButtonPressed(Game.Difficulty.VeryEasy);
        }

        private void OnBtnEasyPressed()
        {
            HackButtonPressed(Game.Difficulty.Easy);
        }

        private void OnBtnAveragePressed()
        {
            HackButtonPressed(Game.Difficulty.Average);
        }

        private void OnBtnHardPressed()
        {
            HackButtonPressed(Game.Difficulty.Hard);
        }

        private void OnBtnVeryHardPressed()
        {
            HackButtonPressed(Game.Difficulty.VeryHard);
        }

        private void OnBtnQuitPressed()
        {
            GetTree().Quit();
        }

    }
}

