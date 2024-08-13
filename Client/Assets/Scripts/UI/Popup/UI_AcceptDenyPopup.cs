using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_AcceptDenyPopup : UI_Popup
{
    protected enum Texts
    {
        Text
    }
    protected enum Buttons
    {
        Btn_Apply,
        Btn_Deny
    }

    public override void Init()
    {
        base.Init();
        Bind<Button>(typeof(Buttons));
        Bind<TMP_Text>(typeof(Texts));

        GetButton((int)Buttons.Btn_Apply).onClick.AddListener(OnClickApplyButton);
        GetButton((int)Buttons.Btn_Deny).onClick.AddListener(OnClickDenyButton);
    }

    // 수락버튼에 바인딩 된 가상함수
    protected virtual void OnClickApplyButton() { }

    // 거절버튼에 바인딩 된 가상함수
    protected virtual void OnClickDenyButton() { }
}
