using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using GenerationTools;
using SeedTools;
using PoliticalEntities;

public class TitleScreenControls : MonoBehaviour
{
    public Canvas loadingCanvas;

    public void CreateGame()
    {
        StaticTerraform.Dispose();
        loadingCanvas.gameObject.SetActive(true);
        SceneManager.LoadScene("CreateGameMenu");
    }

    public void ResumeGame()
    {
    }

    public void TerraformMode()
        => SceneManager.LoadScene("TerraformMenu");

    public void SandboxMode()
    {
    }
}