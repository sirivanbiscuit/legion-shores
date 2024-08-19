using GenerationTools;
using PoliticalEntities;
using SeedTools;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreateGameMenuControls : MonoBehaviour
{
    private World upload;

    private Realm selected;

    public Minimap map;

    public TMP_Text infoText;

    public GameObject ethnicPanel;
    public GameObject realmPanel;
    public GameObject startButton;

    public GameObject ethnicNull, realmNull;

    public TMP_Text ethnicName, ethnicDesc;
    public TMP_Text realmName, realmDesc;

    public Canvas loadingCanvas;

    private void Start()
    {
        if (!StaticTerraform.Exists()) BuildDefault();
        // get world and draw
        upload = StaticTerraform.Get();
        int len = upload.GetTerr().GetLength(0);
        map.DrawWorld(upload);
        SetInfoText();
        // remove info panels
        ethnicPanel.SetActive(false);
        realmPanel.SetActive(false);
        startButton.SetActive(false);
        ethnicNull.SetActive(true);
        realmNull.SetActive(true);
    }

    public void ExitToTitleScreen()
        => SceneManager.LoadScene("TitleScreen");

    public void StartGame()
    {
        if (selected == null) return; // shouldn't ever happen
        GameSession gameSession = new(upload, selected);
        StaticGameInstance.BindAndSave(gameSession);
        loadingCanvas.gameObject.SetActive(true);
        SceneManager.LoadScene("GameSessionScreen");
    }

    private void SetInfoText()
    {
        infoText.text =
            "Map Size: " + upload.Size()
            + "    Seed: " + upload.Seed()
            + "    Ethnics: " + (upload.EthnicsCount() - 1)
            + "    Players: " + upload.PlayerCount()
            + "    Realms: " + upload.RealmsCount();
    }

    private static void BuildDefault()
    {
        // parameters for default 256-world
        int seed = new Seed().Get();
        byte[,] defaultTerr = MapConstructor.Export(256,
                (c) => c
                .ApplySeed(seed)
                .OverlayFlooderFill(6, 6, 2500, 0, 0, 100,
                    false, true, true, 0.5f)
                .DeteriorateWetlands(0.1, true, 1, 0)
                .SettleFluids(0.05f, 0.2f, 3, 3, 15, 0)
                .GenerateRivers(50, 3, true, 0.1f, 0.5f, 0.0f, 0, 0)
                .SimpleLandCleanup(false, 0)
                .BuildShalllows(0.05, 0)
                .ExpandShallows(1, 0)
                .GrowForests(100, 50, 0, false, 0.2f)
                .Desertifacation(1, 0, true, 0.1f, 0, 0)
                .Desertifacation(15, 0, false, 0.1f, 0.99f, 0.99f)
                .SimpleLandCleanup(false, 0)
                .SimpleLandCleanup(false, 0)
                .SetCloudBorder(1)
                );
        World buildDefault = WorldPopulator.Export(
            defaultTerr, seed, 16, RegS.NORMAL, 0.7f, false);
        buildDefault.SpawnWorldRealms(4);
        StaticTerraform.Bind(buildDefault);
    }

    public void SelectEthnic(Ethnic ethnic)
    {
        ethnicPanel.SetActive(true);
        ethnicNull.SetActive(false);
        ethnicName.text = ethnic.GetName();
        ethnicDesc.text = ethnic.PlayersCount() + " Playable Realms";
    }

    public void SelectRealm(Realm realm)
    {
        realmPanel.SetActive(true);
        realmNull.SetActive(false);
        startButton.SetActive(true);
        realmName.text = realm.GetName();
        realmDesc.text = realm.VassalsCount() + " Starting Vassals";
        selected = realm;
    }

    public void Deselect()
    {
        ethnicPanel.SetActive(false);
        realmPanel.SetActive(false);
        ethnicNull.SetActive(true);
        realmNull.SetActive(true);
        startButton.SetActive(false);
        selected = null;
    }

}