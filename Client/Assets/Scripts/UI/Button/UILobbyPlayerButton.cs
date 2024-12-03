using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UILobbyPlayerButton : MonoBehaviour, ISelectHandler
{
    int _playerid;

    public void SetPlayerId(int id)
    {
        _playerid = id;
    }

    public void OnSelect(BaseEventData eventData)
    {
        UILobbyScene lobbyScene = GetComponentInParent<UILobbyScene>();
        if(lobbyScene != null )
            lobbyScene._selectbuttonid = _playerid;
        else
            Debug.Log("LobbyScene Found Fail");
    }
}
