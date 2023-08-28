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
using System.Text;
using System;
using System.Linq;

public class SteamConnection : MonoBehaviour
{
    NetworkManager networkManager;
    [SerializeField] TextMeshProUGUI screenText;  //temp text for showing host SteamID
    [SerializeField] TMP_InputField selectedIDDisplay;

    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> JoinRequested;
    protected Callback<LobbyEnter_t> LobbyEnter;
    protected Callback<LobbyDataUpdate_t> LobbyDataUpdate;
    protected Callback<LobbyMatchList_t> LobbyMatchList;
    protected Callback<LobbyChatUpdate_t> LobbyChatUpdate;
    protected Callback<LobbyChatMsg_t> LobbyChatMsg;

    const string gameIdkey = "GameID";
    [SerializeField] const string gameIdValue = "GundamOnline2_maybe";
    public ulong CurrentLobbyId;
    public CSteamID CurrentLobbyOwnerId;
    private const string HostIdKey = "HostID";
    [SerializeField] const int lobbyListMaxCount = 50;
    //byte[] hostDisconnectMsg = ASCIIEncoding.ASCII.GetBytes("disconnectAll");
    bool doCreateClient;
    bool doLobbySearch;

    List<CSteamID> lobbyListSteamID = new List<CSteamID>();

    //Test
    int selectedLobbyIndex = 0;

