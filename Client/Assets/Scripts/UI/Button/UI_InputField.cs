using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_InputField : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    TMP_InputField _input;

    void Start()
    {
        _input = GetComponent<TMP_InputField>();
        _input.onValueChanged.AddListener(SetPlayerName);
    }

    void SetPlayerName(string name)
    {
        //Managers.Object.MyPlayer.Name = name;
    }

    public void OnSelect(BaseEventData eventData)
    {
        //Managers.Object.MyPlayer._inputMode = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        //Managers.Object.MyPlayer._inputMode = false;
    }
}
