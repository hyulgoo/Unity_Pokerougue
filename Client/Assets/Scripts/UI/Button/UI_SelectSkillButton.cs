using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_SelectSkillButton : UI_Base, ISelectHandler
{
    public int SkillId { get; set; }
    public UI_SkillSelectPopup SkillSelectPopup { get; set; }

    public override void Init()
    {        
    }

    public void OnSelect(BaseEventData eventData)
    {
        SkillSelectPopup.SetCurSkillInfo(SkillId);
    }
}
