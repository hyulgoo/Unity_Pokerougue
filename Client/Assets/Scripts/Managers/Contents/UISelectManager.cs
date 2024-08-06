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
        if (_totButtoninPanel.Length > 0)
        {
            _curSelectButton = _totButtoninPanel[0];
            _curSelectButton.Select();
            UI_Panel uipanel = panel.GetComponent<UI_Panel>();
            if(uipanel != null)
            {
                uipanel.SetArrowPos(_curSelectButton.transform);
            }
        }
        else
        { 
            _curSelectButton = null;
        }
    }
}
