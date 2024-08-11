using PoliticalEntities;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MinimapSelectManager : MonoBehaviour
{
    private Dictionary<Color, Minimap> _colMap;

    private Texture2D _lastTex = null;
    private Minimap _lastMap = null;

    public Minimap background;

    public Button templateButton;

    private readonly List<GameObject> buttonCache = new();

    public CreateGameMenuControls creationController; // nullable

    private IEnumerator SelectEthnicCoroutine()
    {
        yield return new WaitForEndOfFrame();
        Vector3 p = Input.mousePosition;
        Vector2Int pos = new((int)p.x, (int)p.y);
        Texture2D t = ScreenCapture.CaptureScreenshotAsTexture();
        Color c = t.GetPixel(pos.x, pos.y);
        // reset the map selection
        if (_lastTex != null) AssignTexture(_lastMap, _lastTex);
        foreach (GameObject button in buttonCache) Destroy(button);
        buttonCache.Clear();
        // select the relevant minimap/ethnic
        if (_colMap.ContainsKey(c))
        {
            World world = StaticTerraform.Get();
            Ethnic findEth = null;
            Dictionary<Realm, Vector4> found = new();
            Texture2D eC = (Texture2D)_colMap[c]
                .GetComponent<MeshRenderer>().material.mainTexture;
            _lastMap = _colMap[c];
            _lastTex = PixelCopyFrom(eC);
            int len = eC.width;
            Color dark = new(0f, 0f, 0f, 0.8f);
            Color light = Color.Lerp(c, Color.white, 0.75f);
            for (int x = 1; x < len - 1; x++)
                for (int y = 1; y < len - 1; y++)
                {
                    if (!eC.GetPixel(x, y).Equals(c))
                    {
                        eC.SetPixel(x, y, dark);
                        continue;
                    }
                    Realm rea = world.GetRealm(x, y);
                    if (rea != null && rea.Type == RealmType.PLAYER)
                    {
                        eC.SetPixel(x, y, light);
                        if (!found.ContainsKey(rea))
                        { found[rea] = new(x, y, x, y); }
                        else
                        {
                            Vector4 box = found[rea];
                            if (x > box.z) box.z = x;
                            if (y < box.y) box.y = y;
                            else if (y > box.w) box.w = y;
                            found[rea] = box;
                        }
                    }
                    findEth ??= world.GetEthnic(x, y);
                }
            int rI = 0;
            float mult = 1000f / 2f / len;
            foreach (Realm r in found.Keys)
            {
                Button b = Instantiate(templateButton);
                b.name = "Realm" + rI++;
                b.gameObject.SetActive(true);
                Vector3 bP = b.transform.position;
                Vector4 box = found[r];
                bP.x = mult * (box.x + box.z);
                bP.y = mult * (box.y + box.w);
                b.transform.position = bP;
                b.transform.SetParent(
                    templateButton.transform.parent, false);
                b.onClick.AddListener(() => creationController.SelectRealm(r));
                buttonCache.Add(b.gameObject);
            }
            eC.Apply();
            // open the ethnic panel
            creationController.SelectEthnic(findEth);
            // force the terrain map back
            AssignTexture(background, (Texture2D)background
                .GetComponent<MeshRenderer>().material.mainTexture);
            // force other eths behind
            foreach (Minimap m in _colMap.Values)
                if (m != _colMap[c]) AssignTexture(m, (Texture2D)m
                    .GetComponent<MeshRenderer>().material.mainTexture);
        }
        else
        {
            // force the terrain map back
            AssignTexture(background, (Texture2D)background
                .GetComponent<MeshRenderer>().material.mainTexture);
            // remove selection panels
            creationController.Deselect();
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData pointerEventData = new(EventSystem.current)
            { position = Input.mousePosition };
            List<RaycastResult> raycastResults = new();
            EventSystem.current.RaycastAll(pointerEventData, raycastResults);
            bool pass = true;
            foreach (RaycastResult result in raycastResults)
                if (result.gameObject.GetComponent<Button>() != null)
                { pass = false; break; }
            // permit coroutine if not a button
            if (pass) StartCoroutine(SelectEthnicCoroutine());
        }
    }

    public void Bind(Dictionary<Color, Minimap> map) => _colMap = map;

    private static void AssignTexture(Minimap map, Texture2D tex)
        => map.GetComponent<MeshRenderer>().material
                = new(Shader.Find("UI/Default"))
                { mainTexture = tex };

    private static Texture2D PixelCopyFrom(Texture2D tex)
    {
        Texture2D export = new(tex.width, tex.height)
        { filterMode = FilterMode.Point };
        export.SetPixels(tex.GetPixels());
        export.Apply();
        return export;
    }

}
