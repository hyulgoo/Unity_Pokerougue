using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UICommonInputField : MonoBehaviour, ISelectHandler, IDeselectHandler
{ 
    public UICommonPopup UI_Popup { get; set; }
    public void OnSelect(BaseEventData eventData)
    {
        UI_Popup.InputMode = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        UI_Popup.InputMode = false;
    }
}
