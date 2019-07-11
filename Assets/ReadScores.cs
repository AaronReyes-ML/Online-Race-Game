using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Diagnostics;
using System.IO;

public class ReadScores : MonoBehaviour
{

    private string scoresPath = "topscores.txt";
    private string timesPath = "toptimes.txt";

    private string userScores = "userScores.txt";

    public Text scoresText;
    public Text timesText;

    // Use this for initialization
    void Start()
    {
        SetScores();
    }

    public void SetScores()
    {
        StreamReader scoresReader = new StreamReader(scoresPath);
        string scoresTextString = "Top Scores: \n";

        for (int i = 0; i < 5; i++)
        {
            for (int k = 0; k < 2; k++)
            {
                scoresTextString += scoresReader.ReadLine() + " ".PadRight(5);
            }
            scoresTextString += "\n";
        }

        StreamReader timesReader = new StreamReader(timesPath);
        string timesTextString = "Top Times: \n";

        for (int i = 0;  i < 5; i++)
        {
            for (int k = 0; k < 2; k++)
            {
                timesTextString += timesReader.ReadLine() + " ".PadRight(5);
            }
            timesTextString += "\n";
        }

        scoresReader.Close();
        timesReader.Close();

        scoresText.text = scoresTextString;
        timesText.text = timesTextString;
    }


}
