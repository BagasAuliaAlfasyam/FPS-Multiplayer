using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;


public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{

    public static MatchManager instance;

    private void Awake()
    {
        instance = this;
    }

    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayer,
        UpdateStat
    }

    public List<PlayerInfo> allplayers = new List<PlayerInfo>();
    private int index;

    private List<LeaderboardPlayer> lBoardPlayers = new List<LeaderboardPlayer>();

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            NewPlayerSend(PhotonNetwork.NickName);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (UIController.instance.Leaderboard.activeInHierarchy)
            {
                UIController.instance.Leaderboard.SetActive(false);
            }
            else
            {
                ShowLeaderboard();
            }
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code < 200)
        {
            EventCodes theEvent = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            //Debug.Log("Event Yang Disampaikan " + theEvent);

            switch (theEvent)
            {
                case EventCodes.NewPlayer:
                    NewPlayerReceive(data);
                    break;

                case EventCodes.ListPlayer:
                    ListPlayersReceive(data);
                    break;

                case EventCodes.UpdateStat:
                    UpdateStatsReceive(data);
                    break;
            }
        }
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void NewPlayerSend(string username)
    {
        object[] package = new object[4];
        package[0] = username;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;


        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
            );
    }
    public void NewPlayerReceive(object[] dataReceived)
    {
        PlayerInfo player = new PlayerInfo((string)dataReceived[0], (int)dataReceived[1], (int)dataReceived[2], (int)dataReceived[3]);

        allplayers.Add(player);

        ListPlayersSend();

    }

    public void ListPlayersSend()
    {
        object[] package = new object[allplayers.Count];

        for (int i = 0; i < allplayers.Count; i++)
        {
            object[] piece = new object[4];
            piece[0] = allplayers[i].name;
            piece[1] = allplayers[i].actor;
            piece[2] = allplayers[i].kills;
            piece[3] = allplayers[i].death;

            package[i] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }
    public void ListPlayersReceive(object[] dataReceived)
    {
        allplayers.Clear();

        for (int i = 0; i < dataReceived.Length; i++)
        {
            object[] piece = (object[])dataReceived[i];

            PlayerInfo player = new PlayerInfo(
                (string)piece[0],
                (int)piece[1],
                (int)piece[2],
                (int)piece[3]
                );

            allplayers.Add(player);

            if (PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
            {
                index = i;
            }
            if(UIController.instance.Leaderboard.activeInHierarchy)
            {
                ShowLeaderboard();
            }
        }
    }

    public void UpdateStatsSend(int actorSending, int statToUpdate, int amoutToChange)
    {
        object[] package = new object[] { actorSending, statToUpdate, amoutToChange };
        PhotonNetwork.RaiseEvent(
    (byte)EventCodes.UpdateStat,
    package,
    new RaiseEventOptions { Receivers = ReceiverGroup.All },
    new SendOptions { Reliability = true }
    );

    }

    public void UpdateStatsReceive(object[] dataReceived)
    {
        int actor = (int)dataReceived[0];
        int statType = (int)dataReceived[1];
        int amount = (int)dataReceived[2];

        for (int i = 0; i < allplayers.Count; i++)
        {
            if (allplayers[i].actor == actor)
            {
                switch (statType)
                {
                    case 0: //kills
                        allplayers[i].kills += amount;
                        Debug.Log("Player " + allplayers[i].name + " : kills" + allplayers[i].kills);
                        break;

                    case 1: //death
                        allplayers[i].death += amount;
                        Debug.Log("Player " + allplayers[i].name + " : death" + allplayers[i].death);
                        break;
                }

                if (i == index)
                {
                    UpdateStatDisplay();
                }

                break;
            }
        }
    }

    public void UpdateStatDisplay()
    {
        if (allplayers.Count > index)
        {
            UIController.instance.KillsText.text = "Kills : " + allplayers[index].kills;
            UIController.instance.deathsText.text = "Death : " + allplayers[index].death;
        }
        else
        {
            UIController.instance.KillsText.text = "Kills : 0 ";
            UIController.instance.deathsText.text = "Death : 0 ";
        }
    }

    void ShowLeaderboard()
    {
        UIController.instance.Leaderboard.SetActive(true);

        foreach (LeaderboardPlayer lp in lBoardPlayers)
        {
            Destroy(lp.gameObject);

        }
        lBoardPlayers.Clear();

        UIController.instance.leaderboardPlayerDisplay.gameObject.SetActive(false);

        List<PlayerInfo> sorted = SortPlayer(allplayers);

        foreach (PlayerInfo player in sorted)
        {
            LeaderboardPlayer newPlayerDisplay = Instantiate(UIController.instance.leaderboardPlayerDisplay, UIController.instance.leaderboardPlayerDisplay.transform.parent);

            newPlayerDisplay.SetDetails(player.name, player.kills, player.death);

            newPlayerDisplay.gameObject.SetActive(true);

            lBoardPlayers.Add(newPlayerDisplay);
        }
    }

    private List<PlayerInfo> SortPlayer(List<PlayerInfo> players)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();


        while (sorted.Count < players.Count)
        {
            int highest = -1;
            PlayerInfo selectionPlayer = players[0];

            foreach (PlayerInfo player in players)
            {
                if (!sorted.Contains(player))
                {
                    if (player.kills > highest)
                    {
                        selectionPlayer = player;
                        highest = player.kills;
                    }
                }
            }
            sorted.Add(selectionPlayer);
        }
        return sorted;
    }

}

[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int actor, kills, death;

    public PlayerInfo(string _name, int _actor, int _kills, int _death)
    {
        name = _name;
        actor = _actor;
        kills = _kills;
        death = _death;
    }
}
