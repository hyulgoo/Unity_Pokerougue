using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UILobbyScene : UICommonScene
{
    Button[] _buttons;
    public int _selectbuttonid;
    public override void Init()
    {
        base.Init();

        _buttons = GetComponentsInChildren<Button>();
    }

    // 전체 유저의 이름 목록을 띄움.
    // 유저 수만큼의 버튼만 활성화
    public void SetUserName(int[] id, string[] names)
    {
        for(int index = 0; index < _buttons.Length; ++index)
        {
            string newtext = string.Empty;
            TextMeshProUGUI text = _buttons[index].gameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (index < names.Length)
            {
                newtext = names[index];
                text.alignment = TextAlignmentOptions.Left;
                _buttons[index].interactable = true;
                UILobbyPlayerButton pc = _buttons[index].gameObject.GetOrAddComponent<UILobbyPlayerButton>();
                pc.SetPlayerId(id[index]);
                _buttons[index].onClick.AddListener(OnClickPlayerButton);
                _buttons[index].Select();
            }
            else
            {
                newtext = "빈 슬롯";
                text.alignment = TextAlignmentOptions.Center;
                _buttons[index].interactable = false;
            }

            text.text = newtext;
        }
    }

    public void OnClickPlayerButton()
    {
        if (_selectbuttonid == Managers.Player.MyPlayer.Id)
        {
            UICommonAnnouncePopup announcePopup = Managers.UI.ShowPopupUI<UICommonAnnouncePopup>();
            announcePopup.SetAnnounceText("자신에게는 신청할 수 없습니다!");
            return;
        }

        UILobbyPlayerSelectPopup popup = Managers.UI.ShowPopupUI<UILobbyPlayerSelectPopup>("UICommonRespondPopup");
        popup.SetEnemyPlayer(_selectbuttonid);
        popup.InitSound = InitSounds.Select;
    }
}
