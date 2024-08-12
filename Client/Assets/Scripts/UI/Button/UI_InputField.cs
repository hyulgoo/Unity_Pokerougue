using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_InputField : MonoBehaviour, ISelectHandler, IDeselectHandler
{ 
    public void OnSelect(BaseEventData eventData)
    {
        Managers.Object.MyPlayer._inputMode = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Managers.Object.MyPlayer._inputMode = false;
    }
}