    private void Start()
    {
        networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>();

        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        LobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
        LobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
        LobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyListUpdate);
        JoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest); //for joining from Steam UI
        LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        LobbyChatMsg = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMsg);

        InvokeRepeating("GetLobbyList", 0, 5);
    }
    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, networkManager.gameObject.GetComponent<FishySteamworks.FishySteamworks>().GetMaximumClients());
        Debug.Log(networkManager.gameObject.GetComponent<FishySteamworks.FishySteamworks>().GetMaximumClients());
    }
    public void HostFriendsLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.gameObject.GetComponent<FishySteamworks.FishySteamworks>().GetMaximumClients());
    }
    public void GetLobbyList()
    {
        SteamMatchmaking.AddRequestLobbyListStringFilter(gameIdkey, gameIdValue, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();
        doLobbySearch = true;
    }
    public void JoinLobby(CSteamID steamIDLobby)
    {
        SteamMatchmaking.JoinLobby(steamIDLobby);
    }
    public void DisconnectLobby()
    {
        SteamMatchmaking.LeaveLobby( new CSteamID(CurrentLobbyId) );
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");

        if (networkManager.IsClient) networkManager.ClientManager.StopConnection();
        if (networkManager.IsServer)
        {
            //SteamMatchmaking.SendLobbyChatMsg(new CSteamID(CurrentLobbyId), hostDisconnectMsg, hostDisconnectMsg.Length + 1);
            networkManager.ServerManager.StopConnection(true);
        }

        if (networkManager.IsOffline)  Debug.Log("Disconnected lobby");
        screenText.text = "Disconnected lobby";
    }

    // on callbacks ===========================================================

    void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK) return;
        Debug.Log("Lobby created successfully");

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), gameIdkey, gameIdValue);

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostIdKey, SteamUser.GetSteamID().ToString());
        screenText.text = callback.m_ulSteamIDLobby.ToString();

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "name", SteamFriends.GetPersonaName() + "'s lobby");

        networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection();
        //already joined lobby as host
    }
    void OnJoinRequest(GameLobbyJoinRequested_t callback)   //for joining from Steam UI
    {
        Debug.Log("Request to join lobby from Steam UI");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }
    void OnLobbyEnter(LobbyEnter_t callback)
    {
        if (callback.m_EChatRoomEnterResponse != ((uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess))
        {
            Debug.Log( callback.m_EChatRoomEnterResponse.ToString() );
            screenText.text = callback.m_EChatRoomEnterResponse.ToString();
            return;
        };

        Debug.Log("Joined lobby");
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        CurrentLobbyId = callback.m_ulSteamIDLobby;
        CurrentLobbyOwnerId = SteamMatchmaking.GetLobbyOwner((CSteamID)CurrentLobbyId);
        SteamMatchmaking.RequestLobbyData(new CSteamID(callback.m_ulSteamIDLobby)); //return on request accepted, not on data received

        if (networkManager.IsOffline) doCreateClient = true;
    }
    void OnLobbyListUpdate(LobbyMatchList_t callback)
    {
        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            SteamMatchmaking.RequestLobbyData( SteamMatchmaking.GetLobbyByIndex(i) );
        }
    }
    void OnLobbyDataUpdated(LobbyDataUpdate_t callback)
    {
        Debug.Log("Data updated");

        if (doCreateClient)    //data update was called 3 times before client is started, adding bool check; for non-host client only
        {
            doCreateClient = false;
            networkManager.gameObject.GetComponent<FishySteamworks.FishySteamworks>().SetClientAddress(SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostIdKey));

            Debug.Log(SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostIdKey)); screenText.text = callback.m_ulSteamIDLobby.ToString();

            networkManager.ClientManager.StartConnection();
        }

        if (doLobbySearch)
        {
            lobbyListSteamID.Clear();

            CSteamID ID = new CSteamID(callback.m_ulSteamIDLobby);  Debug.Log(ID);
            if ( SteamMatchmaking.GetLobbyData(ID , gameIdkey) == gameIdValue )
            {
                lobbyListSteamID.Add(ID);
                if (lobbyListSteamID.Count >= lobbyListMaxCount)
                {
                    Debug.LogWarning("OK" + lobbyListSteamID);
                }
            }
        };
    }
    void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        Debug.Log(SteamMatchmaking.GetLobbyOwner((CSteamID)CurrentLobbyId));
        if ((CSteamID)callback.m_ulSteamIDUserChanged == CurrentLobbyOwnerId)  //Owner state changed; can't use GetLobbyOwner() here if not wanting auto owner transfer
        {
            // left or leaving / disconnected without leaving lobby
            if (callback.m_rgfChatMemberStateChange == (uint)EChatMemberStateChange.k_EChatMemberStateChangeLeft || callback.m_rgfChatMemberStateChange == (uint)EChatMemberStateChange.k_EChatMemberStateChangeDisconnected)
            {
                DisconnectLobby();
            }
        }
    }
    void OnLobbyChatMsg(LobbyChatMsg_t callback)
    {

        //byte[] msgBuffer = new byte[4000];  Debug.Log("pass 1");
        //CSteamID steamUser; Debug.Log("pass 2");
        //EChatEntryType chatType;    Debug.Log("pass 3");
        //int msgSize = SteamMatchmaking.GetLobbyChatEntry( (CSteamID)callback.m_ulSteamIDLobby, (int)callback.m_iChatID, out steamUser, msgBuffer, 4000, out chatType); Debug.Log("pass 4");//returns steamUser & chatType
        //System.Array.Resize(ref msgBuffer, msgSize - 1);   Debug.Log("pass 5");    // 1 byte is reserved for null terminator
        //Debug.Log( BitConverter.ToString(msgBuffer) + " " + BitConverter.ToString(hostDisconnectMsg) ); Debug.Log("pass 6");
        //if (msgBuffer.SequenceEqual(hostDisconnectMsg)) Debug.Log("pass 7");
        //{
        //    DisconnectLobby();  Debug.Log("pass 8");
        //}
    }
    //Test
    public void JoinLobby()
    {
        SteamMatchmaking.JoinLobby(lobbyListSteamID[selectedLobbyIndex]);
    }
    public void ChangeSelectedLobby()
    {
        if (selectedLobbyIndex + 1 >= lobbyListSteamID.Count) selectedLobbyIndex = 0;
        else selectedLobbyIndex++;

        if (lobbyListSteamID.Count == 0) return;
        selectedIDDisplay.text = lobbyListSteamID[selectedLobbyIndex].ToString();
    }
}
