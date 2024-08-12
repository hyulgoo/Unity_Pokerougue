using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_SelectPlayerPopup : UI_Popup
{
    int OpponentId;
    enum Texts
    {
        Text
    }
    enum Buttons
    {
        Btn_Apply,
        Btn_Cancle
    }

    public override void Init()
    {
        base.Init();
        Bind<Button>(typeof(Buttons));
        Bind<Text>(typeof(Texts));

        GetButton((int)Buttons.Btn_Apply).onClick.AddListener(OnClickApplyButton);
        GetButton((int)Buttons.Btn_Cancle).onClick.AddListener(OnClickCancleButton);
    }

    public void SetOpponentPlayer(int playerid)
    {
        string playerName = Managers.Object.GetPlayerName(playerid);
        playerName += "님에게 \n 대결 신청?";
        GetText((int)Texts.Text).text = playerName;
    }

    void OnClickApplyButton()
    {

    }

    void OnClickCancleButton()
    {
        ClosePopupUI();
    }
}
