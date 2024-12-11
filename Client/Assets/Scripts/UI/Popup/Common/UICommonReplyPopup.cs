using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class UICommonReplyPopup : UICommonPopup
{
    protected enum Texts
    {
        Announce
    }
    protected enum Buttons
    {
        Btn_Apply,
        Btn_Deny
    }

    public override void Init()
    {
        base.Init();
        BindButton(typeof(Buttons));
        BindText(typeof(Texts));
        InputMode = false;

        GetButton((int)Buttons.Btn_Apply).onClick.AddListener(OnClickApplyButton);
        GetButton((int)Buttons.Btn_Deny).onClick.AddListener(OnClickDenyButton);
    }

    protected abstract void OnClickApplyButton();

    protected abstract void OnClickDenyButton();
}
