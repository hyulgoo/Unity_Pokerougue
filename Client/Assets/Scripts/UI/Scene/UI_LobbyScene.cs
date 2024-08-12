using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_LobbyScene : UI_Scene
{
    Button[] _buttons;
    public int _selectbuttonid;
    public override void Init()
    {
        base.Init();

        _buttons = GetComponentsInChildren<Button>();
    }

    public void SetUserName(int[] id, string[] names)
    {
        for(int i = 0; i < _buttons.Length; ++i)
        {
            string newtext = "";
            TextMeshProUGUI text = _buttons[i].gameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (i < names.Length)
            {                
                newtext = names[i];
                text.alignment = TextAlignmentOptions.Left;
                _buttons[i].interactable = true;
                UI_PlayerButton pc = _buttons[i].gameObject.GetOrAddComponent<UI_PlayerButton>();
                pc.SetPlayerId(id[i]);
                _buttons[i].onClick.AddListener(OnClickPlayerButton);
            }
            else
            {
                newtext = "ºó ½½·Ô";
                text.alignment = TextAlignmentOptions.Center;
                _buttons[i].interactable = false;
            }
            text.text = newtext;
                _buttons[i].Select();
        }
    }

    public void OnClickPlayerButton()
    {
        UI_SelectPlayerPopup popup = Managers.UI.ShowPopupUI<UI_SelectPlayerPopup>();
        popup.SetOpponentPlayer(_selectbuttonid);
    }
}
