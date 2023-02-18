using Godot;
using System;
using System.Collections;
using System.Collections.Generic;


namespace HackingMiniGames.TerminalHack
{
    // TODO: This and BracketPairs may be better if they extend the same base class
    public class PuzzleWord : Reference
    {
        public String WordString{
            set; get;
        }

        public int Length{
            get { return CharacterPositions.Count; }
        }

        public List<TerminalCharacter> CharacterPositions{
            set; get;
        }

        public int Correctness{
            set; get;
        }

        public bool Used{ set; get; }
    }
}

