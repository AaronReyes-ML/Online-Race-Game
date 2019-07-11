using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System.Diagnostics;


public class CarMovement : NetworkBehaviour
{
    #region variables

    private System.Diagnostics.Stopwatch raceTimer;

    private const short MessageToServer = 299;

    public SpriteRenderer CarSprite;

    public Sprite NormalSprite;
    public Sprite FastSprite;
    public Sprite HeavySprite;

    [SyncVar]
    public string PlayerName;

    [SyncVar]
    public bool isInvincible;

    [SyncVar]
    public int score;

    [SyncVar]
    public double raceTime;

    [SyncVar]
    public double raceFinishTime = int.MaxValue;

    [SyncVar]
    public int carType = 1; //0 light //1 regular //2 heavy

    [SyncVar]
    public float rotateMultiplier = 1;

    [SyncVar]
    public float speedMultiplier = 1;

    GameObject networkManager;
    ConnectedPlayers connectedPlayers;

    #region playerControlSettings

    public float m_Speed = 50f; //used for normal drive
    public float m_maxVelocity = 35; //used for drift drive
    public float m_TurnSpeed = 180f; //used for both
    public float m_AccelRate = 0.1f; //used for both
    public Text m_ResetText;
    private string m_90sButton; //used to activate drift drive

    #endregion playerControlSettings

    #region mapSettings

    public int m_PlayerNumber = 1;
    public int m_TotalCheckpoints = 3;
    public int m_TotalLaps = 3;
    public GameObject m_resetLocation;

    #endregion mapSettings

    #region visualSettings

    public ParticleSystem m_ExhaustExplosion;
    public Slider m_Slider;
    public Image m_FillImage;
    public Color m_FullSpeedColor = Color.red;
    public Color m_ZeroSpeedColor = Color.yellow;
    public Image m_CatOnImage;
    public Sprite m_Empty;
    public Sprite m_CatOn;
    private bool hasCatOn;
    private float catOffFactor;

    #endregion visualSettingsS

    #region auidoSettings

    public AudioSource m_RunningInThe90sAudio;
    public AudioSource m_BackTurbineSource;
    public AudioSource m_EngineAudio;
    public AudioClip m_EngineIdle;
    public AudioClip m_EngineDriving;
    public AudioClip m_EngineInThe90s;
    public AudioClip m_BackTurbine;
    public AudioClip m_RunningInThe90s;

    #endregion audioSettings

    #region privateControls

    private string m_MovementAxisName;          // The name of the input axis for moving forward and back.
    private string m_TurnAxisName;              // The name of the input axis for turning.
    private Rigidbody m_Rigidbody;
    private float m_MovementInputValue;         // The current value of the movement input.
    private float m_TurnInputValue;             // The current value of the turn input.
    private float m_velocity;
    private string m_resetButton;
    [SyncVar]
    public int m_lastCheckpoint = 0;
    [SyncVar]
    private int m_laps = 0;
    private Canvas m_UICanvas;
    private Text m_LapsText;
    private bool inSpeedZone;
    private bool isRunningInThe90s = false;
    [SyncVar]
    public bool isFinished;

    [SyncVar]
    public bool dontMove = false;

    #endregion privateControls

    #endregion variables

    #region startup

    private void Awake()
    {
        raceTimer = new Stopwatch();
        m_Rigidbody = GetComponent<Rigidbody>();
        m_UICanvas = GetComponentInChildren<Canvas>();
        m_LapsText = m_UICanvas.GetComponentInChildren<Text>();
        m_CatOnImage.sprite = m_Empty;
        DontDestroyOnLoad(this);
        m_LapsText.enabled = false;
        raceTimer.Start();
        raceTime = 0;
        score = 0;
    }

    private void OnEnable()
    {
        // When the tank is turned on, make sure it's not kinematic.
        m_Rigidbody.isKinematic = false;

        // Also reset the input values.
        m_MovementInputValue = 0f;
        m_TurnInputValue = 0f;
    }

