using GenerationTools;
using PoliticalEntities;
using SeedTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Debug = UnityEngine.Debug;
using Image = UnityEngine.UI.Image;
using Slider = UnityEngine.UI.Slider;
using WP = GenerationTools.WorldPopulator;

public class TerraformMenuControls : MonoBehaviour
{
    private int lastMapSize = 0;
    private bool minimapLarge = false;
    private bool minimapEth = false;

    private byte[,] terr;
    private World save;

    public CameraManager cameraManager;
    public Minimap minimap;
    public RectTransform minimapBounds;
    public Tilemap terrMap;

    public Canvas terrBackCV;
    public Image terrBack;

    public Canvas loadingCanvas;

    public Tile
        cloudTile, upperCloudTile, mountainsTile,
        oceanTile, shallowTile, swampTile,
        wetlandsTile, plainsTile, forestTile,
        desertTile, dryForestTile;

    public TMP_InputField
        mapSizeInput, borderInput, seedInput,
        cyclesInput, floodsPerCycleInput,
        floodLengthInput, floodBufferInput, floodDecayInput,
        dryBufferInput, sinkBufferInput, settleCyclesInput,
        forestCyclesInput, forestSizeInput,
        desertCyclesInput,
        riverCyclesInput, riverSizeInput,
        maxEthnicsInput, maxRealmsInput;

    public TMP_Dropdown
        autofillDropdown,
        regionSizeDropdown,
        wildernessDropdown;

    public Slider
        floodVarianceInput,
        fluidSettleInput, landSettleInput,
        weatherRadialInput, weatherForceInput,
        forestDensityInput,
        desertVarianceInput,
        algaeInput, riverCurveInput, riverDepthInput, riverPreferenceInput,
        regionRoughnessInput;

    public Button
        toggleEthnicsButton,
        generateEthnicsButton,
        createMapButton;

    private void Start()
    {
        // if there is a previous map, show it first
        if (StaticTerraform.Exists())
        {
            World get = StaticTerraform.Get();
            terr = get.GetTerr();
            save = get;
            DrawTerrain();
            DrawEthnics();
            ToggleEthnics();
        }
    }

    public void ExitToTitleScreen()
        => SceneManager.LoadScene("TitleScreen");

    public void ResizeMinimap()
    {
        if (minimapLarge)
        {
            Vector2 panelVec = minimapBounds.sizeDelta;
            panelVec.x = 500f;
            panelVec.y = 250f;
            minimapBounds.sizeDelta = panelVec;
            minimap.transform.rotation = Quaternion.Euler(60f, 0f, 45f);
            minimap.transform.localScale = new(300f, 300f, 300f);
            minimapLarge = false;
        }
        else
        {
            Vector2 panelVec = minimapBounds.sizeDelta;
            panelVec.x = 950f;
            panelVec.y = 950f;
            minimapBounds.sizeDelta = panelVec;
            minimap.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            minimap.transform.localScale = new(600f, 600f, 600f);
            minimapLarge = true;
        }
    }

    public void ToggleEthnics()
    {
        if (minimapEth)
        {
            minimap.DrawTerrain(terr);
            minimapEth = false;
        }
        else
        {
            minimap.DrawEthnics(save.GetPol(), true);
            minimapEth = true;
        }
    }

    public void AutofillPreset()
        => AutofillFields(autofillDropdown.value);

    public void RandomSeed()
        => seedInput.SetTextWithoutNotify(new Seed().Get() + "");

