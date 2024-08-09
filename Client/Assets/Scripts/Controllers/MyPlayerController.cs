using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyPlayerController : PlayerController
{
    public bool _inputMode = false;

    private void Start()
    {
        Managers.Object.MyPlayer = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Backspace) && !_inputMode)
            Managers.UI.ClosePopupUI();
    }
}
