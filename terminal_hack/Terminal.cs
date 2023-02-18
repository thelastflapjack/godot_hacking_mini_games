using Godot;
using System;
using System.Collections;
using System.Collections.Generic;


namespace HackingMiniGames.TerminalHack
{
    public class Terminal : Spatial
    {
        [Export]
        private NodePath _initialPage = null;


        private TerminalPage _currentPage;
        private Dictionary<String, TerminalPage> _allPages = new Dictionary<String, TerminalPage>();
        private Audio _audio;
        private bool _isActive;

        public override void _Ready()
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            ChangePage(GetNode<TerminalPage>(_initialPage));
            _audio = GetNode<Audio>("Audio");

            foreach(Control node in GetNode<Viewport>("ViewportContainer/Viewport").GetChildren())
            {
                if (node is TerminalPage)
                {
                    _allPages.Add(node.Name, node as TerminalPage);
                    (node as TerminalPage).SetTerminalAudio(_audio);
                }
            }

            _audio.StartBackgroundLoop();
            _audio.PlayBleepGood();
            _isActive = true;
        }

        public override void _Process(float delta)
        {
            if (_isActive)
            {
                bool isNavigationInput = (
                    Input.IsActionJustPressed("ui_left") || Input.IsActionJustPressed("ui_right") || 
                    Input.IsActionJustPressed("ui_up") || Input.IsActionJustPressed("ui_down")
                );
                if (isNavigationInput)
                {
                    _audio.PlayNavKey();
                }
            }
        }
        

        private void OnCurrentPageStartHackRequest(Game.Difficulty difficulty)
        {
            Game hackGame = (Game)_allPages["Game"];
            hackGame.StartNewGame(difficulty);
            ChangePage(hackGame);
        }

        private void OnCurrentPageSwitchRequest(String targetPageName)
        {
            if (_allPages.ContainsKey(targetPageName))
            {
                ChangePage(_allPages[targetPageName]);
            }
        }


        private void ChangePage(TerminalPage targetPage)
        {
            if (_currentPage != null)
            {
                _currentPage.Activate(false);
                _currentPage.Disconnect(nameof(TerminalPage.SwitchPageRequest), this, nameof(OnCurrentPageSwitchRequest));
                _currentPage.Disconnect(nameof(TerminalPage.StartHackRequest), this, nameof(OnCurrentPageStartHackRequest));
            }

            targetPage.Activate(true);
            _currentPage = targetPage;
            _currentPage.Connect(nameof(TerminalPage.SwitchPageRequest), this, nameof(OnCurrentPageSwitchRequest));
            _currentPage.Connect(nameof(TerminalPage.StartHackRequest), this, nameof(OnCurrentPageStartHackRequest));
        }
    }
}

