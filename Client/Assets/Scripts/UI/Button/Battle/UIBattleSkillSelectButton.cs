using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIBattleSkillSelectButton : UICommonButton
{
    public int SkillId { get; set; }
    public UIBattleSkillSelectPopup SkillSelectPopup { get; set; }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);

        SkillSelectPopup.SetSelectSkillInfo(SkillId);
    }
}
