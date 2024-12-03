using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEditor;
using UnityEngine;

public class UICommonWaitPopup : UICommonPopup
{
    public string Text { get; set; }

    float _updateDelay = 0.7f;
    int _curCount = 0;

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

        StartCoroutine(ChangeText(_updateDelay));
    }

    IEnumerator ChangeText(float delay)
    {
        // . -> .. -> ... -> . 순서로 _text 뒤에 붙도록 함
        _curCount = _curCount == 3 ? 1 : _curCount + 1;
        string text = Text;
        for (int i = 0; i < _curCount; i++)
            text += ".";
        GetText((int)Texts.Announce).text = text;
        yield return new WaitForSeconds(delay);

        yield return StartCoroutine(ChangeText(delay));
    }    
}
