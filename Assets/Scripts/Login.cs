using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

public class Login : MonoBehaviour {

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
}
