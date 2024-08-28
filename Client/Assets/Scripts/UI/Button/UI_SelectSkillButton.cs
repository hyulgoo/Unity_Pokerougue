using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_SelectSkillButton : UI_Button, ISelectHandler
{
    public int SkillId { get; set; }
    public UI_SkillSelectPopup SkillSelectPopup { get; set; }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        SkillSelectPopup.SetCurSkillInfo(SkillId);
    }
}
