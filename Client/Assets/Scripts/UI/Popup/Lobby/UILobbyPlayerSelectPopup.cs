using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           
using UnityEngine;
using UnityEngine.UI;

public class UILobbyPlayerSelectPopup : UICommonReplyPopup
{
    int _enemyId;

    public override void Init()
    {
        base.Init();

        GetButton((int)Buttons.Btn_Apply).gameObject.GetComponentInChildren<TMP_Text>().text = "신청";
        GetButton((int)Buttons.Btn_Deny).gameObject.GetComponentInChildren<TMP_Text>().text = "취소";
    }

    public void SetEnemyPlayer(int playerid)
    {
        _enemyId = playerid;
        string playerName = Managers.Player.GetPlayerName(playerid);
        playerName += "님에게 \n 대결 신청?";

        TMP_Text textbtn = GetText((int)Texts.Announce);
        textbtn.text = playerName;
    }

    protected override void OnClickApplyButton()
    {
        C_RequestDuel requestduelpacket = new C_RequestDuel();
        requestduelpacket.FromId = Managers.Player.MyPlayer.Id;
        requestduelpacket.ToId = _enemyId;
        Managers.Network.Send(requestduelpacket);

        ClosePopupUI();

        UICommonWaitPopup popup = Managers.UI.ShowPopupUI<UICommonWaitPopup>();
        popup.Text = "서버 응답 대기 중";
    }

    protected override void OnClickDenyButton()
    {
        ClosePopupUI();
    }
}
