using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_Panel : UI_Popup    
{
    public GameObject _select;

    // 현재 선택된 버튼을 가리키는 Obj PosSetting
    public void SetArrowPos(Transform parent)
    {
        if (_select != null)
        {
            _select.transform.SetParent(parent);
            RectTransform selectrect = _select.GetComponent<RectTransform>();
            selectrect.offsetMin = Vector2.zero;
            selectrect.offsetMax = Vector2.zero;
        }
    }
}
