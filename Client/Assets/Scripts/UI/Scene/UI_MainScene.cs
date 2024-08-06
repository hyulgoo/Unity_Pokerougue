using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class UI_MainScene : UI_Scene
{
    public string _defalutPanelName;
    public override void Init()
    {
        base.Init();
        Managers.UI.SceneUI = this;
        Managers.UI.ShowPopupUI<UI_Panel>(_defalutPanelName);
    }
}
