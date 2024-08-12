using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_PlayerButton : MonoBehaviour, ISelectHandler
{
    int _playerid;

    public void SetPlayerId(int id)
    {
        _playerid = id;
    }

    public void OnSelect(BaseEventData eventData)
    {
        UI_LobbyScene lobby = GetComponentInParent<UI_LobbyScene>();
        if(lobby != null )
        {
            lobby._selectbuttonid = _playerid;
        }
        else
        {
            Debug.Log("LOBBY is Empty");
        }
    }


}
