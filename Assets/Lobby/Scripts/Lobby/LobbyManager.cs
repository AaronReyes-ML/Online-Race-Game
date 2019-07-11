using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using System.Collections;
using System.Collections.Generic;
using System.IO;


namespace Prototype.NetworkLobby
{
    public class LobbyManager : NetworkLobbyManager 
    {
        public RectTransform VehicleSelectPanel;

        public Image normalCarImage;
        public Image fastCarImage;
        public Image heavyCarImage;

        public Button normalButton;
        public Button fastButton;
        public Button heavyButton;

        public Text normalText;
        public Text fastText;
        public Text heavyText;

        public Image normalChecked;
        public Image fastChecked;
        public Image heavyChecked;

        public Button selectVehicleButton;


        public Text personalScoresLight;
        public Text personalScoresNormal;
        public Text personalScoresHeavy;

        string userDataPath = "userinfo.txt";

        Dictionary<string, string> userDataList = new Dictionary<string, string>();

        public Text usernameRepeated;
        public Text usernameDoesNotExist;
        public Text passwordIncorrect;
        public Text userCreatedSuccess;

        public InputField newUsername;
        public InputField newPassword;

        public InputField existingUsername;
        public InputField existingPassword;

        public static string loggedInName;
        public static int carType = 1;

        static short MsgKicked = MsgType.Highest + 1;

        static public LobbyManager s_Singleton;

        public Text topscores;
        public Text toptimes;

        [Header("Unity UI Lobby")]
        [Tooltip("Time in second between all players ready & match start")]
        public float prematchCountdown = 5.0f;

        [Space]
        [Header("UI Reference")]
        public LobbyTopPanel topPanel;

        public RectTransform loginPanel;

        public RectTransform mainMenuPanel;
        public RectTransform lobbyPanel;

        public RectTransform serverPanel;
        public RectTransform leaveOnPanel;

        public LobbyInfoPanel infoPanel;
        public LobbyCountdownPanel countdownPanel;
        public GameObject addPlayerButton;

        protected RectTransform currentPanel;

        public Button backButton;

        public Text statusInfo;
        public Text hostInfo;

        //Client numPlayers from NetworkManager is always 0, so we count (throught connect/destroy in LobbyPlayer) the number
        //of players, so that even client know how many player there is.
        [HideInInspector]
        public int _playerNumber = 0;

        //used to disconnect a client properly when exiting the matchmaker
        [HideInInspector]
        public bool _isMatchmaking = false;

        protected bool _disconnectServer = false;
        
        protected ulong _currentMatchID;

        protected LobbyHook _lobbyHooks;

        void Start()
        {
            if (!File.Exists("userinfo.txt"))
            {
                StreamWriter sw = new StreamWriter("userinfo.txt");
                sw.Close();
            }
            if (!File.Exists("topscores.txt"))
            {
                StreamWriter sw = new StreamWriter("topscores.txt");
                for (int i = 0; i < 10; i++)
                {
                    sw.WriteLine(0);
                }
                sw.Close();
            }
            if (!File.Exists("toptimes.txt"))
            {
                StreamWriter sw = new StreamWriter("toptimes.txt");
                for (int i = 0; i < 10; i++)
                {
                    sw.WriteLine(100000);
                }
                sw.Close();
            }

            s_Singleton = this;
            _lobbyHooks = GetComponent<Prototype.NetworkLobby.LobbyHook>();

            HandleServerQuestion();

            HideAll();

            currentPanel = mainMenuPanel;

            normalChecked.enabled = true;
            fastChecked.enabled = false;
            heavyChecked.enabled = false;

            selectVehicleButton.enabled = true;
            VehicleSelectPanel.gameObject.SetActive(false);

            backButton.gameObject.SetActive(false);
            GetComponent<Canvas>().enabled = true;

            DontDestroyOnLoad(gameObject);

            SetServerInfo("Offline", "None");
        }

        public void HandleServerQuestion()
        {
            loginPanel.gameObject.SetActive(false);
            mainMenuPanel.gameObject.SetActive(false);
            serverPanel.gameObject.SetActive(true);
        }

        public void Yes()
        {
            serverPanel.gameObject.SetActive(false);
            leaveOnPanel.gameObject.SetActive(true);
        }

        public void No()
        {
            HandleLogin();
        }

        public void HandleLogin()
        {
            serverPanel.gameObject.SetActive(false);
            mainMenuPanel.gameObject.SetActive(false);
            loginPanel.gameObject.SetActive(true);
        }

        public void Login()
        {
            HideAll();
            if (ValidateExistingUser())
            {
                loginPanel.gameObject.SetActive(false);
                mainMenuPanel.gameObject.SetActive(true);
            }
        }