    public void GenerateTerrain()
    {
        try
        {
            // ints
            int mapSeed = int.Parse(seedInput.text);
            int mapSize = int.Parse(mapSizeInput.text);
            int cycles = int.Parse(cyclesInput.text);
            int floodsPerCycle = int.Parse(floodsPerCycleInput.text);
            int floodSize = int.Parse(floodLengthInput.text);
            int floodOriginBuffer = int.Parse(floodBufferInput.text);
            int floodDecay = int.Parse(floodDecayInput.text);
            int fluidDryBuffer = int.Parse(dryBufferInput.text);
            int landSinkBuffer = int.Parse(sinkBufferInput.text);
            int settleCycles = int.Parse(settleCyclesInput.text);
            int rivers = int.Parse(riverCyclesInput.text);
            int riverSize = int.Parse(riverSizeInput.text);
            int forestSize = int.Parse(forestSizeInput.text);
            int forestAmount = int.Parse(forestCyclesInput.text);
            int desertCycles = int.Parse(desertCyclesInput.text);
            int borderThickness = int.Parse(borderInput.text);
            // floats
            float floodOriginVariance = floodVarianceInput.value;
            float fluidSettleStrength = fluidSettleInput.value;
            float landSettleStrength = landSettleInput.value;
            float riverStraightness = riverCurveInput.value;
            float riverNatPref = riverPreferenceInput.value;
            float shoreSwamps = algaeInput.value;
            float riverDepth = riverDepthInput.value;
            float forestDensity = forestDensityInput.value;
            float weatherDirection = weatherRadialInput.value;
            float weatherForce = weatherForceInput.value;
            float desertVariance = desertVarianceInput.value;
            // construct
            terr = MapConstructor.Export(mapSize,
                (c) => c
                .ApplySeed(mapSeed)
                .OverlayFlooderFill(cycles, floodsPerCycle, floodSize,
                    floodOriginBuffer, 0,
                    floodDecay, false, true,
                    true, floodOriginVariance)
                .DeteriorateWetlands(0.1, true, 1, 0)
                .SettleFluids(fluidSettleStrength, landSettleStrength,
                    fluidDryBuffer, landSinkBuffer, settleCycles, 0)
                .GenerateRivers(rivers, riverSize, true,
                    riverStraightness, riverDepth, riverNatPref, 0, 0)
                .SimpleLandCleanup(false, 0)
                .BuildShalllows(shoreSwamps, 0)
                .ExpandShallows(1, 0)
                .GrowForests(forestSize, forestAmount, 0,
                    false, forestDensity)
                .Desertifacation(1, 0, true, weatherDirection, 0, 0)
                .Desertifacation(desertCycles, 0, false,
                    weatherDirection, weatherForce, desertVariance)
                .SimpleLandCleanup(false, 0)
                .SimpleLandCleanup(false, 0)
                .SetCloudBorder(borderThickness)
                );
            // draw terrain map
            DrawTerrain();
        }
        // catch parsing error
        catch (FormatException)
        { throw new Exception("TM01: Field Parsing Failed"); }
    }

    public void GenerateEthnics()
    {
        try
        {
            // parsings
            int mapSeed = int.Parse(seedInput.text);
            int maxEthnics = int.Parse(maxEthnicsInput.text);
            int maxRealmsPerEthnic = int.Parse(maxRealmsInput.text);
            int regionOption = regionSizeDropdown.value;
            float regionRoughness = regionRoughnessInput.value;
            bool forceWildDeserts = wildernessDropdown.value == 0;
            // run populator algorithm
            Seed seed = new(mapSeed);
            World world = WP.Export(
                terr, mapSeed,
                maxEthnics, GetSizeOption(regionOption), regionRoughness,
                forceWildDeserts);
            world.SpawnWorldRealms(maxRealmsPerEthnic);
            save = world;
            // draw ethnics map
            DrawEthnics();
        }
        // catch parsing error
        catch (FormatException)
        { throw new Exception("TM02: Field Parsing Failed"); }
    }

    private void DrawTerrain()
    {
        int mapSize = terr.GetLength(0);
        // clear map and paint
        AssetDatabase.Refresh();
        terrMap.ClearAllTiles();
        Tile[] types = MapUtil.GetTileSet(
            cloudTile, upperCloudTile,
            oceanTile, swampTile, wetlandsTile, shallowTile,
            plainsTile, mountainsTile, forestTile,
            desertTile, dryForestTile);
        for (int x = 0; x < mapSize; x++)
            for (int y = 0; y < mapSize; y++)
            {
                terrMap.SetTile(new(x, y), types[terr[x, y]]);
            }
        // set minimap
        minimap.DrawTerrain(terr);
        minimap.SetCacheNull();
        minimapEth = false;
        generateEthnicsButton.interactable = true;
        toggleEthnicsButton.interactable = false;
        // set the background
        CameraManager c = cameraManager;
        RectTransform tCV = terrBackCV.GetComponent<RectTransform>();
        RectTransform t = terrBack.GetComponent<RectTransform>();
        Vector3 thisP = terrMap.transform.position;
        tCV.sizeDelta = new(mapSize, mapSize / 2);
        tCV.anchoredPosition = new(thisP.x, thisP.y + mapSize / 4);
        t.sizeDelta = new(mapSize, mapSize / 2);
        c.camera.orthographicSize = CameraManager.MAX_ZOOM;
        c.BindTo(mapSize);
        if (mapSize != lastMapSize)
        {
            c.camera.transform.position = new(0f, mapSize / 4f, -1f);
            lastMapSize = mapSize;
        }
    }

