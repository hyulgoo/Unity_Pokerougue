using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEditor;
using UnityEngine;

public class UICommonWaitPopup : UICommonPopup
{
    public string Text { set { _text = value; } }
    string _text;

    const float _updateDelay = 0.7f;
    int _currentDotCount = 0;
    const int _maxDotCount = 3;

    enum Texts
    {
         Announce
    }

    // 대기 중일 때는 창을 끄거나 다른 행동을 할 수 없음.
    public override void Init()
    {
        base.Init();

        BindText(typeof(Texts));
        InputMode = false;
        StartCoroutine(SetWaitingText(_updateDelay));
    }

    IEnumerator SetWaitingText(float delay)
    {
        // . -> .. -> ... -> . 순서로 _text 뒤에 붙도록 함
        _currentDotCount = _currentDotCount == _maxDotCount ? 1 : _currentDotCount + 1;
        string text = _text;
        for (int index = 0; index < _currentDotCount; index++)
            text += ".";
        GetText((int)Texts.Announce).text = text;
        yield return new WaitForSeconds(delay);

        yield return StartCoroutine(SetWaitingText(delay));
    }    
}
