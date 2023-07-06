using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayersSpawner : MonoBehaviour
{

    public static PlayersSpawner instance;

    private void Awake()
    {
        instance = this;
    }

    public GameObject playerPrefab;
    private GameObject player;
    public GameObject deathEffect;
    public float responTime = 5f;

    // Start is called before the first frame update
    void Start()
    {
        if(PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        Transform spawnPoint = SpawnManager.instance.GetSpawnPoint();
        player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
    }
    public void Die(string damager)
    {


        UIController.instance.deathText.text = "Masak Di Bunuh Kau Sama " + damager;

        //PhotonNetwork.Destroy(player);

        //SpawnPlayer();

        MatchManager.instance.UpdateStatsSend(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

        if(player != null)
        {
            StartCoroutine(DieCo());
        }
    }

    public IEnumerator DieCo()
    {
        PhotonNetwork.Instantiate(deathEffect.name, player.transform.position, Quaternion.identity);
        PhotonNetwork.Destroy(player);
        UIController.instance.deathScreen.SetActive(true);

        yield return new WaitForSeconds(responTime);

        UIController.instance.deathScreen.SetActive(false);

        SpawnPlayer();
    }
}