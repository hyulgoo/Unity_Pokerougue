using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_AnnouncePopup : UI_Popup
{
    enum Texts
    {
        Announce
    }

    enum Buttons
    {
        Btn_Accept
    }

    public override void Init()
    {
        base.Init();

        Bind<TMP_Text>(typeof(Texts));            
        Bind<Button>(typeof(Buttons));
        GetButton((int)Buttons.Btn_Accept).onClick.AddListener(OnClickAcceptButton);
    }

    public void SetAnnounceText(string text)
    {
        GetText((int)Texts.Announce).text = text;
    }

    public void OnClickAcceptButton()
    {
        ClosePopupUI();
    }
}