    private void DrawEthnics()
    {
        // enable creation and toggle
        createMapButton.interactable = true;
        toggleEthnicsButton.interactable = true;
        minimapEth = false;
        minimap.SetCacheNull();
        ToggleEthnics();
    }

    public void CreateMap()
    {
        StaticTerraform.Bind(save);
        terrMap.ClearAllTiles();
        loadingCanvas.gameObject.SetActive(true);
        SceneManager.LoadScene("CreateGameMenu");
    }

    private void AutofillFields(int option)
    {
        int[] ints;
        float[] floats;
        int regSizeOption, wildOption;
        // build from option
        if (option == 0)
        {
            ints = new int[]
            {
                256, 1,
                6, 6, 2500, 0, 100,
                3, 3, 15,
                100, 50,
                15,
                50, 3,
                16, 4
            };
            floats = new float[]
            {
                0.5f,
                0.05f, 0.2f,
                0.1f, 0.99f,
                0.2f,
                0.99f,
                0.05f, 0.1f, 0.5f, 0.0f,
                0.7f
            };
            regSizeOption = 2; wildOption = 2;
        }
        else /* if (option == 1) */
        {
            ints = new int[]
            {
                512, 1,
                6, 6, 10000, 0, 400,
                3, 3, 15,
                200, 100,
                25,
                100, 3,
                16, 4
            };
            floats = new float[]
            {
                0.5f,
                0.05f, 0.2f,
                0.1f, 0.99f,
                0.2f,
                0.99f,
                0.05f, 0.1f, 0.5f, 0.0f,
                0.7f
            };
            regSizeOption = 2; wildOption = 2;
        }
        // assign to fields
        RandomSeed();
        mapSizeInput.SetTextWithoutNotify(ints[0] + "");
        borderInput.SetTextWithoutNotify(ints[1] + "");
        cyclesInput.SetTextWithoutNotify(ints[2] + "");
        floodsPerCycleInput.SetTextWithoutNotify(ints[3] + "");
        floodLengthInput.SetTextWithoutNotify(ints[4] + "");
        floodBufferInput.SetTextWithoutNotify(ints[5] + "");
        floodDecayInput.SetTextWithoutNotify(ints[6] + "");
        dryBufferInput.SetTextWithoutNotify(ints[7] + "");
        sinkBufferInput.SetTextWithoutNotify(ints[8] + "");
        settleCyclesInput.SetTextWithoutNotify(ints[9] + "");
        forestCyclesInput.SetTextWithoutNotify(ints[10] + "");
        forestSizeInput.SetTextWithoutNotify(ints[11] + "");
        desertCyclesInput.SetTextWithoutNotify(ints[12] + "");
        riverCyclesInput.SetTextWithoutNotify(ints[13] + "");
        riverSizeInput.SetTextWithoutNotify(ints[14] + "");
        maxEthnicsInput.SetTextWithoutNotify(ints[15] + "");
        maxRealmsInput.SetTextWithoutNotify(ints[16] + "");
        // assign to sliders
        floodVarianceInput.SetValueWithoutNotify(floats[0]);
        fluidSettleInput.SetValueWithoutNotify(floats[1]);
        landSettleInput.SetValueWithoutNotify(floats[2]);
        weatherRadialInput.SetValueWithoutNotify(floats[3]);
        weatherForceInput.SetValueWithoutNotify(floats[4]);
        forestDensityInput.SetValueWithoutNotify(floats[5]);
        desertVarianceInput.SetValueWithoutNotify(floats[6]);
        algaeInput.SetValueWithoutNotify(floats[7]);
        riverCurveInput.SetValueWithoutNotify(floats[8]);
        riverDepthInput.SetValueWithoutNotify(floats[9]);
        riverPreferenceInput.SetValueWithoutNotify(floats[10]);
        regionRoughnessInput.SetValueWithoutNotify(floats[11]);
        // assign to dropdowns
        regionSizeDropdown.SetValueWithoutNotify(regSizeOption);
        wildernessDropdown.SetValueWithoutNotify(wildOption);
    }

    private static RegS GetSizeOption(int fromDropdown)
    {
        switch (fromDropdown)
        {
            case 0: return RegS.TINY;
            case 1: return RegS.SMALL;
            case 2: return RegS.NORMAL;
            case 3: return RegS.LARGE;
            case 4: return RegS.HUGE;
        }
        throw new ArgumentException("TM03: Region Parse Failure");
    }

}
