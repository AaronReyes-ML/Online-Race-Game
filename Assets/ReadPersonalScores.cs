using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Diagnostics;
using System.IO;

public class ReadPersonalScores : MonoBehaviour {


    public Text personalScores;

    // Use this for initialization
    void Start () {

	}

    public void SetPersonalScores()
    {
        StreamReader personalReader = new StreamReader("PlayerScores/" + Prototype.NetworkLobby.LobbyManager.loggedInName + ".txt");

        personalScores.text = "";

        for (int i = 0; i < 5; i++)
        {
            personalScores.text += personalReader.ReadLine();
        }

        personalReader.Close();
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
