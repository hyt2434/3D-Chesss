// ConfirmDialogController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmDialogController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI messageText; // The text field to show the confirmation message
    public Button yesButton;            // Button for confirming
    public Button noButton;             // Button for cancelling

    /// <summary>
    /// Show the confirmation dialog.
    /// </summary>
    /// <param name="message">The question to display.</param>
    /// <param name="onYes">Callback when user clicks Yes.</param>
    /// <param name="onNo">Callback when user clicks No.</param>
    public void Show(string message, System.Action onYes, System.Action onNo)
    {
        gameObject.SetActive(true);
        messageText.text = message;

        // Clear any previous listeners
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        // Configure Yes button
        yesButton.onClick.AddListener(() =>
        {
            onYes?.Invoke();
            Hide();
        });

        // Configure No button
        noButton.onClick.AddListener(() =>
        {
            onNo?.Invoke();
            Hide();
        });
    }

    /// <summary>
    /// Hide the confirmation dialog.
    /// </summary>
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
