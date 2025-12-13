using UnityEngine;

public class LobbyStampSelectLocal : MonoBehaviour
{
    [SerializeField] private Sprite[] stampSprites;
    [SerializeField] private StampDisplay selfStampDisplay;

    public void OnClickStamp(int stampId)
    {
        selfStampDisplay.Show(stampSprites[stampId]);
    }
}
