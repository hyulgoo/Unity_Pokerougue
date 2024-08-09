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
            TextMeshProUGUI text = buttons[i].gameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (i < names.Length)
            {
                newtext = names[i];
                text.alignment = TextAlignmentOptions.Left;
                buttons[i].interactable = true;
            }
            else
            {
                newtext = "ºó ½½·Ô";
                text.alignment = TextAlignmentOptions.Center;
                buttons[i].interactable = false;
            }
            text.text = newtext;
        }
    }
}