    private void Start()
    {
        if (isServer)
        {
            DeathRaceManager.AddCar(this);
            NetworkServer.RegisterHandler(MessageToServer, ServerRecieveMessage);
        }

        if (isLocalPlayer)
        {
            CmdSetNameAndType(Prototype.NetworkLobby.LobbyManager.loggedInName, Prototype.NetworkLobby.LobbyManager.carType);
            if (carType == 0)
            {
                GetComponent<CarHealth>().CmdSetStartHealth(50);
                GetComponent<CarHealth>().CmdUpdateSliderMax(50);
            }
            else if (carType == 2)
            {
                GetComponent<CarHealth>().CmdSetStartHealth(500);
                GetComponent<CarHealth>().CmdUpdateSliderMax(50);
            }
            else
            {
                GetComponent<CarHealth>().CmdSetStartHealth(100);
                GetComponent<CarHealth>().CmdUpdateSliderMax(100);
            }
        }

        connectedPlayers = GetComponent<ConnectedPlayers>();

        m_MovementAxisName = "Vertical" + 1;
        m_TurnAxisName = "Horizontal" + 1;
        m_resetButton = "Reset" + 1;
        m_90sButton = "90s" + 1;

        m_LapsText.text = m_laps.ToString() + "/" + m_TotalLaps;
        m_resetLocation.transform.parent = null;
        m_EngineAudio.clip = m_EngineIdle;
        m_EngineAudio.loop = true;
        m_EngineAudio.volume = 0.1f;
        m_BackTurbineSource.clip = m_BackTurbine;
        m_RunningInThe90sAudio.clip = m_RunningInThe90s;
        m_CatOnImage.sprite = m_Empty;
    }

    [Command]
    void CmdSetNameAndType(string playerName, int cartypename)
    {
        PlayerName = playerName;
        carType = cartypename;

        if (carType == 0)
        {
            speedMultiplier = 1.2f;
            rotateMultiplier = 6f;
        }
        else if (carType == 2)
        {
            speedMultiplier = 0.9f;
            rotateMultiplier = 0.7f;
        }
        else
        {
            speedMultiplier = 1;
            rotateMultiplier = 1;
        }
    }

    public void SetSprite()
    {
        if (carType == 0)
        {
            CarSprite.sprite = FastSprite;
        }
        else if (carType == 2)
        {
            CarSprite.sprite = HeavySprite;
        }
        else
        {
            CarSprite.sprite = NormalSprite;
        }
    }

    #endregion startup

    #region race controllers

    public double GetCurrentRaceTime()
    {
        return raceTimer.Elapsed.Seconds;
    }

    public void Disable()
    {
        dontMove = true;
        CmdIncreasePoints(-100000);
        m_Rigidbody.isKinematic = false;
    }

    [Command]
    public void CmdKill()
    {
        if (!isInvincible)
        {
            this.RpcRaceLoseByExplode();
            if (DeathRaceManager.RemoveCarAndCheckWinner(this))
            {
                CarMovement winner = DeathRaceManager.GetWinnerByKill();
                winner.RpcWonByLastPlayer();
                winner.isFinished = true;
            }
            m_Rigidbody.AddExplosionForce(20000, transform.position, 5);
        }
    }

    [ClientRpc]
    public void RpcWonByLastPlayer()
    {
        GetComponent<ScoreBoard>().CmdUpdateScoreBoard();
        if (isLocalPlayer)
        {
            raceFinishTime = GetCurrentRaceTime();
            GameObject.Find("GameMessages").GetComponentInChildren<Text>().text = "Everyone else was destroyed\n" +
                "Press SHIFT + R to exit race: ";
            isFinished = true;
        }

        if (isServer)
        {
            GetComponent<ScoreBoard>().CmdWriteOutScoresData();
            GetComponent<ScoreBoard>().CmdWriteOutIndividualData();
        }
        GameObject.Find("LobbyManager").GetComponent<ReadScores>().SetScores();
    }

    [ClientRpc]
    public void RpcRaceLoseByExplode()
    {
        GetComponent<ScoreBoard>().CmdUpdateScoreBoard();
        if (isLocalPlayer)
        {
            raceFinishTime = GetCurrentRaceTime();
            GameObject.Find("GameMessages").GetComponentInChildren<Text>().text = "You are destroyed\n" +
                "Press SHIFT + R to exit race: ";
            isFinished = true;
        }

        if (isServer)
        {
            GetComponent<ScoreBoard>().CmdWriteOutScoresData();
            GetComponent<ScoreBoard>().CmdWriteOutIndividualData();
        }
        GameObject.Find("LobbyManager").GetComponent<ReadScores>().SetScores();
    }

