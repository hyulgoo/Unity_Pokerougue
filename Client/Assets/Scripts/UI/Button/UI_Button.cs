using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_Button : MonoBehaviour, ISelectHandler
{
    public string _nextUIName = null;

    

    // 다른 창을 띄우는 버튼에 할당될 메서드
    public void SetNextPanel()
    {
        UI_Panel nextPanel = Managers.UI.ShowPopupUI<UI_Panel>(_nextUIName);
        Managers.Select.CurPanel = nextPanel.gameObject;
    }

    // 닫기 버튼에 할당될 메서드
    public void ClosePanel()
    {
        UI_Panel curPanel = GetComponentInParent<UI_Panel>();
        Managers.UI.ClosePopupUI(curPanel);
    }

    public void LoadLobby()
    {
        Managers.Scene.LoadScene(Define.Scene.Lobby);
        
        //Managers.Network.Call();
    }

    // ISelectHandler 인터페이스
    public void OnSelect(BaseEventData eventData)
    {
        GameObject go = Managers.Select.CurPanel;

        if (go == null) return;
        UI_Panel panel = go.GetComponent<UI_Panel>();

        if (panel == null) return;
        panel.SetArrowPos(eventData.selectedObject.transform);
    }

}