        public void Create()
        {
            HideAll();
            CreateNewUser();
        }

        public override void OnLobbyClientSceneChanged(NetworkConnection conn)
        {
            if (SceneManager.GetSceneAt(0).name == lobbyScene)
            {
                if (topPanel.isInGame)
                {
                    ChangeTo(lobbyPanel);
                    if (_isMatchmaking)
                    {
                        if (conn.playerControllers[0].unetView.isServer)
                        {
                            backDelegate = StopHostClbk;
                        }
                        else
                        {
                            backDelegate = StopClientClbk;
                        }
                    }
                    else
                    {
                        if (conn.playerControllers[0].unetView.isClient)
                        {
                            backDelegate = StopHostClbk;
                        }
                        else
                        {
                            backDelegate = StopClientClbk;
                        }
                    }
                }
                else
                {
                    ChangeTo(mainMenuPanel);
                }

                topPanel.ToggleVisibility(true);
                topPanel.isInGame = false;
            }
            else
            {
                ChangeTo(null);

                Destroy(GameObject.Find("MainMenuUI(Clone)"));

                //backDelegate = StopGameClbk;
                topPanel.isInGame = true;
                topPanel.ToggleVisibility(false);
            }
        }

        public void ChangeTo(RectTransform newPanel)
        {

            SetPersonalScores();
            if (currentPanel != null)
            {
                currentPanel.gameObject.SetActive(false);
            }

            if (newPanel != null)
            {
                newPanel.gameObject.SetActive(true);
            }

            currentPanel = newPanel;

            if (currentPanel != mainMenuPanel)
            {
                backButton.gameObject.SetActive(true);
            }
            else
            {
                backButton.gameObject.SetActive(false);
                SetServerInfo("Offline", "None");
                _isMatchmaking = false;
            }
        }

        public void DisplayIsConnecting()
        {
            var _this = this;
            infoPanel.Display("Connecting...", "Cancel", () => { _this.backDelegate(); });
        }

        public void SetServerInfo(string status, string host)
        {
            //statusInfo.text = status;
            //hostInfo.text = host;
        }


        public delegate void BackButtonDelegate();
        public BackButtonDelegate backDelegate;
        public void GoBackButton()
        {
            backDelegate();
			topPanel.isInGame = false;
        }

        // ----------------- Server management

        public void AddLocalPlayer()
        {
            TryToAddPlayer();
        }

        public void RemovePlayer(LobbyPlayer player)
        {
            player.RemovePlayer();
        }

        public void SimpleBackClbk()
        {
            ChangeTo(mainMenuPanel);
        }
                 
        public void StopHostClbk()
        {
            if (_isMatchmaking)
            {
				matchMaker.DestroyMatch((NetworkID)_currentMatchID, 0, OnDestroyMatch);
				_disconnectServer = true;
            }
            else
            {
                StopHost();
            }

            
            ChangeTo(mainMenuPanel);
        }

        public void StopClientClbk()
        {
            StopClient();

            if (_isMatchmaking)
            {
                StopMatchMaker();
            }

            ChangeTo(mainMenuPanel);
        }

        public void StopServerClbk()
        {
            StopServer();
            ChangeTo(mainMenuPanel);
        }

        class KickMsg : MessageBase { }
        public void KickPlayer(NetworkConnection conn)
        {
            conn.Send(MsgKicked, new KickMsg());
        }

        public void KickedMessageHandler(NetworkMessage netMsg)
        {
            infoPanel.Display("Kicked by Server", "Close", null);
            netMsg.conn.Disconnect();
        }

        //===================

        public override void OnStartHost()
        {
            base.OnStartHost();

            ChangeTo(lobbyPanel);
            backDelegate = StopHostClbk;
            SetServerInfo("Hosting", networkAddress);
        }

		public override void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
		{
			base.OnMatchCreate(success, extendedInfo, matchInfo);
            _currentMatchID = (System.UInt64)matchInfo.networkId;
		}

		public override void OnDestroyMatch(bool success, string extendedInfo)
		{
			base.OnDestroyMatch(success, extendedInfo);
			if (_disconnectServer)
            {
                StopMatchMaker();
                StopHost();
            }
        }

        //allow to handle the (+) button to add/remove player
        public void OnPlayersNumberModified(int count)
        {
            _playerNumber += count;

            int localPlayerCount = 0;
            foreach (PlayerController p in ClientScene.localPlayers)
                localPlayerCount += (p == null || p.playerControllerId == -1) ? 0 : 1;

            addPlayerButton.SetActive(localPlayerCount < maxPlayersPerConnection && _playerNumber < maxPlayers);
        }

