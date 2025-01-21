using UnityEngine;
using UnityEngine.UI;

public class InstructionsPanelHandler : MonoBehaviour
{
    public GameObject instructionsPanel; // Reference to the panel
    public Button exitButton; // Reference to the exit button

    void Start()
    {
        // Ensure the panel is hidden at the start
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(false);
        }

        // Add listener to the exit button
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ClosePanel);
        }
        else
        {
            Debug.LogError("Exit button is not assigned!");
        }
    }

    // Method to show the instructions panel
    public void ShowPanel()
    {
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Instructions panel is not assigned!");
        }
    }

    // Method to close the instructions panel
    private void ClosePanel()
    {
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(false);
        }
    }
}

