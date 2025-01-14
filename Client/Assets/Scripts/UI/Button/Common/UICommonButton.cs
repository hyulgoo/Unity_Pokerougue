using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UICommonButton : MonoBehaviour, ISelectHandler
{    
    // ISelectHandler 인터페이스
    public virtual void OnSelect(BaseEventData eventData)
    {
        GameObject go = Managers.Select.CurPanel;
        if (go == null) 
            return;

        UICommonBase uibase = go.GetComponent<UICommonBase>();
        if (uibase == null) 
            return;

        uibase._lastSelected = eventData.selectedObject.GetComponent<Button>();
        uibase.SetPointerPos(eventData.selectedObject.transform);
        Managers.Sound.Play("system/select");
    }    
}
