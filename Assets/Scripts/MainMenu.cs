using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using UnityEngine.Audio;
using UnityEngine.UI;
using Assets.Scripts;

public class MainMenu : MonoBehaviourPunCallbacks
{
    public static MainMenu Instance;
    public Animator SceneTransition;
    [Header("Find Room Menu")]
    public Transform roomListContent;
    public GameObject roomItemPrefab;
    [Header("Room Menu")]
    public TextMeshProUGUI roomName;
    public Transform playerListContent;
    public GameObject playerItemPrefab;
    public GameObject hostRoomControlPanel;
    public GameObject nonhostRoomControlPanel;
    [Header("Error Menu")]
    public TextMeshProUGUI errorText;
    [Header("Loading Menu")]
    public TextMeshProUGUI loadingText;
    [Header("Join Room with Pass Menu")]
    public TMP_InputField createRoomPassword;
    [Header("Create Room Menu")]
    public TMP_InputField inputRoomPass;
    public TMP_InputField createRoomName;
    [Header("Host ControlPanel")]
    public TMP_Dropdown mapSelect;
    public TMP_Dropdown difficultySelect;
    public TextMeshProUGUI startButtonText;
    [Header("Non-Host ControlPanel")]
    public TextMeshProUGUI mapText;
    public TextMeshProUGUI difficultyText;
    public TextMeshProUGUI countDownText;
    [Header("Loadout")]
    public Transform loadoutTransform;
    public GameObject minionDropdownPrefab;
    public TMP_Dropdown primary;
    public TMP_Dropdown secondary;
    [Header("Settings")]
    public AudioMixer mixer;
    public Slider Master;
    public Slider Music;
    public Slider SFX;
    public TMP_InputField nickNameInput;
    public TextMeshProUGUI nameMessage;
    private string NickName;
    private MenuManager menuManager;
    public RoomInfo selectedRoom = null;

    bool RoomStarting = false;
    bool InTransition = false;

    private void Awake()
    {
        menuManager = gameObject.GetComponent<MenuManager>();
        Instance = this;
    }

    void Start()
    {
        NickName = PlayerPrefs.GetString("NickName", "Player " + string.Format("{0:0000}", Random.Range(1, 9999)));
        PlayerPrefs.SetString("NickName", NickName);
        Master.minValue = 0.0001f;
        Master.value = PlayerPrefs.GetFloat("MasterVol", 1);
        mixer.SetFloat("MasterVol", Mathf.Log10(Master.value) * 20);
        Master.onValueChanged.AddListener((float value) =>
        {
            mixer.SetFloat("MasterVol", Mathf.Log10(value) * 20);
            PlayerPrefs.SetFloat("MasterVol",value);
        });
        Music.minValue = 0.0001f;
        Music.value = PlayerPrefs.GetFloat("MusicVol", 1);
        mixer.SetFloat("MusicVol", Mathf.Log10(Music.value) * 20);
        Music.onValueChanged.AddListener((float value) =>
        {
            mixer.SetFloat("MusicVol", Mathf.Log10(value) * 20);
            PlayerPrefs.SetFloat("MusicVol", value);
        });
        SFX.minValue = 0.0001f;
        SFX.value = PlayerPrefs.GetFloat("SFXVol", 1);
        mixer.SetFloat("SFXVol", Mathf.Log10(SFX.value) * 20);
        SFX.onValueChanged.AddListener((float value) =>
        {
            mixer.SetFloat("SFXVol", Mathf.Log10(value) * 20);
            PlayerPrefs.SetFloat("SFXVol", value);
        });
        nickNameInput.text = NickName;
        nickNameInput.onEndEdit.AddListener((string value) =>
        {
            if (value.Length <= 20)
            {
                if (value.Length >= 3)
                {
                    NickName = value;
                    PhotonNetwork.NickName = value;
                    nameMessage.color = Color.green;
                    nameMessage.text = "Successfully set name!";
                    PlayerPrefs.SetString("NickName", NickName);
                }
                else
                {
                    nameMessage.color = Color.red;
                    nameMessage.text = "Name is too short!";
                }
            }
            else
            {
                nameMessage.color = Color.red;
                nameMessage.text = "Name is too long!";
            }
        });

        PhotonNetwork.NickName = NickName;
        PhotonNetwork.AutomaticallySyncScene = true;
        menuManager.OpenMenu("ButtonsMenu");
        if (Application.isEditor)
            PlayerPrefs.SetInt("Cash", 2147483647);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if(cause != DisconnectCause.DisconnectByClientLogic)
        {
            errorText.text = "Error: " + cause.ToString();
            menuManager.OpenMenu("ErrorMenu");
        }
    }

