using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using PoliticalEntities;

public class GameInterfaceControls : MonoBehaviour
{
    public Tilemap terrainMap;
    public TileCollection tileCollection;
    public CameraManager cameraManager;

    public Canvas backgroundCanvas;
    public Image backgroundImage;

    public GameObject leftSidePanel, rightSidePanel;

    private bool leftPanelPulled = false;
    private bool rightPanelPulled = false;

    public void ExitSession()
    {
        StaticGameInstance.SaveInstance();
        SceneManager.LoadScene("TitleScreen");
    }

    public void PullLeftPanel()
    {
        Vector3 vec = leftSidePanel.transform.position;
        vec.x += leftPanelPulled ? -570f : 570f;
        leftSidePanel.transform.position = vec;
        leftPanelPulled = !leftPanelPulled;
    }

    public void PullRightPanel()
    {
        Vector3 vec = rightSidePanel.transform.position;
        vec.x += rightPanelPulled ? 600f : -600f;
        rightSidePanel.transform.position = vec;
        rightPanelPulled = !rightPanelPulled;
    }

    private void Start()
    {
        // This scene works DIRECTLY from the static instance
        // Such must be setup prior to using this screen
        if (!StaticGameInstance.Exists())
            throw new System.Exception("GI01: No bound instance");
        cameraManager.camera.orthographicSize = 10f;
        DrawTerrainMap();
    }

    private void DrawTerrainMap()
    {
        int mapSize = Terr().GetLength(0);
        // clear map and paint
        AssetDatabase.Refresh();
        terrainMap.ClearAllTiles();
        Tile[] types = tileCollection.GetCollection();
        for (int x = 0; x < mapSize; x++)
            for (int y = 0; y < mapSize; y++)
            { terrainMap.SetTile(new(x, y), types[Terr()[x, y]]); }
        // set the background
        CameraManager c = cameraManager;
        RectTransform tCV = backgroundCanvas.GetComponent<RectTransform>();
        RectTransform t = backgroundImage.GetComponent<RectTransform>();
        Vector3 thisP = terrainMap.transform.position;
        tCV.sizeDelta = new(mapSize, mapSize / 2f);
        tCV.anchoredPosition = new(thisP.x, thisP.y + mapSize / 4f);
        t.sizeDelta = new(mapSize, mapSize / 2f);
        c.BindTo(mapSize, false);
    }

    private World GetSessionWorld() => StaticGameInstance.Get().GetWorld();

    private byte[,] Terr() => GetSessionWorld().GetTerr();

    private string[,,] Pol() => GetSessionWorld().GetPol();

}
