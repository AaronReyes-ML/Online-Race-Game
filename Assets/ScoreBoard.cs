using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Linq;
using System.IO;

public class ScoreBoard : NetworkBehaviour {

    public Text scoreboardText;

    public List<CarMovement> allCars;

    public TextAsset scoresFile;
    public TextAsset timesFile;

    private string scoresPath = "topscores.txt";
    private string timesPath = "toptimes.txt";

    List<NameVal> fileScores = new List<NameVal>();
    List<NameVal> fileTimes = new List<NameVal>();

    List<NameVal> gameScores = new List<NameVal>();
    List<NameVal> gameTimes = new List<NameVal>();

    Dictionary<string, List<int>> PlayerScoreData = new Dictionary<string, List<int>>();

    public bool Winner = false;
    public string winnerName = "";

	// Use this for initialization
	void Start () {
        scoreboardText.enabled = false;
        ReadInScoresData();
	}

    [Command]
    public void CmdUpdateScoreBoard()
    {

        allCars = DeathRaceManager.GetCars();
        SortResults();

        scoreboardText.text = "Scoreboard\n";

        for (int i = 0; i < allCars.Count;i++)
        {
            scoreboardText.text += "Player: " + allCars[i].PlayerName + " " +
                "Score: " + allCars[i].score + " ";
            if (allCars[i].raceFinishTime != 9999999)
            {
                scoreboardText.text += "Time: " + allCars[i].raceFinishTime + "\n";
            }
            else
            {
                scoreboardText.text += "Time: " + "NOT FINISHED\n";
            }
        }

        scoreboardText.text += "Winner: " + allCars[0].PlayerName;

        RpcUpdateScoreBoardText(scoreboardText.text);
    }

    public void SortResults()
    {
        allCars = (allCars.OrderBy(s => s.score).ThenByDescending(t => t.raceFinishTime)).ToList<CarMovement>();
        allCars.Reverse();
    }

    public void ReadInScoresData()
    {
        StreamReader scoresReader = new StreamReader(scoresPath);

        for (int i = 0; i < 5; i++)
        {
            NameVal temp = new NameVal();
            temp.name = scoresReader.ReadLine();
            temp.val = int.Parse(scoresReader.ReadLine());
            fileScores.Add(temp);
        }

        scoresReader.Close();

        StreamReader timesReader = new StreamReader(timesPath);

        for (int i = 0; i < 5; i++)
        {
            NameVal temp = new NameVal();
            temp.name = timesReader.ReadLine();
            temp.val = int.Parse(timesReader.ReadLine());
            fileTimes.Add(temp);
        }

        timesReader.Close();
    }

    [Command]
    public void CmdWriteOutScoresData()
    {
        if (!isServer)
            return;

        gameScores.Clear();
        gameTimes.Clear();

        foreach (CarMovement car in DeathRaceManager.GetCars())
        {
            NameVal temp = new NameVal();
            temp.name = car.PlayerName;
            temp.val = car.score;
            gameScores.Add(temp);
        }

        foreach (CarMovement car in DeathRaceManager.GetCars())
        {
            NameVal temp = new NameVal();
            temp.name = car.PlayerName;
            temp.val = (int)car.raceFinishTime;
            gameTimes.Add(temp);
        }

        foreach (NameVal pair in gameScores)
        {
            fileScores.Add(pair);
        }

        foreach (NameVal pair in gameTimes)
        {
            fileTimes.Add(pair);
        }

        fileScores = (fileScores.OrderBy(s => s.val)).ToList<NameVal>();
        fileScores.Reverse();

        fileTimes = (fileTimes.OrderBy(t => t.val)).ToList<NameVal>();

        List<NameVal> tempFileScores = new List<NameVal>(5);
        List<NameVal> tempFileTimes = new List<NameVal>(5);

        for (int i = 0; i < 5; i++)
        {
            tempFileScores.Add(fileScores[i]);
            tempFileTimes.Add(fileTimes[i]);
        }

        fileScores = tempFileScores;
        fileTimes = tempFileTimes;

        StreamWriter scoresWriter = new StreamWriter(scoresPath, false);

        foreach (NameVal pair in fileScores)
        {
            scoresWriter.WriteLine(pair.name);
            scoresWriter.WriteLine(pair.val);
        }

        scoresWriter.Close();

        StreamWriter timesWriter = new StreamWriter(timesPath, false);

        foreach (NameVal pair in fileTimes)
        {
            timesWriter.WriteLine(pair.name);
            timesWriter.WriteLine(pair.val);
        }

        timesWriter.Close();
    }

    [Command]
    public void CmdUpdateIndividualScores()
    {
        if (!isServer)
        {
            return;
        }

        //Debug.Log("Attempting to update individual scores");
        PlayerScoreData.Clear();

        foreach (CarMovement car in DeathRaceManager.GetCars())
        {
            InitializeScoreFile(car.PlayerName);

            StreamReader carReader = new StreamReader("PlayerScores/" + car.PlayerName + car.carType + ".txt");

            //Debug.Log("accessing file");

            List<int> temp = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    temp.Add(int.Parse(carReader.ReadLine()));
                }
                catch
                {
                    break;
                }
            }
            temp.Add(car.score);
            temp.Sort();
            temp.Reverse();

            PlayerScoreData.Add(car.PlayerName, temp);
            carReader.Close();
        }
    }

    [Command]
    public void CmdWriteOutIndividualData()
    {
        if (!isServer)
        {
            return;
        }

        CmdUpdateIndividualScores();

        foreach (CarMovement car in DeathRaceManager.GetCars())
        {
            StreamWriter carWriter = new StreamWriter("PlayerScores/" + car.PlayerName + car.carType + ".txt");

            foreach (int score in PlayerScoreData[car.PlayerName])
            {
                carWriter.WriteLine(score);
            }

            carWriter.Close();
        }
    }

    [ClientRpc]
    public void RpcUpdateScoreBoardText(string text)
    {
        scoreboardText.text = text;
    }

    [ClientRpc]
    public void RpcSetWinner(bool winner)
    {
        Winner = winner;
    }

    public void InitializeScoreFile(string name)
    {
        if (!File.Exists("PlayerScores/" + name + "0.txt"))
        {
            StreamWriter sw = new StreamWriter("PlayerScores/" + Prototype.NetworkLobby.LobbyManager.loggedInName + "0.txt");
            for (int i = 0; i < 10; i++)
            {
                sw.WriteLine(0);
            }
            sw.Close();
        }
        if (!File.Exists("PlayerScores/" + name + "1.txt"))
        {
            StreamWriter sw = new StreamWriter("PlayerScores/" + Prototype.NetworkLobby.LobbyManager.loggedInName + "1.txt");
            for (int i = 0; i < 10; i++)
            {
                sw.WriteLine(0);
            }
            sw.Close();
        }
        if (!File.Exists("PlayerScores/" + name + "2.txt"))
        {
            StreamWriter sw = new StreamWriter("PlayerScores/" + Prototype.NetworkLobby.LobbyManager.loggedInName + "2.txt");
            for (int i = 0; i < 10; i++)
            {
                sw.WriteLine(0);
            }
            sw.Close();
        }
    }
}


public class NameVal
{
    public string name;
    public int val;
}
