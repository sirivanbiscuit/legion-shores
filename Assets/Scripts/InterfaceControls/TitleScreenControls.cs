using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using GenerationTools;
using SeedTools;
using PoliticalEntities;
using System.IO;

public class TitleScreenControls : MonoBehaviour
{
    public Canvas loadingCanvas;
    public Canvas resumingCanvas;

    public void CreateGame()
    {
        StaticTerraform.Dispose();
        loadingCanvas.gameObject.SetActive(true);
        SceneManager.LoadScene("CreateGameMenu");
    }

    public void ResumeGame()
    {
        try
        {
            StaticGameInstance.LoadInstance();
            StaticTerraform.Bind(StaticGameInstance.Get().GetWorld());
            resumingCanvas.gameObject.SetActive(true);
            SceneManager.LoadScene("GameSessionScreen");
        }
        catch (FileNotFoundException) { /* Do nothing */ }
    }

    public void TerraformMode()
        => SceneManager.LoadScene("TerraformMenu");

    public void SandboxMode()
    {
        // TODO: add this
    }
}
