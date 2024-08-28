using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_InputField : MonoBehaviour, ISelectHandler, IDeselectHandler
{ 
    public UI_Popup UI_Popup { get; set; }
    public void OnSelect(BaseEventData eventData)
    {
        UI_Popup.InputMode = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        UI_Popup.InputMode = false;
    }
}
