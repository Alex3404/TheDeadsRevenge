using Assets.Scripts;
using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUIController : MonoBehaviourPun
{
    public Slider healthSlider, shieldSlider, waveSlider; 
    public GameObject pauseGui, deathGui, respawnCounter;
    public TextMeshProUGUI healthText, shieldText, ammoText, gunName, waveText, respawnIn, deathmenuInfo, cashText;
    public Image LowHealth;
    public bool paused = false;
    PlayerController playerController;
    PhotonView PV;
    MenuManager menuManager;
    GameManager roomManager;

    [SerializeField] AudioMixer mixer;
    [SerializeField] Slider Master;
    [SerializeField] Slider Music;
    [SerializeField] Slider SFX;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        menuManager = GetComponent<MenuManager>();
        PV = GetComponent<PhotonView>();
        roomManager = GameManager.Instance;

        Master.minValue = 0.0001f;
        Master.value = PlayerPrefs.GetFloat("MasterVol",1);
        mixer.SetFloat("MasterVol", Mathf.Log10(Master.value) * 20);
        Master.onValueChanged.AddListener((float value) =>
        {
            mixer.SetFloat("MasterVol", Mathf.Log10(value) * 20);
            PlayerPrefs.SetFloat("MasterVol", value);
            PlayerPrefs.Save();
        });
        Music.minValue = 0.0001f;
        Music.value = PlayerPrefs.GetFloat("MusicVol",1);
        mixer.SetFloat("MusicVol", Mathf.Log10(Music.value) * 20);
        Music.onValueChanged.AddListener((float value) =>
        {
            mixer.SetFloat("MusicVol", Mathf.Log10(value) * 20);
            PlayerPrefs.SetFloat("MusicVol", value);
            PlayerPrefs.Save();
        });
        SFX.minValue = 0.0001f;
        SFX.value = PlayerPrefs.GetFloat("SFXVol",1);
        mixer.SetFloat("SFXVol", Mathf.Log10(SFX.value) * 20);
        SFX.onValueChanged.AddListener((float value) =>
        {
            mixer.SetFloat("SFXVol", Mathf.Log10(value) * 20);
            PlayerPrefs.SetFloat("SFXVol", value);
            PlayerPrefs.Save();
        });
    }

    public void FixedUpdate()
    {
        if (playerController.isDead&& !roomManager.GameEnded)
        {
            respawnCounter.SetActive(true);
            respawnIn.text = $"You Respawn in:\n{(playerController.respawnTime - playerController.timeSinceHit).ToString("0.0")}";
        }
        else
        {
            respawnCounter.SetActive(false);
        }
        Weapon weapon = playerController.weapons[playerController.equippedWeapons[playerController.weaponIndex]];
        ammoText.text = $"{(weapon.isReloading() ? "(Reloading) " : "")}{weapon.getAmmo()}/{weapon.getMaxAmmo()}";
        gunName.text = weapon.gameObject.name;
        float healthValue = playerController.GetHealth() / playerController.MaxHealth;
        healthSlider.value = healthValue;
        shieldSlider.value = playerController.GetShield() / playerController.MaxShield;
        healthText.text = $"Health: {playerController.GetHealth()}/{playerController.MaxHealth}";
        shieldText.text = $"Shield: {playerController.GetShield()}/{playerController.MaxShield}";
        if (healthValue < 0.7f)
            LowHealth.color = new Color(
                LowHealth.color.r,
                LowHealth.color.g,
                LowHealth.color.b,
                Mathf.Abs(healthValue-1)/0.7f*0.35f
            );
        else
            LowHealth.color = new Color(LowHealth.color.r, LowHealth.color.g, LowHealth.color.b, 0f);
        if (GameManager.Instance != null)
        {
            waveText.text = "Wave: " + GameManager.Instance.Wave;
            waveSlider.value = Mathf.Abs(((float)GameObject.FindGameObjectsWithTag("Enemy").Length / (GameManager.Instance.Wave+4)) - 1);
        }
    }

    public void UpdateCashText()
    {
        cashText.text = System.String.Format("{0:n0}", PlayerPrefs.GetInt("Cash"));
    }

    public void GameEnded()
    {
        Resume();
        int highscore = PlayerPrefs.GetInt("Highscore", 0);
        bool newHighscore = false;
        if (highscore < GameManager.Instance.Wave)
        {
            PlayerPrefs.SetInt("Highscore", GameManager.Instance.Wave);
            highscore = GameManager.Instance.Wave;
            newHighscore = true;
        }
        respawnCounter.SetActive(false);
        deathGui.SetActive(true);
        deathmenuInfo.text =
            $"Enemys Killed: {GameManager.Instance.EnemysDowned}\n" +
            $"Died on Wave: {GameManager.Instance.Wave}\n" +
            $"Wave Highscore: {highscore} {(newHighscore ? "(New!)" : "")}\n" +
            $"Cash Gained: ${GameManager.Instance.CashGained}\n";
    }

    public void Resume()
    {
        paused = false;
        pauseGui.SetActive(paused);
    }

    public void QuitToMainMenu()
    {
        if (PhotonNetwork.InRoom)
        {
            GameManager.Instance.QuitToMainMenu();
        }
        else
        {
            MusicManager.Instance.StopSonic();
            SceneManager.LoadScene("Main Menu");
        }
    }

    void Update()
    {
        if (PV.IsMine&& !roomManager.GameEnded)
        {
            if (Input.GetKeyDown(KeyCode.Escape) && menuManager.onMainMenu)
            {
                paused = !paused;
                pauseGui.SetActive(paused);
                UpdateCashText();
            }
        }
    }
}
