using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICommonPopup : UICommonBase
{
    public bool InputMode { get; set; } = false;
    public override void Init()
    {
        Managers.UI.SetCanvas(gameObject, true);
    }

    protected virtual void Update()
    {
        if(Input.GetKeyDown(KeyCode.Backspace) && !InputMode)
            ClosePopupUI();
    }

    public virtual void ClosePopupUI()
    {
        Managers.UI.ClosePopupUI(this);
    }
}
