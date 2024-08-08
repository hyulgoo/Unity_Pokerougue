using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_LobbyScene : UI_Scene
{
    Button[] buttons;

    public override void Init()
    {
        base.Init();

        buttons = GetComponentsInChildren<Button>();
    }

    public void SetUserName(string[] names)
    {
        for(int i = 0; i < buttons.Length; ++i)
        {
            string newtext = "";
            TMP_Text text = buttons[i].GetComponent<TMP_Text>();
            if (i < names.Length)
            {
                newtext = names[i];
                buttons[i].GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Left;
                buttons[i].interactable = true;
            }
            else
            {
                newtext = "ºó ½½·Ô";
                buttons[i].GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
                buttons[i].interactable = false;
            }
            text.text = newtext;
        }
    }
}
