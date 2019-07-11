using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    private GameObject networkManager;
    private ConnectedPlayers connectedPlayers;

    public int m_NumRoundsToWin = 5;        
    public float m_StartDelay = 3f;         
    public float m_EndDelay = 3f;           
    public Text m_MessageText;              
    public GameObject m_CarPrefab;
    public TankManager[] m_Cars;
    public Camera m_Camera;


    private SmoothFollowCSharp m_Target;
    private int m_RoundNumber;              
    private WaitForSeconds m_StartWait;     
    private WaitForSeconds m_EndWait;       
    private TankManager m_RoundWinner;
    private TankManager m_GameWinner;       

    private IEnumerator Start()
    {
        networkManager = GameObject.Find("NetworkManager");
        connectedPlayers = networkManager.GetComponent<ConnectedPlayers>();

        m_StartWait = new WaitForSeconds(m_StartDelay);
        m_EndWait = new WaitForSeconds(m_EndDelay);

        //yield return StartCoroutine(WaitForPlayers(2));
        return null;
    }

    private void SpawnCars()
    {
        for (int i = 0; i < m_Cars.Length; i++)
        {
            m_Cars[i].m_Instance =
                Instantiate(m_CarPrefab, m_Cars[i].m_SpawnPoint.position, m_Cars[i].m_SpawnPoint.rotation) as GameObject;
            m_Cars[i].m_PlayerNumber = i + 1;
            m_Cars[i].Setup();
        }
    }

    private IEnumerator WaitForPlayers(int playersToWaitFor)
    {
        while (!PlayersConnected(playersToWaitFor))
        {
            yield return StartCoroutine(GameLoop());
        }
    }

    private bool PlayersConnected(int playersToWaitFor)
    {
        if (connectedPlayers.GetPlayerCount() != playersToWaitFor)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private IEnumerator GameLoop()
    {
        yield return StartCoroutine(RoundStarting());
        yield return StartCoroutine(RoundPlaying());
        yield return StartCoroutine(RoundEnding());

        if (m_GameWinner != null)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            StartCoroutine(GameLoop());
        }
    }


    private IEnumerator RoundStarting()
    {
        ResetAllTanks();
        DisableTankControl();
        m_RoundNumber++;
        m_MessageText.text = "RACE " + m_RoundNumber + " START";
        
        yield return m_StartWait;
    }


    private IEnumerator RoundPlaying()
    {
        EnableTankControl();
        m_MessageText.text = "";

        while(!OneTankLeft() && !OneCarFinished())
        {
            yield return null;
        }

    }


    private IEnumerator RoundEnding()
    {
        DisableTankControl();
        m_RoundWinner = null;
        m_RoundWinner = GetRoundWinner();

        if (m_RoundWinner != null)
        {
            m_RoundWinner.m_Wins++;
        }

        m_GameWinner = GetGameWinner();

        m_MessageText.text = EndMessage();

        yield return m_EndWait;
    }


    private bool OneTankLeft()
    {
        int numTanksLeft = 0;

        for (int i = 0; i < m_Cars.Length; i++)
        {
            if (m_Cars[i].m_Instance.activeSelf)
                numTanksLeft++;
        }

        return numTanksLeft <= 1;
    }

    private bool OneCarFinished()
    {
        int numCarsFinished = 0;

        for (int i = 0; i < m_Cars.Length; i++)
        {
            Rigidbody car = m_Cars[i].m_Instance.GetComponent<Rigidbody>();
            CarMovement carMovement = car.GetComponent<CarMovement>();
            if (carMovement.IsRaceFinished())
            {
                numCarsFinished++;
                Debug.LogError(numCarsFinished);
            }
        }

        return numCarsFinished >= 1;
    }


    private TankManager GetRoundWinner()
    {
        for (int i = 0; i < m_Cars.Length; i++)
        {
            if (m_Cars[i].m_Instance.activeSelf)
                return m_Cars[i];
        }

        return null;
    }


    private TankManager GetGameWinner()
    {
        for (int i = 0; i < m_Cars.Length; i++)
        {
            if (m_Cars[i].m_Wins == m_NumRoundsToWin)
                return m_Cars[i];
        }

        return null;
    }


    private string EndMessage()
    {
        string message = "DRAW!";

        if (m_RoundWinner != null)
            message = m_RoundWinner.m_ColoredPlayerText + " WINS THE RACE!";

        message += "\n\n\n\n";

        for (int i = 0; i < m_Cars.Length; i++)
        {
            message += m_Cars[i].m_ColoredPlayerText + ": " + m_Cars[i].m_Wins + " WINS\n";
        }

        if (m_GameWinner != null)
            message = m_GameWinner.m_ColoredPlayerText + " WINS THE RACE!";

        return message;
    }


    private void ResetAllTanks()
    {
        for (int i = 0; i < m_Cars.Length; i++)
        {
            m_Cars[i].Reset();
        }
    }


    private void EnableTankControl()
    {
        for (int i = 0; i < m_Cars.Length; i++)
        {
            m_Cars[i].EnableControl();
        }
    }


    private void DisableTankControl()
    {
        for (int i = 0; i < m_Cars.Length; i++)
        {
            m_Cars[i].DisableControl();
        }
    }
}