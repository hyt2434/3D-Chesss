using UnityEngine;

public class DevTools : MonoBehaviour
{
    public void ResetPlayerData()
    {
        PlayerPrefs.DeleteAll(); // remove all preferences
        PlayerPrefs.Save();      // ensure changes persist to disk
    }
}