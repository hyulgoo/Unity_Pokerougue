using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISelectManager
{
    [SerializeField] GameObject _curSelectPanel = null;
    public GameObject CurPanel
    {
        get { return _curSelectPanel; }
        set
        {
            _curSelectPanel = value;
            SetPanel(value);
        }
    }

    Button _curSelectButton;
    Button[] _totButtoninPanel;
    
    void SetPanel(GameObject panel)
    {
        if (panel == null) return;

        _totButtoninPanel = panel.GetComponentsInChildren<Button>();
        if (_totButtoninPanel.Length <= 0)
        {
            _curSelectButton = null;
            return;
        }

        UICommonBase commmonBase = panel.GetComponent<UICommonBase>();
        if (commmonBase == null) return;

        if(commmonBase._lastSelected != null)
            _curSelectButton = commmonBase._lastSelected;
        else
            _curSelectButton = _totButtoninPanel[0];

        _curSelectButton.Select();
        commmonBase.SetPointerPos(_curSelectButton.transform);

        switch (commmonBase.InitSound)
        {
            case UICommonBase.InitSounds.None:
            break;
            case UICommonBase.InitSounds.Popup:
                Managers.Sound.Play("system/menu_open");
            break;
            case UICommonBase.InitSounds.Select:
                Managers.Sound.Play("system/select");
            break;
            case UICommonBase.InitSounds.Error:
                Managers.Sound.Play("system/error");
            break;
        }
    }
}
