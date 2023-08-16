using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    //public override void OnStartClient()
    //{
    //    base.OnStartClient();
    //    UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    //}
    private void Start()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }
}
