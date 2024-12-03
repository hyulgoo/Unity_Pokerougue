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
            UICommonBase bc = panel.GetComponent<UICommonBase>();
            if (bc == null) return;

            if(bc._lastSelected != null)
            {
                _curSelectButton = bc._lastSelected;
            }
            else
            {
                _curSelectButton = _totButtoninPanel[0];
            }
            _curSelectButton.Select();
            bc.SetPointerPos(_curSelectButton.transform);
        }
        else
        { 
            _curSelectButton = null;
        }
    }
}