    [ClientRpc]
    public void RpcRaceOver()
    {
        GetComponent<ScoreBoard>().CmdUpdateScoreBoard();
        if (isLocalPlayer)
        {
            raceFinishTime = GetCurrentRaceTime();
            GameObject.Find("GameMessages").GetComponentInChildren<Text>().text = "Race finished\n" +
                "Press SHIFT + R to exit race: ";
            isFinished = true;
        }

        if (isServer)
        {
            GetComponent<ScoreBoard>().CmdWriteOutScoresData();
            GetComponent<ScoreBoard>().CmdWriteOutIndividualData();
        }
        GameObject.Find("LobbyManager").GetComponent<ReadScores>().SetScores();
    }

    [ClientRpc]
    public void RpcRaceFinished()
    {
        GetComponent<ScoreBoard>().CmdUpdateScoreBoard();
        if (isLocalPlayer)
        {
            isFinished = true;
            raceFinishTime = GetCurrentRaceTime();
            GameObject.Find("GameMessages").GetComponentInChildren<Text>().text = "You finished the race\n" +
                "Press SHIFT + R to Leave: ";
        }

        if (isServer)
        {
            GetComponent<ScoreBoard>().CmdWriteOutScoresData();
            GetComponent<ScoreBoard>().CmdWriteOutIndividualData();
        }
        GameObject.Find("LobbyManager").GetComponent<ReadScores>().SetScores();
    }

    public void ShowScoreBoard()
    {
        GetComponent<ScoreBoard>().scoreboardText.enabled = true;
    }

    [Command]
    public void CmdClearLists()
    {
        DeathRaceManager.ClearLists();
    }

    void BackToLobby()
    {
        if (!isServer)
        {
            CmdClearLists();
            DeathRaceManager.ClearLists();
            FindObjectOfType<NetworkLobbyManager>().SendReturnToLobby();
        }
        else
        {
            DeathRaceManager.ClearLists();
            FindObjectOfType<NetworkLobbyManager>().SendReturnToLobby();
        }
        //FindObjectOfType<NetworkLobbyManager>().ServerReturnToLobby();
    }

    public void SendMessage()
    {
        StringMessage a = new StringMessage("Return to lobby");
        NetworkManager.singleton.client.Send(MessageToServer, a);
    }

    private void ServerRecieveMessage(NetworkMessage message)
    {
        FindObjectOfType<NetworkLobbyManager>().ServerReturnToLobby();
    }

    void GoToMain()
    {
        FindObjectOfType<NetworkLobbyManager>().ServerChangeScene(FindObjectOfType<NetworkLobbyManager>().lobbyScene);
    }

    #endregion race controllers

    #region update

    private void Update()
    {

        SetSprite();

        if (isLocalPlayer && Camera.main.GetComponent<SmoothFollowCSharp>().target == null)
        {
            Camera.main.GetComponent<SmoothFollowCSharp>().target = transform;
        }

        if (isFinished)
        {
            GetComponent<ScoreBoard>().CmdUpdateScoreBoard();
            ShowScoreBoard();
        }

        if (!isLocalPlayer)
            return;

        if (isFinished)
            isInvincible = true;

        // Store the value of both input axes.
        m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
        m_TurnInputValue = Input.GetAxis(m_TurnAxisName);

        if (hasCatOn)
        {
            catOffFactor += 1 + m_TurnInputValue * 25;
            if (catOffFactor > 700)
            {
                SetCatOff();
            }
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            RpcRaceOver();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnityEngine.Debug.Log("Attempting to return to lobby");
            Invoke("BackToLobby", 0);
        }

        if (isFinished && Input.GetKeyDown(KeyCode.R))
        {
            Invoke("BackToLobby", 0);
        }

        if (Input.GetButtonDown(m_90sButton))
        {
            if (!isRunningInThe90s)
            {
                isRunningInThe90s = true;
                m_RunningInThe90sAudio.Play();
            }
            else
            {
                isRunningInThe90s = false;
                m_RunningInThe90sAudio.Stop();
            }
            m_Rigidbody.velocity = new Vector3(0, 0, 0);
        }

        EngineAudio();
    }


