using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace HackingMiniGames.TerminalHack
{
    public class Game : TerminalPage
    {
        /// Signals ///

        /// Enums ///
        public enum Difficulty
        {
            VeryEasy, Easy, Average, Hard, VeryHard
        }

        /// Constants ///
        private const String _allowedSymbolsString = "!$&+=;:\\/?#^*-~|";

        /// Fields - protected or private ///
        private int _puzzleGridWidth;
        private int _puzzleGridHeight;
        private int _puzzleGridCharCount;
        private Dictionary<Difficulty, String> _wordSetFilePaths = new Dictionary<Difficulty, String>()
        {
            { Difficulty.VeryEasy, "res://terminal_hack/word_sets/len_4.txt"},
            { Difficulty.Easy, "res://terminal_hack/word_sets/len_6.txt"},
            { Difficulty.Average, "res://terminal_hack/word_sets/len_8.txt"},
            { Difficulty.Hard, "res://terminal_hack/word_sets/len_10.txt"},
            { Difficulty.VeryHard, "res://terminal_hack/word_sets/len_12.txt"}
        };

        private char[] _allowedSymbolsArray;
        private GridContainer _gridLeft;
        private GridContainer _gridRight;

        private List<TerminalCharacter> _terminalCharacters = new List<TerminalCharacter>();
        private List<PuzzleWord> _puzzleWords = new List<PuzzleWord>();
        private List<BracketPair> _bracketPairs = new List<BracketPair>();
        private String _solutionString;
        private Dictionary<TerminalCharacter, PuzzleWord> _terminalCharWordOwners = new Dictionary<TerminalCharacter, PuzzleWord>();
        private Dictionary<TerminalCharacter, BracketPair> _terminalCharBracketOwners = new Dictionary<TerminalCharacter, BracketPair>();
        private TerminalCharacter _focusedTerminalCharacter = new TerminalCharacter();
        private List<HistoryLine> _historyLines = new List<HistoryLine>();
        private SelectionLine _selectionLine;
        private int _attemptsCounter = 0;
        private int _appemptsMax = 4;
        private Label _attemptsLabel;
        private Difficulty _currentDifficulty;


        //////////////////////////////
        // Engine Callback Methods  //
        //////////////////////////////
        public override void _Ready()
        {
            base._Ready();
            GD.Randomize();

            CacheNodeReferences();

            _puzzleGridWidth = _gridLeft.Columns;
            _puzzleGridHeight = _gridLeft.GetChildCount() / _puzzleGridWidth;
            _puzzleGridCharCount = _gridLeft.GetChildCount();

            _allowedSymbolsArray = _allowedSymbolsString.ToCharArray();
            
            BuildTerminalCharactersList();
        }
        
        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_accept") && _isActive)
            {
                _terminalAudio.PlayEnterKey();
                ProcessUserSelection();
            }
            // else if (@event.IsActionPressed("ui_cancel") && _isActive)
            // {
            //     GD.Print("Debug Restart");
            //     StartNewGame(_currentDifficulty);
            // }
        }
        
        //////////////////////////////
        //      Public Methods      //
        //////////////////////////////
        public override void SetTerminalAudio(Audio audio)
        {
            base.SetTerminalAudio(audio);
            _selectionLine.TerminalAudio = audio;
        }

        public void StartNewGame(Difficulty difficulty)
        {
            _currentDifficulty = difficulty;
            _isActive = true;
            _attemptsCounter = 0;
            _puzzleWords.Clear();
            _bracketPairs.Clear();
            _terminalCharWordOwners.Clear();
            _terminalCharBracketOwners.Clear();
            _solutionString = "";
            foreach(HistoryLine line in _historyLines)
            {
                line.Text = "";
            }
            foreach(TerminalCharacter termChar in _terminalCharacters)
            {
                termChar.IsReserved = false;
            }

            HighlightTerminalCharacters(_terminalCharacters, false);
            IncrementAttemptCounter(-_appemptsMax);

            GetViewport().Connect("gui_focus_changed", this, nameof(OnGUIFocusChanged));

            InitFocusNeighbours(_gridLeft, _gridRight);
            InitFocusNeighbours(_gridRight, _gridLeft);

            SetHexNumberColumns();

            RandomizeTerminalCharacters();

            List<String> puzzleStrings = GeneratePuzzleWordStrings(_currentDifficulty);
            GeneratePuzzleWordPositions(puzzleStrings);
            PlacePuzzleWordsOnGrid();
            UpdateWordFocusNeighbours();

            GenerateBracketPairs(4);
            PlaceBracketPairsOnGrid();
            UpdateBracketFocusNeighbours();

            GD.Print(_solutionString);
        }


        //////////////////////////////
        // Signal Connected Methods //
        //////////////////////////////
        private void OnGUIFocusChanged(Control newFocusTarget)
        {
            if (newFocusTarget is TerminalCharacter)
            {
                // Unhighlight last
                List<TerminalCharacter> lastHighlightedChars = new List<TerminalCharacter>();
                if (_terminalCharWordOwners.ContainsKey(_focusedTerminalCharacter)) // Check if PuzzleWord
                {
                    lastHighlightedChars = _terminalCharWordOwners[_focusedTerminalCharacter].CharacterPositions;
                }
                else if (_terminalCharBracketOwners.ContainsKey(_focusedTerminalCharacter)) // Check if BracketPair
                {
                    lastHighlightedChars = _terminalCharBracketOwners[_focusedTerminalCharacter].CharacterPositions;
                }
                HighlightTerminalCharacters(lastHighlightedChars, false);

                _focusedTerminalCharacter = (TerminalCharacter)newFocusTarget;
                if (_terminalCharWordOwners.ContainsKey(_focusedTerminalCharacter))
                {
                    PuzzleWord focusedWord = _terminalCharWordOwners[_focusedTerminalCharacter];
                    HighlightTerminalCharacters(focusedWord.CharacterPositions, true);
                    if (!focusedWord.Used)
                    {
                        _selectionLine.UpdateLine(focusedWord.WordString);
                    }
                    else
                    {
                        _selectionLine.UpdateLine(_focusedTerminalCharacter.Text);
                    }
                }
                else if (_terminalCharBracketOwners.ContainsKey(_focusedTerminalCharacter))
                {
                    BracketPair focusedBracketPair = _terminalCharBracketOwners[_focusedTerminalCharacter];
                    // Only highlight if the opening bracket is selected
                    if (!focusedBracketPair.Used && focusedBracketPair.CharacterPositions[0] == _focusedTerminalCharacter)
                    {
                        HighlightTerminalCharacters(focusedBracketPair.CharacterPositions, true);
                    }
                }
                else
                {
                    _selectionLine.UpdateLine(_focusedTerminalCharacter.Text);
                }
            }
        }
        

        //////////////////////////////
        //      Private Methods     //
        //////////////////////////////

        private void CacheNodeReferences()
        {
            _gridLeft = GetNode<GridContainer>("HBoxContainer2/ProblemSpace/ColumnLeft");
            _gridRight = GetNode<GridContainer>("HBoxContainer2/ProblemSpace/ColumnRight");
            _attemptsLabel = GetNode<Label>("HBoxContainer2/VBoxContainer/InputPrompt/AttemptCounter");

            foreach(Control line in GetNode<VBoxContainer>("HBoxContainer2/VBoxContainer/InputLines").GetChildren())
            {
                if (line is HistoryLine)
                {
                    _historyLines.Add(line as HistoryLine);
                }
                else if (line is SelectionLine)
                {
                    _selectionLine = line as SelectionLine;
                }
            }
        }

        private void InitFocusNeighbours(GridContainer targetGrid, GridContainer otherGrid)
        {
            for (int charColumnNum = 0; charColumnNum < _puzzleGridHeight; charColumnNum++)
            {
                int rowStartNum = ((charColumnNum) * _puzzleGridWidth) + 1;
                for (int charRowNum = 0; charRowNum < _puzzleGridWidth; charRowNum++)
                {
                    int charLabelNumber = rowStartNum + charRowNum; 
                    Label charLabel = targetGrid.GetNode<Label>($"TerminalCharacter{charLabelNumber}");

                    int neighbourIndexLeft = charLabelNumber - 1;
                    if (neighbourIndexLeft >= rowStartNum)
                    {
                        charLabel.FocusNeighbourLeft =  new NodePath($"{targetGrid.GetPath()}/TerminalCharacter{neighbourIndexLeft}");
                    }
                    else
                    {
                        // Wrap around to other grid
                        neighbourIndexLeft = rowStartNum + _puzzleGridWidth - 1;
                        charLabel.FocusNeighbourLeft =  new NodePath($"{otherGrid.GetPath()}/TerminalCharacter{neighbourIndexLeft}");
                    }

                    int neighbourIndexRight = charLabelNumber + 1;
                    if (neighbourIndexRight < rowStartNum + _puzzleGridWidth)
                    {
                        charLabel.FocusNeighbourRight =  new NodePath($"{targetGrid.GetPath()}/TerminalCharacter{neighbourIndexRight}");
                    }
                    else
                    {
                        // Wrap around to other grid
                        neighbourIndexRight = rowStartNum;
                        charLabel.FocusNeighbourRight =  new NodePath($"{otherGrid.GetPath()}/TerminalCharacter{neighbourIndexRight}");
                    }


                    int neighbourIndexBottom = charLabelNumber + _puzzleGridWidth;
                    if (neighbourIndexBottom <= _puzzleGridCharCount)
                    {
                        charLabel.FocusNeighbourBottom =  new NodePath($"{targetGrid.GetPath()}/TerminalCharacter{neighbourIndexBottom}");
                    }
                    else
                    {
                        // Wrap around to the top of this column
                        int indexJump = (_puzzleGridHeight * _puzzleGridWidth) - _puzzleGridWidth;
                        neighbourIndexBottom = charLabelNumber - indexJump;
                        charLabel.FocusNeighbourBottom =  new NodePath($"{targetGrid.GetPath()}/TerminalCharacter{neighbourIndexBottom}");
                    }


                    int neighbourIndexTop = charLabelNumber - _puzzleGridWidth;
                    if (neighbourIndexTop > 0)
                    {
                        charLabel.FocusNeighbourTop =  new NodePath($"{targetGrid.GetPath()}/TerminalCharacter{neighbourIndexTop}");
                    }
                    else
                    {
                        // Wrap around to the bottom of this column
                        int indexJump = (_puzzleGridHeight * _puzzleGridWidth) - _puzzleGridWidth;
                        neighbourIndexTop = charLabelNumber + indexJump;
                        charLabel.FocusNeighbourTop =  new NodePath($"{targetGrid.GetPath()}/TerminalCharacter{neighbourIndexTop}");
                    }
                }
            }
        }
        
        private int CalcCorrectness(String guessWord)
        {
            int score = 0;
            for(int i = 0; i < guessWord.Length; i++)
            {
                if (guessWord[i] == _solutionString[i])
                {
                    score++;
                }
            }
            return score;
        }

        private void SetHexNumberColumns()
        {
            uint num = 20000 + (GD.Randi() % 40000); // A range of numbers which are 4 characters long in hex

            foreach (Label label in GetNode<VBoxContainer>("HBoxContainer2/ProblemSpace/HexColumnLeft").GetChildren())
            {
                label.Text = $"0x{num.ToString("X4")}";
                num += (uint)_puzzleGridWidth;
            }
            foreach (Label label in GetNode<VBoxContainer>("HBoxContainer2/ProblemSpace/HexColumnRight").GetChildren())
            {
                label.Text = $"0x{num.ToString("X4")}";
                num += (uint)_puzzleGridWidth;
            }
        }

        private void BuildTerminalCharactersList()
        {
            foreach (TerminalCharacter terminalCharacter in _gridLeft.GetChildren())
            {
                _terminalCharacters.Add(terminalCharacter);
            }
            foreach (TerminalCharacter terminalCharacter in _gridRight.GetChildren())
            {
                _terminalCharacters.Add(terminalCharacter);
            }
        }

        private void RandomizeTerminalCharacters()
        {
            foreach (TerminalCharacter terminalCharacter in _terminalCharacters)
            {
                char randSymbol = _allowedSymbolsArray[GD.Randi() % _allowedSymbolsArray.Length];
                terminalCharacter.SetCharacter(randSymbol.ToString());
            }
        }

        private List<String> GeneratePuzzleWordStrings(Difficulty difficulty)
        {
            // Read all words from file
            List<String> wordSet = new List<String>();
            File file = new File();
            file.Open(_wordSetFilePaths[difficulty], File.ModeFlags.Read);
            while (!file.EofReached())
            {   
                String nextWord = file.GetLine();
                wordSet.Add(nextWord);
            }
            file.Close();

            // Generate puzzle word strings
            var rand = new Random();
            bool isValidPuzzleSetFound = false;
            List<String> puzzleWordStrings = new List<String>();
            while (!isValidPuzzleSetFound)
            {
                puzzleWordStrings.Clear();
                wordSet = wordSet.OrderBy(x => rand.Next()).ToList(); // shuffle list
                _solutionString = wordSet[0];
                puzzleWordStrings.Add(_solutionString);
                
                int bucketSize = 4;
                int targetWordCount = (bucketSize * 3) + 1; // Buckets plus solution word
                int wordLength = _solutionString.Length;
                int boundaryVeryBad = Mathf.FloorToInt((wordLength * 0.25f) * 1); // Score boundaries for buckets
                int boundaryBad = Mathf.FloorToInt((wordLength * 0.25f) * 2);
                int boundaryGood = Mathf.FloorToInt((wordLength * 0.25f) * 3);
                int wordsVeryGood = 0;
                int wordsGood = 0;
                int wordsBad = 0;
                int wordsVeryBad = 0;
                foreach (String candidateWord in wordSet)
                {
                    if (puzzleWordStrings.Count == targetWordCount)
                    {
                        break;
                    }
                    
                    int wordCorrectness = CalcCorrectness(candidateWord);
                    if (wordCorrectness <= boundaryVeryBad)
                    {
                        if (wordsVeryBad < bucketSize)
                        {
                            wordsVeryBad++;
                            puzzleWordStrings.Add(candidateWord);
                        }
                        continue;
                    }
                    else if (wordCorrectness <= boundaryBad)
                    {
                        if (wordsBad < bucketSize)
                        {
                            wordsBad++;
                            puzzleWordStrings.Add(candidateWord);
                        }
                        continue;
                    }
                    else if (wordCorrectness <= boundaryGood)
                    {
                        if (wordsGood < bucketSize)
                        {
                            wordsGood++;
                            puzzleWordStrings.Add(candidateWord);
                        }
                        continue;
                    }
                    else if (wordsVeryGood < bucketSize && wordCorrectness != wordLength)
                    {
                        wordsVeryGood++;
                        puzzleWordStrings.Add(candidateWord);
                    }
                    
                }
                
                isValidPuzzleSetFound = puzzleWordStrings.Count == targetWordCount;
            }

            return puzzleWordStrings;
        }

        // CONSIDER: More efficient solution.
        private void GeneratePuzzleWordPositions(List<String> puzzleWordStrings)
        {
            int wordLength = puzzleWordStrings[0].Length;
            var rand = new Random();
            foreach (String wordString in puzzleWordStrings)
            {
                List<TerminalCharacter> wordCharacterPositions = new List<TerminalCharacter>();
                bool isValidPositionFound = false;
                while (!isValidPositionFound)
                {
                    wordCharacterPositions.Clear();
                    TerminalCharacter randStartPosition = _terminalCharacters[rand.Next(_terminalCharacters.Count)];
                    if (randStartPosition.IsReserved)
                    {
                        continue;
                    }
                    wordCharacterPositions.Add(randStartPosition);

                    // Ensure a one character gap at the start of the word
                    int startPosIndex = int.Parse(randStartPosition.Name.LStrip("TerminalCharacter"));
                    if (startPosIndex > 1)
                    {
                        TerminalCharacter paddingCharacterStart = randStartPosition.GetNode<TerminalCharacter>($"../TerminalCharacter{startPosIndex - 1}");
                        if (paddingCharacterStart.IsReserved)
                        {
                            continue;
                        }
                    }

                    // Ensure a one character gap at the end of the word
                    if (startPosIndex + wordLength < _puzzleGridCharCount)
                    {
                        TerminalCharacter paddingCharacterEnd = randStartPosition.GetNode<TerminalCharacter>($"../TerminalCharacter{startPosIndex + wordLength + 1}");
                        if (paddingCharacterEnd.IsReserved)
                        {
                            continue;
                        }
                    }

                    // Ensure all of the character positions are available. First char was already checked.
                    for (int i = 1; i < wordLength; i++)
                    {
                        int targetPositionIndex = startPosIndex + i;
                        if (targetPositionIndex > _puzzleGridCharCount)
                        {
                            break; // target positionIndex is out of range
                        }
                        TerminalCharacter characterPosition = randStartPosition.GetNode<TerminalCharacter>($"../TerminalCharacter{targetPositionIndex}");
                        if (characterPosition.IsReserved)
                        {
                            break;
                        }
                        else
                        {
                            wordCharacterPositions.Add(characterPosition);
                        }
                    }

                    isValidPositionFound = wordCharacterPositions.Count == wordLength;
                }

                PuzzleWord newWord = new PuzzleWord();
                newWord.WordString = wordString;
                newWord.Correctness = CalcCorrectness(wordString);
                newWord.CharacterPositions = wordCharacterPositions;
                _puzzleWords.Add(newWord);
                //GD.Print($"{newWord.WordString}  |  {newWord.Correctness}");
                // Reserve positions
                foreach(TerminalCharacter position in newWord.CharacterPositions)
                {
                    _terminalCharWordOwners.Add(position, newWord);
                    position.IsReserved = true;
                }
            }
        }

        private void PlacePuzzleWordsOnGrid()
        {
            foreach(PuzzleWord puzzleWord in _puzzleWords)
            {
                for (int i = 0; i < puzzleWord.Length; i++)
                {
                    puzzleWord.CharacterPositions[i].SetCharacter(puzzleWord.WordString.Substr(i, 1));
                }
            }
        }

        private void HighlightTerminalCharacters(List<TerminalCharacter> characters, bool highlightOn)
        {
            foreach(TerminalCharacter termChar in characters)
            {
                termChar.Highlight(highlightOn);
            }
        }

        private void UpdateWordFocusNeighbours()
        {
            foreach(PuzzleWord word in _puzzleWords)
            {
                NodePath focusNeighbourLeft = word.CharacterPositions[0].FocusNeighbourLeft;
                NodePath focusNeighbourRight = word.CharacterPositions[word.Length - 1].FocusNeighbourRight;
                foreach (TerminalCharacter termChar in word.CharacterPositions)
                {
                    termChar.FocusNeighbourLeft = focusNeighbourLeft;
                    termChar.FocusNeighbourRight = focusNeighbourRight;
                }
            }
        }
    
        private void GenerateBracketPairs(int count)
        {
            int maxLength = 6;
            var rand = new Random();
            for (int i = 0; i < count; i++)
            {
                int length = rand.Next(2, maxLength + 1);
                List<TerminalCharacter> wordCharacterPositions = new List<TerminalCharacter>();
                bool isValidPositionFound = false;
                while (!isValidPositionFound)
                {
                    wordCharacterPositions.Clear();
                    TerminalCharacter randStartPosition = _terminalCharacters[rand.Next(_terminalCharacters.Count)];
                    if (randStartPosition.IsReserved)
                    {
                        continue;
                    }
                    wordCharacterPositions.Add(randStartPosition);

                    // Ensure all of the character positions are available. First char was already checked.
                    int startPosIndex = int.Parse(randStartPosition.Name.LStrip("TerminalCharacter"));
                    for (int j = 1; j < length; j++)
                    {
                        int targetPositionIndex = startPosIndex + j;
                        if (targetPositionIndex > _puzzleGridCharCount)
                        {
                            break; // target positionIndex is out of range
                        }
                        TerminalCharacter characterPosition = randStartPosition.GetNode<TerminalCharacter>($"../TerminalCharacter{targetPositionIndex}");
                        if (characterPosition.IsReserved)
                        {
                            break;
                        }
                        else
                        {
                            wordCharacterPositions.Add(characterPosition);
                        }
                    }

                    isValidPositionFound = wordCharacterPositions.Count == length;
                }

                BracketPair newPair = new BracketPair();
                newPair.CharacterPositions = wordCharacterPositions;
                _bracketPairs.Add(newPair);
                // Reserve positions
                foreach(TerminalCharacter position in newPair.CharacterPositions)
                {
                    _terminalCharBracketOwners.Add(position, newPair);
                    position.IsReserved = true;
                }
            }
        }

        private void PlaceBracketPairsOnGrid()
        {
            Random rand = new Random();
            foreach(BracketPair bracketPair in _bracketPairs)
            {
                String bracketString = BracketPair.StringPairs[rand.Next(BracketPair.StringPairs.Count)];
                bracketPair.CharacterPositions[0].SetCharacter(bracketString.Substr(0, 1));
                bracketPair.CharacterPositions[bracketPair.Length - 1].SetCharacter(bracketString.Substr(1, 1));
            }
        }

        private void UpdateBracketFocusNeighbours()
        {
            foreach(BracketPair pair in _bracketPairs)
            {
                TerminalCharacter firstBracket = pair.CharacterPositions[0];
                firstBracket.FocusNeighbourRight = pair.CharacterPositions[pair.Length - 1].FocusNeighbourRight;
            }
        }

        private void NewHistoryLine(String text)
        {
            for (int i = 0; i < _historyLines.Count - 1; i++)
            {
                HistoryLine currentLine = _historyLines[i];
                HistoryLine belowLine = _historyLines[i + 1];
                currentLine.Text = belowLine.Text;
            }

            HistoryLine bottomLine = _historyLines[_historyLines.Count - 1];
            bottomLine.Text = text;
        }

        private void ProcessUserSelection()
        {
            if (_terminalCharWordOwners.ContainsKey(_focusedTerminalCharacter))
            {
                PuzzleWord selectedWord = _terminalCharWordOwners[_focusedTerminalCharacter];
                if (!selectedWord.Used)
                {
                    NewHistoryLine($"{selectedWord.WordString.ToUpper()} | {selectedWord.Correctness}/{_solutionString.Length}");
                    if (selectedWord.Correctness == _solutionString.Length)
                    {
                        GameWin();
                    }
                    else
                    {
                        IncrementAttemptCounter();
                        // Hide delected word
                        selectedWord.Used = true;
                        foreach (TerminalCharacter termChar in selectedWord.CharacterPositions)
                        {
                            termChar.SetCharacter(".");
                        }
                        _selectionLine.UpdateLine(_focusedTerminalCharacter.Text);
                    }
                }
            }
            else if (_terminalCharBracketOwners.ContainsKey(_focusedTerminalCharacter))
            {
                BracketPair bracketPair = _terminalCharBracketOwners[_focusedTerminalCharacter];
                if (!bracketPair.Used && bracketPair.CharacterPositions[0] == _focusedTerminalCharacter)
                {
                    UseBracketPair(bracketPair);
                }
            }
        }

        private void IncrementAttemptCounter(int increment = 1)
        {
            _attemptsCounter = Mathf.Clamp(_attemptsCounter + increment, 0, _appemptsMax);
            if(_attemptsCounter == _appemptsMax)
            {
                GameOver();
            }
            else
            {
                _attemptsLabel.Text = $"Remaining attempts: {_appemptsMax - _attemptsCounter}";
            }
        }

        private void UseBracketPair(BracketPair selectedPair)
        {
            // Hide selectedPair
            selectedPair.Used = true;
            foreach (TerminalCharacter termChar in selectedPair.CharacterPositions)
            {
                termChar.Highlight(false);
                termChar.SetCharacter(".");
            }

            if (GD.Randf() < 0.5)
            {
                IncrementAttemptCounter(-1); // Gain an Attempt
                NewHistoryLine("Attempt gained");
            }
            else
            {
                RemoveDud();
                NewHistoryLine("Dud removed");
            }
        }

        private void RemoveDud()
        {
            Random rand = new Random();
            while (true)
            {
                PuzzleWord randPuzzleWord = _puzzleWords[rand.Next(_puzzleWords.Count)];
                if (!randPuzzleWord.Used && randPuzzleWord.Correctness != _solutionString.Length)
                {
                    randPuzzleWord.Used = true;
                    foreach (TerminalCharacter termChar in randPuzzleWord.CharacterPositions)
                    {
                        termChar.Highlight(false);
                        termChar.SetCharacter(".");
                    }
                    break;
                }
            }
        }

        private void GameOver()
        {
            _terminalAudio.PlayBleepBad();
            _attemptsLabel.Text = "Locked out";
            GD.Print("!!! Game Over !!!");
            // TODO: show a game over screen
            Exit();
        }

        private void GameWin()
        {
            _terminalAudio.PlayBleepGood();
            _attemptsLabel.Text = "Access Granted";
            // TODO: show a win screen
            GD.Print("!!! Game Win !!!");
            Exit();
        }

        private void Exit()
        {
            GetViewport().Disconnect("gui_focus_changed", this, nameof(OnGUIFocusChanged));
            EmitSignal(nameof(SwitchPageRequest), "Menu");
        }
    }
}

