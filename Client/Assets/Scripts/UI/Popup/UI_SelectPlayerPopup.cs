using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           
using UnityEngine;
using UnityEngine.UI;

public class UI_SelectPlayerPopup : UI_AcceptDenyPopup
{
    int _oponentId;
    public override void Init()
    {
        base.Init();
    }

    public void SetOpponentPlayer(int playerid)
    {
        _oponentId = playerid;
        string playerName = Managers.Object.GetPlayerName(playerid);
        playerName += "님에게 \n 대결 신청?";

        TMP_Text textbtn = GetText((int)Texts.Text);
        textbtn.text = playerName;
    }

    protected override void OnClickApplyButton()
    {
        C_RequestDuel requestduelpacket = new C_RequestDuel();
        requestduelpacket.ApplyId = Managers.Object.MyPlayer.Id;
        requestduelpacket.OpponentId = _oponentId;
        Managers.Network.Send(requestduelpacket);

        ClosePopupUI();

        UI_WaitingForRespondPopup popup = Managers.UI.ShowPopupUI<UI_WaitingForRespondPopup>();
        popup.Text = "서버 응답 대기 중";
    }

    protected override void OnClickDenyButton()
    {
        ClosePopupUI();
    }
}
