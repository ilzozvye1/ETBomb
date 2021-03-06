﻿using System.Collections.Generic;
using AkaUI;
using ET;
using GameEventType;

namespace Bomb.View
{
    [UI(Preload = true)]
    public partial class RoomPage: UIPage
    {
        private PlayerPanel[] playerPanels;
        private HandCardPanel handCardPanel;

        protected override void OnCreate()
        {
            // 测试按钮面板
            this.AddPanel<TestButtonGroup>();

            this.handCardPanel = this.AddPanel<HandCardPanel>();
            CreatePlayerPanels();

            // 房间按钮事件
            this._destroyRoomBtn.onClick.AddListener(() => { LobbyPlayer.Ins.ExitRoom(true).Coroutine(); });
            this._exitRoomBtn.onClick.AddListener(() => { LobbyPlayer.Ins.ExitRoom().Coroutine(); });
            this._readyBtn.onClick.AddListener(() =>
                    LobbyPlayer.Ins.RoomOp(RoomOpType.Ready).Coroutine());
            this._notPopBtn.onClick.AddListener(() => { LobbyPlayer.Ins.RoomOp(RoomOpType.NotPlay).Coroutine(); });
            this._promptBtn.onClick.AddListener(() =>
            {
                List<Card> cards = LocalPlayerComponent.Instance.Player.Promp();
                if (cards != null)
                {
                    Log.Debug($"提示:{cards.ToText()}");
                    handCardPanel.SelectPrompt(cards);
                }
                else
                {
                    Log.Debug($"手上没有大于的牌!");
                }
            });

            this._popBtn.onClick.AddListener(async () =>
            {
                if (CardComponent.Lock)
                {
                    return;
                }

                handCardPanel.Lock();

                List<Card> cards = this.handCardPanel.GetSelectedCards();
                try
                {
                    int errorCode = await LocalPlayerComponent.Instance.Player.Play(cards);
                    if (errorCode == ErrorCode.ERR_Success)
                    {
                        // 出牌成功，移除手牌中的牌View
                        this.handCardPanel.RemoveSelectedCard();
                    }
                    else
                    {
                        // TODO 根据错误码进行提示
                    }
                }
                finally
                {
                    handCardPanel.UnLock();
                }
            });

            // Player事件
            this.Subscribe<PlayerRoomEvent>(On);
            this.Subscribe<GameStartEvent>(On);
            this.Subscribe<TeamChangedEvent>(On);
            this.Subscribe<TurnGameEvent>(On);
        }

        private void On(TurnGameEvent e)
        {
            var room = Game.Scene.GetComponent<Room>();
            var game = room.GetComponent<GameController>();
            var lastPlayer = room.Get(game.LastOpSeat);

            var panel = GetPlayerPanel(game.LastOpSeat);
            panel.ShowPopTime(false);
            panel.ShowNotImage(lastPlayer.Action == PlayerAction.NotPlay);

            // 玩家出了牌
            if (lastPlayer.Action == PlayerAction.Play)
            {
                panel.ShowPopCards(this._card, game.DeskCards);
                panel.RefreshPokerNumber(lastPlayer);
            }

            // 当前出牌的玩家
            var curPanel = GetPlayerPanel(game.CurrentSeat);
            curPanel.ClearPlayCards();
            curPanel.ShowNotImage(false);
            ShowInteractionUI(false);

            // LocalPlayer出牌 显示按钮
            if (game.CurrentSeat != LocalPlayerComponent.Instance.LocalPlayerSeatIndex)
            {
                curPanel.ShowPopTime();
                return;
            }

            ShowInteractionUI();
        }

        /// <summary>
        /// 显示出牌交互UI
        /// </summary>
        /// <param name="show"></param>
        private void ShowInteractionUI(bool show = true)
        {
            this._popBtn.gameObject.SetActive(show);
            this._promptBtn.gameObject.SetActive(show);
            this._notPopBtn.gameObject.SetActive(show);
        }

        private void On(TeamChangedEvent e)
        {
            Player player = Game.Scene.GetComponent<Room>().GetSameTeam(LocalPlayerComponent.Instance.Player);
            GetPlayerPanel(player.SeatIndex).ShowTeam();
        }

        private void CreatePlayerPanels()
        {
            // 创建玩家面板
            playerPanels = new PlayerPanel[4];
            for (int i = 0; i < this.playerPanels.Length; i++)
            {
                var panel = new PlayerPanel(i);
                this.AddPanel(panel, nameof (PlayerPanel) + i);
                this.playerPanels[i] = panel;
            }
        }

        protected override void OnOpen(object args = null)
        {
            ReflushRoomInfo();
            Reset();
        }

        private void ReflushRoomInfo()
        {
            var room = Game.Scene.GetComponent<Room>();
            var gameInfo = room.GetComponent<GameInfo>();

            this._roomInfoText.text = $"房间号: {room.Num} 局数:{gameInfo.Count}/5";
        }

        private void On(GameStartEvent e)
        {
            this._firendBtn.gameObject.SetActive(e.GameOver);
            this._exitRoomBtn.gameObject.SetActive(e.GameOver);
            this._destroyRoomBtn.gameObject.SetActive(e.GameOver);

            if (e.GameOver)
            {
                // 先重置UI
                Reset();

                // 重新显示Player
                for (int i = 0; i < Game.Scene.GetComponent<Room>().Players.Length; i++)
                {
                    EventBus.Publish(new PlayerRoomEvent { Seat = i, Action = PlayerRoomEvent.ActionState.Enter });
                }

                return;
            }

            // 创建手牌
            var handCards = LocalPlayerComponent.Instance.Player.GetComponent<HandCardsComponent>().Cards;
            CardViewHelper.CreateCards(this._card, this._handCardPanel.transform, handCards);
            this.handCardPanel.Reflush();

            foreach (PlayerPanel playerPanel in this.playerPanels)
            {
                playerPanel.StartGame();
            }

            // 显示局数
            ReflushRoomInfo();
        }

        private void On(PlayerRoomEvent e)
        {
            PlayerPanel panel = GetPlayerPanel(e.Seat);
            Player player = Game.Scene.GetComponent<Room>().Get(e.Seat);
            if (e.Action == PlayerRoomEvent.ActionState.Exit)
            {
                player = null;
            }
            else
            {
                // LocalPlayer准备
                if (player.IsReady && e.Seat == LocalPlayerComponent.Instance.LocalPlayerSeatIndex)
                {
                    LocalPlayerReady(player.IsReady);
                }
            }

            panel.Refresh(player);
        }

        private void LocalPlayerReady(bool ready)
        {
            this._readyBtn.gameObject.SetActive(!ready);
        }

        private PlayerPanel GetPlayerPanel(int seat)
        {
            // 映射   
            int uiSeat = SeatHelper.MappingToView(LocalPlayerComponent.Instance.LocalPlayerSeatIndex, seat, this.playerPanels.Length);
            return this.playerPanels[uiSeat];
        }

        private void Reset()
        {
            this._firendBtn.gameObject.SetActive(true);
            this._exitRoomBtn.gameObject.SetActive(true);
            this._destroyRoomBtn.gameObject.SetActive(true);
            this._readyBtn.gameObject.SetActive(true);

            this._cancelReadyBtn.gameObject.SetActive(false);

            this._popBtn.gameObject.SetActive(false);
            this._notPopBtn.gameObject.SetActive(false);
            this._promptBtn.gameObject.SetActive(false);

            for (int i = 0; i < this.playerPanels.Length; i++)
            {
                this.playerPanels[i].Reset();
            }

            this.handCardPanel.Reset();

            this.ShowInteractionUI(false);
        }
    }
}