using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class UI_MainScene : UI_Scene
{
    enum Buttons
    {
        Btn_Start,
        Btn_Setting,
    }

    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));
        Managers.Select.CurPanel = gameObject;

        GetButton((int)Buttons.Btn_Start).onClick.AddListener(OnClickStartButton);
        GetButton((int)Buttons.Btn_Setting).onClick.AddListener(OnClickSettingButton);
    }

    public void OnClickStartButton()
    {
        Managers.UI.ShowPopupUI<UI_LoginPopup>();
    }

    public void OnClickSettingButton()
    {
        Managers.UI.ShowPopupUI<UI_SettingPopup>();
    }
}
