using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class UIMainScene : UICommonScene
{
    enum Buttons
    {
        Btn_Start,
        Btn_Setting,
    }

    public override void Init()
    {
        base.Init();

        BindButton(typeof(Buttons));

        GetButton((int)Buttons.Btn_Start).onClick.AddListener(OnClickStartButton);
        GetButton((int)Buttons.Btn_Setting).onClick.AddListener(OnClickSettingButton);
    }

    public void OnClickStartButton()
    {
        Managers.UI.ShowPopupUI<UIMainLoginPopup>();
    }

    public void OnClickSettingButton()
    {
        Managers.UI.ShowPopupUI<UISettingPopup>();
    }
}
