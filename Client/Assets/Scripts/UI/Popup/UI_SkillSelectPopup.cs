using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

public class UI_SkillSelectPopup : UI_Popup
{
    enum Buttons
    {
        Btn_Skill_00 = 0,
        Btn_Skill_01 = 1,
        Btn_Skill_02 = 2,
        Btn_Skill_03 = 3,
    }

    enum Texts
    {
        Text_Skill_00 = 0,
        Text_Skill_01 = 1,
        Text_Skill_02 = 2,
        Text_Skill_03 = 3,
        Text_SkillAttribute,
        Text_SkillPP,
        Text_SkillPower,
        Text_SkillAccuracy,
    }

    enum Images
    {
        Image_SkillAttribute,
        Image_SkillType,
    }

    public override void Init()
    {
        base.Init();

        BindButton(typeof(Buttons));
        BindImage(typeof(Images));
        BindText(typeof(Texts));
    }

    void SetSkillInfo(int skillId)
    {
        for(int i = 0; i < 4; ++i)
        {
            int id = Managers.Object.MyPlayer.GetCurMonsterInfo().SkillId[i];
            Data.SkillData info = Managers.Data.SkillDict[id];

            // 스킬 이름 설정
            GetText(i).text = info.name;
            // 버튼에 스크립트 넣고 멤버로 info 넣어주기.
            // GetButton(i).
        }
    }
}
