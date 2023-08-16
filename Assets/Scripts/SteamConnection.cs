using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using FishySteamworks;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class SteamConnection : MonoBehaviour
{
    NetworkManager manager;
    [SerializeField] TextMeshProUGUI text;

    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> JoinRequest;
    protected Callback<LobbyEnter_t> LobbyEntered;
    protected Callback<LobbyDataUpdate_t> LobbyDataUpdated;

    public ulong CurrentLobbyId;
    private const string HostAddressKey = "HostAddress";
    bool doCreateClient;

    private void Start()
    {
        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        JoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        LobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);

        manager = Object.FindObjectOfType<NetworkManager>();

        //manager.ClientManager.OnClientConnectionState += ConnectToGame;

    }
    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, manager.gameObject.GetComponent<FishySteamworks.FishySteamworks>().GetMaximumClients());
    }


    void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK) return;

        Debug.Log("Lobby created successfully");
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());

        Debug.Log(SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey)); text.text = callback.m_ulSteamIDLobby.ToString();

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "name", SteamFriends.GetPersonaName() + "'s lobby");
        manager.ServerManager.StartConnection();
        manager.ClientManager.StartConnection();
        //already joined lobby as host
    }

    void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Request to join lobby");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);

    }
    void OnLobbyEntered(LobbyEnter_t callback)
    {
        Debug.Log("Joined lobby");
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        CurrentLobbyId = callback.m_ulSteamIDLobby;
        SteamMatchmaking.RequestLobbyData(new CSteamID(callback.m_ulSteamIDLobby)); //return on request accepted, not on data received
        doCreateClient = true;
    }
    void OnLobbyDataUpdated(LobbyDataUpdate_t callback)
    {
        Debug.Log("Data updated");

        if (manager.IsOffline && doCreateClient)    //data update was called 3 times before client is started, adding bool check
        {
            doCreateClient = false;
            manager.gameObject.GetComponent<FishySteamworks.FishySteamworks>().SetClientAddress(SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey));

            Debug.Log(SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey)); text.text = callback.m_ulSteamIDLobby.ToString();

            manager.ClientManager.StartConnection();
        }
    }

    //void ConnectToGame(ClientConnectionStateArgs args)
    //{
    //    if (args.ConnectionState == LocalConnectionState.Started)
    //    //if (network.ClientManager.Connection.IsActive)
    //    {
    //        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    //    }
    //}
}
