using Godot;
using System;
using System.Collections;
using System.Collections.Generic;


namespace HackingMiniGames.TerminalHack
{
    public class BracketPair : Reference
    {
        
        static public List<String> StringPairs = new List<String>{
            "[]", "{}", "<>", "()"
        };

        public bool Used = false;
        
        public int Length{
            get { return CharacterPositions.Count; }
        }

        public List<TerminalCharacter> CharacterPositions{
            set; get;
        }
    }
}

