using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{

    public static Launcher instance;

    private void Awake()
    {
        instance = this;
    }

    public GameObject loadingScreen;
    public TMP_Text loadingText;

    public GameObject menuButtons;

    public GameObject createRoomScreen;
    public TMP_InputField roomNameInput;

    public GameObject roomScreen;
    public TMP_Text roomNameText, playerNameLabel;

    private List<TMP_Text> allPlayersName = new List<TMP_Text>();

    public GameObject errorScreen;
    public TMP_Text errorText;

    public GameObject roomBrowserScreen;
    public RoomButton theRoomButton;

    private List<RoomButton> allRoomButton = new List<RoomButton>();

    public GameObject nameInputScreen;
    public TMP_InputField nameInput;
    private bool hasSetNick;

    public string levelToPlay;
    public GameObject startButton;

    public GameObject roomTestButton;


    // Start is called before the first frame update
    void Start()
    {
        CloseMenu();
        loadingScreen.SetActive(true);
        loadingText.text = "Sedang Memuat.....";
        PhotonNetwork.ConnectUsingSettings();

#if UNITY_EDITOR
        roomTestButton.SetActive(true);
#endif
    }

    void CloseMenu()
    {
        loadingScreen.SetActive(false);
        menuButtons.SetActive(false);
        createRoomScreen.SetActive(false);
        roomScreen.SetActive(false);
        errorScreen.SetActive(false);
        roomBrowserScreen.SetActive(false);
        nameInputScreen.SetActive(false);
    }

    public override void OnConnectedToMaster()
    { 

        PhotonNetwork.JoinLobby();

        PhotonNetwork.AutomaticallySyncScene = true;

        loadingText.text = "Memasuki Lobby...";
    }


    public override void OnJoinedLobby()
    {
        CloseMenu();
        menuButtons.SetActive(true);

        PhotonNetwork.NickName = Random.Range(0, 1000).ToString();

        if(!hasSetNick)
        {
            CloseMenu();
            nameInputScreen.SetActive(true);

            if(PlayerPrefs.HasKey("PlayerName"))
            {
                nameInput.text = PlayerPrefs.GetString("playerName");
            }
        }
        else
        {
            PhotonNetwork.NickName = PlayerPrefs.GetString("playerName");
        }
    }

    public void OpenRoomCreate()
    {
        CloseMenu();
        createRoomScreen.SetActive(true);
    }

    public void CreateRoom()
    {
        if(!string.IsNullOrEmpty(roomNameInput.text))
        {

            RoomOptions options = new RoomOptions();

            options.MaxPlayers = 8;

            PhotonNetwork.CreateRoom(roomNameInput.text, options);

            CloseMenu();
            loadingText.text = "Sedang Membuat Lobi...";
            loadingScreen.SetActive(true);
        }
    }

    public override void OnJoinedRoom()
    {
        CloseMenu();
        roomScreen.SetActive(true);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        ListAllPlayers();

        if(PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
    }

    private void ListAllPlayers()
    {
        foreach(TMP_Text player in allPlayersName)
        {
            Destroy(player.gameObject);
        }
        allPlayersName.Clear();

        Player[] players = PhotonNetwork.PlayerList;

        for(int i = 0; i < players.Length; i++)
        {
            TMP_Text newPlayerLabels = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
            newPlayerLabels.text = players[i].NickName;
            newPlayerLabels.gameObject.SetActive(true);

            allPlayersName.Add(newPlayerLabels);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TMP_Text newPlayerLabels = Instantiate(playerNameLabel, playerNameLabel.transform.parent);
        newPlayerLabels.text = newPlayer.NickName;
        newPlayerLabels.gameObject.SetActive(true);

        allPlayersName.Add(newPlayerLabels);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ListAllPlayers();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Gagal Membuat Lobi \n" + message;
        CloseMenu();
        errorScreen.SetActive(true);
    }

    public void CloseErrorScreen()
    {
        CloseMenu();
        menuButtons.SetActive(true);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        CloseMenu();
        loadingText.text = "Meninggalkan Lobi";
        loadingScreen.SetActive(true);
    }

    public override void OnLeftRoom()
    {
        CloseMenu();
        menuButtons.SetActive(true);
    }

    public void OpenRoomBrowser()
    {
        CloseMenu();
        roomBrowserScreen.SetActive(true);
    }
    public void CloseRoomBrowser()
    {
        CloseMenu();
        menuButtons.SetActive(true);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach(RoomButton rb in allRoomButton)
        {
            Destroy(rb.gameObject);
        }
        allRoomButton.Clear();

        theRoomButton.gameObject.SetActive(false);

        for(int i = 0; i < roomList.Count; i++)
        {
            if(roomList[i].PlayerCount != roomList[i].MaxPlayers && !roomList[i].RemovedFromList)
            {
                RoomButton newButton = Instantiate(theRoomButton, theRoomButton.transform.parent);
                newButton.SetButtonDetails(roomList[i]);
                newButton.gameObject.SetActive(true);

                allRoomButton.Add(newButton);

            }
        }
    }

    public void JoinRoom(RoomInfo inputInfo)
    {
        PhotonNetwork.JoinRoom(inputInfo.Name);

        CloseMenu();
        loadingText.text = "Memasuki Lobi..";
        loadingScreen.SetActive(true);
    }

    public void SetNickName()
    {
        if(!string.IsNullOrEmpty(nameInput.text))
        {
            PhotonNetwork.NickName = nameInput.text;

            PlayerPrefs.SetString("playerName", nameInput.text);

            CloseMenu();
            menuButtons.SetActive(true);

            hasSetNick = true;
        }
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(levelToPlay);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.SetActive(true);
        }
        else
        {
            startButton.SetActive(false);
        }
    }

    public void QuickJoin()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 8;

        PhotonNetwork.CreateRoom("Test", options);
        CloseMenu();
        loadingText.text = "Membuat Lobi";
        loadingScreen.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