        // ----------------- Server callbacks ------------------

        //we want to disable the button JOIN if we don't have enough player
        //But OnLobbyClientConnect isn't called on hosting player. So we override the lobbyPlayer creation
        public override GameObject OnLobbyServerCreateLobbyPlayer(NetworkConnection conn, short playerControllerId)
        {
            GameObject obj = Instantiate(lobbyPlayerPrefab.gameObject) as GameObject;

            LobbyPlayer newPlayer = obj.GetComponent<LobbyPlayer>();
            newPlayer.ToggleJoinButton(numPlayers + 1 >= minPlayers);

            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers + 1 >= minPlayers);
                }
            }

            return obj;
        }

        public override void OnLobbyServerPlayerRemoved(NetworkConnection conn, short playerControllerId)
        {
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers + 1 >= minPlayers);
                }
            }
        }

        public override void OnLobbyServerDisconnect(NetworkConnection conn)
        {
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers >= minPlayers);
                }
            }

        }

        public override bool OnLobbyServerSceneLoadedForPlayer(GameObject lobbyPlayer, GameObject gamePlayer)
        {
            //This hook allows you to apply state data from the lobby-player to the game-player
            //just subclass "LobbyHook" and add it to the lobby object.

            if (_lobbyHooks)
                _lobbyHooks.OnLobbyServerSceneLoadedForPlayer(this, lobbyPlayer, gamePlayer);

            return true;
        }

        // --- Countdown management

        public override void OnLobbyServerPlayersReady()
        {
			bool allready = true;
			for(int i = 0; i < lobbySlots.Length; ++i)
			{
				if(lobbySlots[i] != null)
					allready &= lobbySlots[i].readyToBegin;
			}

			if(allready)
				StartCoroutine(ServerCountdownCoroutine());
        }

        public IEnumerator ServerCountdownCoroutine()
        {
            float remainingTime = prematchCountdown;
            int floorTime = Mathf.FloorToInt(remainingTime);

            while (remainingTime > 0)
            {
                yield return null;

                remainingTime -= Time.deltaTime;
                int newFloorTime = Mathf.FloorToInt(remainingTime);

                if (newFloorTime != floorTime)
                {//to avoid flooding the network of message, we only send a notice to client when the number of plain seconds change.
                    floorTime = newFloorTime;

                    for (int i = 0; i < lobbySlots.Length; ++i)
                    {
                        if (lobbySlots[i] != null)
                        {//there is maxPlayer slots, so some could be == null, need to test it before accessing!
                            (lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(floorTime);
                        }
                    }
                }
            }

            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                if (lobbySlots[i] != null)
                {
                    (lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(0);
                }
            }

            ServerChangeScene(playScene);
        }

        // ----------------- Client callbacks ------------------

        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);

            infoPanel.gameObject.SetActive(false);

            conn.RegisterHandler(MsgKicked, KickedMessageHandler);

            if (!NetworkServer.active)
            {//only to do on pure client (not self hosting client)
                ChangeTo(lobbyPanel);
                backDelegate = StopClientClbk;
                SetServerInfo("Client", networkAddress);
            }
        }


        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            ChangeTo(mainMenuPanel);
        }

        public override void OnClientError(NetworkConnection conn, int errorCode)
        {
            ChangeTo(mainMenuPanel);
            infoPanel.Display("Cient error : " + (errorCode == 6 ? "timeout" : errorCode.ToString()), "Close", null);
        }

        public bool CreateNewUser()
        {
            ReadUserData();
            string username = newUsername.text;
            string password = newPassword.text;

            if (userDataList.ContainsKey(username))
            {
                Debug.Log("Repeat username found: " + username);
                ShowUsernameRepeated();
                return false;
            }

            StreamWriter dataWriter = new StreamWriter(userDataPath, true);
            dataWriter.WriteLine(username);
            dataWriter.WriteLine(password);

            dataWriter.Close();

            ShowUserCreatedSuccess();
            return true;
        }

        public bool ValidateExistingUser()
        {
            ReadUserData();
            string username = existingUsername.text;
            string password = existingPassword.text;

            if (!userDataList.ContainsKey(username))
            {
                Debug.Log("No user with this username");
                ShowUsernameDNE();
                return false;
            }

            if (userDataList[username] != password)
            {
                Debug.Log("Password does not match for given user");
                ShowPasswordIncorrect();
                return false;
            }

            loggedInName = username;
            SetPersonalScores();

            return true;
        }

        public void ShowUserCreatedSuccess()
        {
            userCreatedSuccess.enabled = true;
        }

        public void HideUserCreatedSuccess()
        {
            userCreatedSuccess.enabled = false;
        }

        public void ShowUsernameRepeated()
        {
            usernameRepeated.enabled = true;
        }

        public void HideUsernameRepeated()
        {
            usernameRepeated.enabled = false;
        }

        public void ShowUsernameDNE()
        {
            usernameDoesNotExist.enabled = true;
        }

        public void HideUsernameDNE()
        {
            usernameDoesNotExist.enabled = false;
        }

        public void ShowPasswordIncorrect()
        {
            passwordIncorrect.enabled = true;
        }

        public void HidePasswordIncorrect()
        {
            passwordIncorrect.enabled = false;
        }

        public void HideAll()
        {
            HidePasswordIncorrect();
            HideUsernameDNE();
            HideUsernameRepeated();
            HideUserCreatedSuccess();
        }

        public void ReadUserData()
        {
            userDataList.Clear();

            StreamReader dataReader = new StreamReader(userDataPath);

            while (!dataReader.EndOfStream)
            {
                if (dataReader.Peek() != -1)
                {
                    userDataList.Add(dataReader.ReadLine(), dataReader.ReadLine());
                }
            }

            dataReader.Close();
        }

        public void InitializeScoreFile()
        {
            if (!File.Exists("PlayerScores/" + Prototype.NetworkLobby.LobbyManager.loggedInName + "0.txt"))
            {
                StreamWriter sw = new StreamWriter("PlayerScores/" + Prototype.NetworkLobby.LobbyManager.loggedInName + "0.txt");
                for (int i = 0; i < 10; i++)
                {
                    sw.WriteLine(0);
                }
                sw.Close();
            }
            if (!File.Exists("PlayerScores/" + Prototype.NetworkLobby.LobbyManager.loggedInName + "1.txt"))
            {
                StreamWriter sw = new StreamWriter("PlayerScores/" + Prototype.NetworkLobby.LobbyManager.loggedInName + "1.txt");
                for (int i = 0; i < 10; i++)
                {
                    sw.WriteLine(0);
                }
                sw.Close();
            }
            if (!File.Exists("PlayerScores/" + Prototype.NetworkLobby.LobbyManager.loggedInName + "2.txt"))
            {
                StreamWriter sw = new StreamWriter("PlayerScores/" + Prototype.NetworkLobby.LobbyManager.loggedInName + "2.txt");
                for (int i = 0; i < 10; i++)
                {
                    sw.WriteLine(0);
                }
                sw.Close();
            }
        }

        public void SetPersonalScores()
        {

            InitializeScoreFile();

            StreamReader personalReader = new StreamReader("PlayerScores/" + Prototype.NetworkLobby.LobbyManager.loggedInName + "0.txt");

            personalScoresLight.text = "";

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    personalScoresLight.text += personalReader.ReadLine();
                    personalScoresLight.text += ", ";
                }
                catch
                {
                    break;
                }
            }

            personalReader.Close();

            personalReader = new StreamReader("PlayerScores/" + Prototype.NetworkLobby.LobbyManager.loggedInName + "1.txt");

            personalScoresNormal.text = "";

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    personalScoresNormal.text += personalReader.ReadLine();
                    personalScoresNormal.text += ", ";
                }
                catch
                {
                    break;
                }
            }

            personalReader.Close();

            personalReader = new StreamReader("PlayerScores/" + Prototype.NetworkLobby.LobbyManager.loggedInName + "2.txt");

            personalScoresHeavy.text = "";

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    personalScoresHeavy.text += personalReader.ReadLine();
                    personalScoresHeavy.text += ", ";
                }
                catch
                {
                    break;
                }
            }

            personalReader.Close();
        }

        public void HideAllCarSelectionOptions()
        {
            VehicleSelectPanel.gameObject.SetActive(false);
            selectVehicleButton.enabled = true;
        }

        public void ShowAllCarSelectionOptions()
        {
            VehicleSelectPanel.gameObject.SetActive(true);
            selectVehicleButton.enabled = false;
        }

        public void SelectNormal()
        {
            normalChecked.enabled = true;
            carType = 1;
            fastChecked.enabled = false;
            heavyChecked.enabled = false;
            HideAllCarSelectionOptions();
        }

        public void SelectFast()
        {
            normalChecked.enabled = false;
            fastChecked.enabled = true;
            carType = 0;
            heavyChecked.enabled = false;
            HideAllCarSelectionOptions();
        }

        public void SelectHeavy()
        {
            normalChecked.enabled = false;
            fastChecked.enabled = false;
            heavyChecked.enabled = true;
            carType = 2;
            HideAllCarSelectionOptions();
        }
    }
}
