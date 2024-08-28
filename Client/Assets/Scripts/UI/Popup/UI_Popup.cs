using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Popup : UI_Base
{
    public bool InputMode { get; set; }
    public override void Init()
    {
        Managers.UI.SetCanvas(gameObject, true);
    }

    protected virtual void Update()
    {
        if(Input.GetKeyDown(KeyCode.Backspace) && !InputMode)
        {
            ClosePopupUI();
        }
    }

    public virtual void ClosePopupUI()
    {
        Managers.UI.ClosePopupUI(this);
    }
}