    private void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;

        if (!dontMove)
        {
            Move();
            Turn();
        }
    }

    #endregion update

    #region movement

    [ClientRpc]
    public void RpcSetRotation(Quaternion rotation)
    {
            m_Rigidbody.MoveRotation(rotation);
    }

    [Command]
    public void CmdServerSetRotation(Quaternion rotation)
    {
        if (!isLocalPlayer)
        {
            m_Rigidbody.MoveRotation(rotation);
        }
    }

    private void Move()
    {
        // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
        if (isRunningInThe90s)
        {
            if ((transform.rotation.z * 360 < 70 && transform.rotation.z * 360 > -70) && (m_MovementInputValue > 0.5f || m_MovementInputValue < -0.5f))
            {
                if (!inSpeedZone)
                {
                    Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime * m_AccelRate * speedMultiplier;
                    movement.y = 0;
                    IncrementAccelRate(true);
                    SetRPMUI();
                    if (m_Rigidbody.velocity.magnitude < m_maxVelocity)
                    {
                        m_Rigidbody.velocity = m_Rigidbody.velocity + (movement);
                    }
                }
                else
                {
                    Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime * 2 * m_AccelRate * speedMultiplier;
                    movement.y = 0;
                    IncrementAccelRate(true);
                    SetRPMUI();
                    if (m_Rigidbody.velocity.magnitude < m_maxVelocity)
                    {
                        m_Rigidbody.velocity = m_Rigidbody.velocity + (movement);
                    }
                }
            }
            else
            {
                IncrementAccelRate(false);
                SetRPMUI();
            }
        }
        else
        {
            if ((transform.rotation.z * 360 < 70 && transform.rotation.z * 360 > -70) && (m_MovementInputValue > 0.5f || m_MovementInputValue < -0.5f))
            {
                if (!inSpeedZone)
                {
                    Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime * m_AccelRate * speedMultiplier;
                    movement.y = 0;
                    IncrementAccelRate(true);
                    SetRPMUI();
                    m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
                }
                else
                {
                    Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime * 2 * m_AccelRate * speedMultiplier;
                    movement.y = 0;
                    IncrementAccelRate(true);
                    SetRPMUI();
                    m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
                }
            }
            else
            {
                IncrementAccelRate(false);
                SetRPMUI();
            }
        }
    }

    private void Turn()
    {
        if (isRunningInThe90s)
        {
            if (m_Rigidbody.velocity.magnitude < 3 && m_Rigidbody.velocity.magnitude > -3)
            {

            }
            else
            {
                float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime * rotateMultiplier;
                if (m_Rigidbody.velocity.magnitude < 0)
                {
                    turn = turn * -1;
                }

                Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);

                m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
                RpcSetRotation(m_Rigidbody.rotation);
                CmdServerSetRotation(m_Rigidbody.rotation);
            }
        }
        else
        {
            if (m_MovementInputValue < 0.5f && m_MovementInputValue > -0.5f)
            {

            }
            else
            {
                float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime * rotateMultiplier;
                if (m_MovementInputValue < 0)
                {
                    turn = turn * -1;
                }

                Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);

                m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
                if (isServer)
                    RpcSetRotation(m_Rigidbody.rotation);
                CmdServerSetRotation(m_Rigidbody.rotation);
            }
        }
    }

    #endregion movement

    #region visualaudio

    private void EngineAudio()
    {
        if (isRunningInThe90s)
        {
            if (Mathf.Abs(m_Rigidbody.velocity.magnitude) < 0.1f)
            {
                if (m_EngineAudio.clip == m_EngineDriving || m_EngineAudio.clip == m_EngineInThe90s)
                {
                    m_EngineAudio.clip = m_EngineIdle;
                    m_EngineAudio.Play();
                }
                m_EngineAudio.pitch = 1;
            }
            else
            {
                if (m_EngineAudio.clip == m_EngineIdle)
                {
                    m_EngineAudio.clip = m_EngineInThe90s;
                    m_EngineAudio.Play();
                    m_EngineAudio.pitch = m_AccelRate;
                }
                else
                {
                    m_EngineAudio.pitch = m_AccelRate;
                }
            }
        }
        else
        {
            if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs (m_TurnInputValue) < 0.1f)
            {
                if (m_EngineAudio.clip == m_EngineDriving || m_EngineAudio.clip == m_EngineInThe90s)
                {
                    m_EngineAudio.clip = m_EngineIdle;
                    m_EngineAudio.Play();
                }
                m_EngineAudio.pitch = 1;
            }
            else
            {
                if (m_EngineAudio.clip == m_EngineIdle)
                {
                    m_EngineAudio.clip = m_EngineDriving;
                    m_EngineAudio.Play();
                    m_EngineAudio.pitch = m_AccelRate;
                }
                else
                {
                    m_EngineAudio.pitch = m_AccelRate;
                }
            }
        }
    }

    private void SetRPMUI()
    {
        m_Slider.value = m_AccelRate;

        m_FillImage.color = Color.Lerp(m_ZeroSpeedColor, m_FullSpeedColor, m_AccelRate / 1f);
    }

    private void PlayParticleSystem()
    {
        
        if (m_Rigidbody.velocity.magnitude > 30)
        {
            m_BackTurbineSource.Play();
            m_ExhaustExplosion.Play();
        }
    }

    public void SetCatOn()
    {
        if (!isLocalPlayer)
            return;

        hasCatOn = true;
        m_CatOnImage.sprite = m_CatOn;
    }

    public void SetCatOff()
    {
        if (!isLocalPlayer)
            return;

        hasCatOn = false;
        m_CatOnImage.sprite = m_Empty;
        catOffFactor = 0;
    }

    #endregion visualaudio

    #region helpers

    [Command]
    public void CmdSetLastCheckpoint(int checkpointNumber)
    {
        if (checkpointNumber == m_lastCheckpoint + 1)
        {
            m_lastCheckpoint = checkpointNumber;

            if (m_lastCheckpoint == m_TotalCheckpoints)
            {
                m_laps += 1;
                RpcSetLaps(m_laps);
                m_lastCheckpoint = 0;
                m_LapsText.text = m_laps.ToString() + "/" + m_TotalLaps;
                if (m_laps == m_TotalLaps)
                {
                    GetComponent<CarHealth>().RpcSetInvincible();
                    CmdIncreasePoints(100000000);
                    if (isServer)
                    {
                        GetComponent<ScoreBoard>().CmdWriteOutScoresData();
                    }
                    GameObject.Find("LobbyManager").GetComponent<ReadScores>().SetScores();
                    isFinished = true;
                    RpcRaceFinished();
                }
            }
        }
        else
        {
            //Debug.LogError("Missed Checkpoint");
        }
        //Debug.Log("Last Checkpoint: " + m_lastCheckpoint);
        //Debug.Log("Laps: " + +m_laps);
    }

    [ClientRpc]
    public void  RpcSetLaps(int laps)
    {
        m_laps = laps;
    }

    [Command]
    public void CmdIncreasePoints(int points)
    {
        score += points;
        RpcSetScore(score);
    }

    [ClientRpc]
    public void RpcSetScore(int points)
    {
        score = points;
    }

    private void IncrementAccelRate(bool accelup)
    {
        if (accelup)
        {
            if (m_AccelRate < 1)
            {
                m_AccelRate += 0.01f;
            }
            if (m_AccelRate > 1)
            {
                m_AccelRate = 1;
            }
        }
        else
        {
            if (m_AccelRate > 0.1f)
            {
                m_AccelRate -= 0.05f;
            }
            if (m_AccelRate < 0.1f)
            {
                m_AccelRate = 0.1f;
            }
        }

    }

    public bool IsMoving()
    {
        if (m_MovementInputValue > 0.5 || m_MovementInputValue < 0.5)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void SetInSpeedZone()
    {
        inSpeedZone = true;
    }

    public void SetOutSpeedZone()
    {
        inSpeedZone = false;
    }

    public bool IsRaceFinished()
    {
        return isFinished;
    }

    #endregion helpers
}