    public override void OnConnectedToMaster()
    {
        loadingText.text = "Connecting to Lobby!";
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        menuManager.OpenMenu("Multiplayer");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Error[" + returnCode.ToString() + "]: " + message;
        menuManager.OpenMenu("ErrorMenu");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        errorText.text = "Error[" + returnCode.ToString() + "]: " + message;
        menuManager.OpenMenu("ErrorMenu");
    }

    public void OnDestroy()
    {
        PlayerPrefs.Save();
    }

    public override void OnJoinedRoom()
    {
        secondary.ClearOptions();
        primary.ClearOptions();
        primary.options.Add(new TMP_Dropdown.OptionData("None"));
        string pEquiped = PlayerPrefs.GetString("pE", "None");
        string sEquiped = PlayerPrefs.GetString("sE", "Glock");
        foreach (WeaponData wep in GameManager.Instance.weapons)
        {
            if (wep.equipable && (wep.givenByDefault || PlayerPrefs.GetInt(wep.name, 0) == 1))
            {
                if (wep.isSecondary)
                {
                    secondary.options.Add(new TMP_Dropdown.OptionData(wep.name));
                    if (wep.name == sEquiped && secondary.options.Count >= 1)
                    {
                        secondary.value = secondary.options.Count-1;
                    }
                }
                else
                {
                    primary.options.Add(new TMP_Dropdown.OptionData(wep.name));
                    if (wep.name == pEquiped && primary.options.Count >= 1)
                    {
                        primary.value = primary.options.Count - 1;
                    }
                }
            }
        }
        foreach (Transform transform in loadoutTransform.GetComponentsInChildren<Transform>(true))
            if (transform != null && transform.gameObject.name == "MinionDropdown")
                Destroy(transform.gameObject);
        for (int i = 0; i < PlayerPrefs.GetFloat("Minion Slots", 1f); i++)
        {
            int index = i;
            string mEquiped = PlayerPrefs.GetString("m" + index, "None");
            GameObject dropdown = Instantiate(minionDropdownPrefab);
            dropdown.name = "MinionDropdown";
            dropdown.transform.SetParent(loadoutTransform);
            dropdown.transform.localScale = Vector3.one;
            dropdown.GetComponentInChildren<TextMeshProUGUI>().text = "Minion " + (index + 1);
            TMP_Dropdown minionDropdown = dropdown.GetComponentInChildren<TMP_Dropdown>();
            foreach (Minion min in GameManager.Instance.minions)
            {
                if (min.givenByDefault || PlayerPrefs.GetInt(min.name, 0) == 1)
                {
                    minionDropdown.options.Add(new TMP_Dropdown.OptionData(min.name));
                    if (min.name == mEquiped)
                        minionDropdown.value = minionDropdown.options.Count - 1;
                }
            }
            minionDropdown.RefreshShownValue();
            minionDropdown.onValueChanged.AddListener((int val) =>
            {
                PlayerPrefs.SetString("m" + index, minionDropdown.options[val].text);
            });
        }
        primary.RefreshShownValue();
        secondary.RefreshShownValue();
        menuManager.OpenMenu("RoomMenu");
        object name;
        PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("RoomName", out name);
        roomName.text = (string)name;
        Dictionary<int, Player> players = PhotonNetwork.CurrentRoom.Players;
        foreach (KeyValuePair<int, Player> player in players)
            Instantiate(playerItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(player.Value);
        hostRoomControlPanel.SetActive(PhotonNetwork.IsMasterClient);
        nonhostRoomControlPanel.SetActive(!PhotonNetwork.IsMasterClient);
        UpdateLoadout();
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateRoomSettings();
        }
        else
        {
            ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
            if (properties["map"] != null)
                mapText.text = (string)properties["map"];
            if (properties["dif"] != null)
                difficultyText.text = difficultySelect.options[(int)properties["dif"]].text;
        }

        ExitGames.Client.Photon.Hashtable lpProperties = PhotonNetwork.LocalPlayer.CustomProperties;
        lpProperties["0"] = primary.options[primary.value].text;
        lpProperties["0d"] = (int)PlayerPrefs.GetFloat(primary.options[primary.value].text + "Damage", 0f);
        lpProperties["1"] = secondary.options[secondary.value].text;
        lpProperties["1d"] = (int)PlayerPrefs.GetFloat(secondary.options[secondary.value].text + "Damage", 0f);
        lpProperties["2"] = "Knife";
        lpProperties["2d"] = (int)PlayerPrefs.GetFloat("KnifeDamage", 0f);
        for (int i = 0; i < PlayerPrefs.GetFloat("Minion Slots", 1f); i++)
        {
            lpProperties["m"+i] = PlayerPrefs.GetString("m"+i+"E");
        }
        lpProperties["ph"] = 50 + (int)PlayerPrefs.GetFloat("PlayerMax Health",0f);
        lpProperties["ps"] = 50 + (int)PlayerPrefs.GetFloat("PlayerMax Shield", 0f);
        PhotonNetwork.LocalPlayer.SetCustomProperties(lpProperties);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        hostRoomControlPanel.SetActive(PhotonNetwork.IsMasterClient);
        nonhostRoomControlPanel.SetActive(!PhotonNetwork.IsMasterClient);
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable properties)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            if (properties["map"] != null)
                mapText.text = (string)properties["map"];
            if (properties["dif"] != null)
                difficultyText.text = difficultySelect.options[(int)properties["dif"]].text;
            if (properties["countdown"] != null)
            {
                if ((bool)properties["countdown"])
                {
                    StartCoroutine(CoStartCountDown());
                }
                else
                {
                    StopAllCoroutines();
                    startButtonText.text = "Start Game";
                    countDownText.text = "Waiting for host to start!";
                }
            }
        }
    }

    public override void OnLeftRoom()
    {
        menuManager.OpenMenu("Multiplayer");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform trans in roomListContent)
        {
            Destroy(trans.gameObject);
        }

        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList)
                continue;
            Instantiate(roomItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(info);
        }
    }

    public IEnumerator CoStartCountDown()
    {
        for (int i = 5; i >= 0; i--)
        {
            startButtonText.text = "Starting in: " + i;
            countDownText.text = "Starting in: " + i;
            yield return new WaitForSeconds(1f);
        }
        InTransition = true;
        SceneTransition.SetTrigger("Start");
        yield return new WaitForSeconds(1f);
        if (PhotonNetwork.IsMasterClient){
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.LoadLevel(1);
        }
        yield break;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(playerItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);
    }


    public void OpenMutliplayer()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            loadingText.text = "Connecting to Server!";
            menuManager.OpenMenu("LoadingMenu");
        }
        else
        {
            menuManager.OpenMenu("Multiplayer");
        }
    }

    public void UpdateRoomSettings()
    {
        ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
        properties["map"] = mapSelect.options[mapSelect.value].text;
        properties["dif"] = difficultySelect.value;
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    public void UpdateLoadout()
    {
        ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.LocalPlayer.CustomProperties;
        if (primary.options.Count > 0 && primary.value <= primary.options.Count - 1)
        {
            properties["0"] = primary.options[primary.value].text;
            properties["0d"] = (int)PlayerPrefs.GetFloat(primary.options[primary.value].text + "Damage", 0f);
            PlayerPrefs.SetString("pE", primary.options[primary.value].text);
        }
        if (secondary.options.Count>0 && secondary.value<=secondary.options.Count-1)
        {
            properties["1"] = secondary.options[secondary.value].text;
            properties["1d"] = (int)PlayerPrefs.GetFloat(secondary.options[secondary.value].text + "Damage", 0f);
            PlayerPrefs.SetString("sE", secondary.options[secondary.value].text);
        }
        properties["2"] = "Knife";
        properties["2d"] = (int)PlayerPrefs.GetFloat("KnifeDamage", 0f);
        PhotonNetwork.LocalPlayer.SetCustomProperties(properties);
    }

    public void JoinRoomWPass()
    {
        if(selectedRoom!=null)
            PhotonNetwork.JoinRoom(selectedRoom.Name);
    }

    public void JoinRoom(RoomInfo info)
    {
        PhotonNetwork.JoinRoom(info.Name);
    }

    public void LeaveCurrentRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void StartRoom()
    {
        if (!InTransition)
        {
            RoomStarting = !RoomStarting;
            if (RoomStarting)
            {
                PhotonNetwork.CurrentRoom.IsVisible = false;
                ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
                properties["countdown"] = true;
                PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
                StartCoroutine(CoStartCountDown());
            }
            else
            {
                PhotonNetwork.CurrentRoom.IsVisible = true;
                ExitGames.Client.Photon.Hashtable properties = PhotonNetwork.CurrentRoom.CustomProperties;
                properties["countdown"] = false;
                PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
                StopAllCoroutines();
                startButtonText.text = "Start Game";
                countDownText.text = "Waiting for host to start!";
            }
        }
    }

    public string RandomDigits(int length)
    {
        string s = string.Empty;
        for (int i = 0; i < length; i++)
            s = string.Concat(s, Random.Range(0, 10).ToString());
        return s;
    }

    public void CreateRoom()
    {
        string roomName = createRoomName.text == "" ? NickName + "'s room" : createRoomName.text;
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;
        roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable();
        roomOptions.CustomRoomProperties["RoomName"] = roomName;
        PhotonNetwork.CreateRoom(RandomDigits(9), roomOptions);
        loadingText.text = "Creating Room!";
        menuManager.OpenMenu("LoadingMenu");
    }
}
