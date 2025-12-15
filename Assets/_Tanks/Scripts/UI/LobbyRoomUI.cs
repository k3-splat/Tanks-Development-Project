using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;

public class LobbyRoomUI : MonoBehaviourPunCallbacks
{
    [Header("Room")]
    [SerializeField] private byte maxPlayers = 2;

    [Header("UI")]
    [SerializeField] private TMP_InputField joinCodeInput;  // JoinCodeInput
    [SerializeField] private TMP_Text statusText;           // 任意（JoinCodeTextでもOK）

    private string GetCode()
    {
        return joinCodeInput != null ? joinCodeInput.text.Trim() : "";
    }

    public void OnClickHost()
    {
        string code = GetCode();
        if (string.IsNullOrEmpty(code))
        {
            if (statusText) statusText.text = "Enter Join Code";
            return;
        }
        Host(code);
    }

    public void OnClickJoin()
    {
        string code = GetCode();
        if (string.IsNullOrEmpty(code))
        {
            if (statusText) statusText.text = "Enter Join Code";
            return;
        }
        Join(code);
    }

    // ---- core ----
    public void Host(string roomCode)
    {
        if (!PhotonNetwork.IsConnected)
        {
            if (statusText) statusText.text = "Not connected to Photon";
            return;
        }

        if (statusText) statusText.text = "Creating room: " + roomCode;
        PhotonNetwork.CreateRoom(roomCode, new RoomOptions { MaxPlayers = maxPlayers });
    }

    public void Join(string roomCode)
    {
        if (!PhotonNetwork.IsConnected)
        {
            if (statusText) statusText.text = "Not connected to Photon";
            return;
        }

        if (statusText) statusText.text = "Joining room: " + roomCode;
        PhotonNetwork.JoinRoom(roomCode);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[PUN] JoinedRoom: " + PhotonNetwork.CurrentRoom.Name);
        if (statusText) statusText.text = "Joined: " + PhotonNetwork.CurrentRoom.Name;
    }

    public override void OnCreateRoomFailed(short code, string msg)
    {
        Debug.LogError($"CreateRoomFailed {code} {msg}");
        if (statusText) statusText.text = $"Create failed: {msg}";
    }

    public override void OnJoinRoomFailed(short code, string msg)
    {
        Debug.LogError($"JoinRoomFailed {code} {msg}");
        if (statusText) statusText.text = $"Join failed: {msg}";
    }
}
