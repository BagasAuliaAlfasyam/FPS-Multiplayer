using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    public Transform viewPoint;
    public float mouseSensitivity = 1f;
    private float verticalRotStrore;
    private Vector2 mouseInput;

    public bool invertLook;

    public float moveSpeed = 5f, runSpeed = 8f;
    private float activeMoveSpeed;
    private Vector3 moveDir, movement;


    public CharacterController charCon;
    private Camera cam;

    public float jumpForce = 12f, gravityMod = 2.5f;

    public Transform groundCheckPoint;
    private bool isGrounded;
    public LayerMask groundLayers;

    public GameObject bulletImpact;
    //public float timeBeetwenShots = .1f;
    private float shotCounter;
    public float muzzleDisplayTime;
    private float muzzleCounter;

    public float maxHeat = 10f, /*heatPerShoot = 1f, */ coolRate = 4f, overHeatCoolRate = 5f;
    private float heatCounter;
    private bool overHeated;

    public Gun[] allGuns;
    private int selectedGun;

    public GameObject playerHitImpact;

    public int maxHealth = 100;
    private int currentHealth;

    public Animator anim;
    public GameObject playerModel;

    public Transform modelGunPoint, gunHolder;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        cam = Camera.main;
        UIController.instance.weaponTempSlider.maxValue = maxHeat;

        //SwitchGun();
        photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        currentHealth = maxHealth;


        if(photonView.IsMine)
        {
            playerModel.SetActive(false);
            UIController.instance.healthSlider.maxValue = maxHealth;
            UIController.instance.healthSlider.value = currentHealth;
        }
        else
        {
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }
        //Transform newTrans = SpawnManager.instance.GetSpawnPoint();
        //transform.position = newTrans.position;
        //transform.rotation = newTrans.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {
            LookingAround();
            Movement();
            GetShoot();
            CursorMode();
            ChangeWeapon();
        }
    }

    private void ChangeWeapon()
    {
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            selectedGun++;

            if (selectedGun >= allGuns.Length)
            {
                selectedGun = 0;
            }
            //SwitchGun();
            photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            selectedGun--;
            if (selectedGun < 0)
            {
                selectedGun = allGuns.Length - 1;
            }
            //SwitchGun();
            photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        }

        for (int i = 0; i < allGuns.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                selectedGun = i;
                //SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);

            }
        }
    }

    void SwitchGun()
    {
        foreach (Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);
        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }

    [PunRPC]
    public void SetGun(int gunToSwitchTo)
    {
        if(gunToSwitchTo < allGuns.Length)
        {
            selectedGun = gunToSwitchTo;
            SwitchGun();
        }
    }

    [PunRPC]
    public void DealDamage(string damager, int damageAmount, int actor)
    {
        TakeDamage(damager, damageAmount, actor);
    }

    public void TakeDamage(string damager, int damageAmount, int actor)
    {
        if (photonView.IsMine)
        {
            //Debug.Log(photonView.Owner.NickName + " Kenak Tembak Aku " + damager);
            currentHealth -= damageAmount;
            if (currentHealth <= 0)
            {
                currentHealth = 0;
                PlayersSpawner.instance.Die(damager);

                MatchManager.instance.UpdateStatsSend(actor, 0, 1);
            }

            UIController.instance.healthSlider.value = currentHealth;
        }
    }

    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            cam.transform.rotation = viewPoint.rotation;
            cam.transform.position = viewPoint.position;
        }
    }

    #region cursor
    private void CursorMode()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Cursor.lockState == CursorLockMode.None)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
    #endregion

    #region Menembak
    private void GetShoot()
    {
        if (allGuns[selectedGun].muzzleFlash.activeInHierarchy)
        {
            muzzleCounter -= Time.deltaTime;

            if (muzzleCounter < 0)
            {
                allGuns[selectedGun].muzzleFlash.SetActive(false);
            }
        }

        if (!overHeated)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }
            if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
            {
                shotCounter -= Time.deltaTime;
                if (shotCounter <= 0)
                {
                    Shoot();
                }
            }
            heatCounter -= coolRate * Time.deltaTime;
        }
        else
        {
            heatCounter -= overHeatCoolRate * Time.deltaTime;
            if (heatCounter <= 0)
            {
                overHeated = false;

                UIController.instance.overheatedMessage.gameObject.SetActive(false);

            }
        }

        if (heatCounter < 0)
        {
            heatCounter = 0f;
        }
        UIController.instance.weaponTempSlider.value = heatCounter;
    }

    private void Shoot()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = cam.transform.position;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            //Debug.Log("We Hit " + hit.collider.gameObject.name);

            if (hit.collider.gameObject.tag == "Player")
            {
                Debug.Log("Hit " + hit.collider.gameObject.GetPhotonView().Owner.NickName);
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);
                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[selectedGun].shootDamage, PhotonNetwork.LocalPlayer.ActorNumber);

            }
            else
            {
                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(bulletImpactObject, 10f);
            }
        }

        shotCounter = allGuns[selectedGun].timeBetweenShots;

        heatCounter += allGuns[selectedGun].heatPerShot;

        if (heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;

            overHeated = true;

            UIController.instance.overheatedMessage.gameObject.SetActive(true);
        }
        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;
    }
    #endregion

    #region Pergerakan/Movement
    public void Movement()
    {
        moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        if (Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed = runSpeed;
        }
        else
        {
            activeMoveSpeed = moveSpeed;
        }


        float yVel = movement.y;
        movement = ((transform.forward * moveDir.z) + (transform.right * moveDir.x)).normalized * activeMoveSpeed;
        movement.y = yVel;

        if (charCon.isGrounded)
        {
            movement.y = 0f;
        }

        isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            movement.y = jumpForce;
        }

        movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

        charCon.Move(movement * Time.deltaTime);

        anim.SetBool("grounded", isGrounded);
        anim.SetFloat("speed", moveDir.magnitude);
    }
    #endregion

    #region Melihat Sekitar
    private void LookingAround()
    {
        //Melihat Sekitar
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);
        verticalRotStrore += mouseInput.y;
        verticalRotStrore = Mathf.Clamp(verticalRotStrore, -60f, 60f);

        if (invertLook)
        {
            viewPoint.rotation = Quaternion.Euler(Mathf.Clamp(verticalRotStrore, -60, 60), viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        }
        else
        {
            viewPoint.rotation = Quaternion.Euler(Mathf.Clamp(-verticalRotStrore, -60, 60), viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
        }
    }
    #endregion
}
