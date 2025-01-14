using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBattleSkillSelectPopup : UICommonPopup
{
    int _selectedSkillId = Define.InValidNumber;

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

        for(int i = (int)Buttons.Btn_Skill_00; i <= (int)Buttons.Btn_Skill_03; ++i)
            GetButton(i).onClick.AddListener(OnClickSkillButton);
              
        SetSkillInfo();
    }

    protected override void Update()
    {
        if (Input.GetKey(KeyCode.Backspace) == false)
            return;

        ClosePopupUI();
        Managers.UI.ShowPopupUI<UIBattleBehaviorSelectPopup>();
    }

    void SetSkillInfo()
    {
        for (int index = (int)Texts.Text_Skill_00; index <= (int)Texts.Text_Skill_03; ++index)
        {
            int skillId = Managers.Player.MyPlayer.GetCurPokemonData().Info.SkillId[index];
            string name = Managers.Data.SkillDict[skillId].name;

            TMPro.TMP_Text skillNameText = GetText(index);
            skillNameText.text = name;

            int currentPokemonId = Managers.Player.MyPlayer.GetCurPokemonData().Id;
            SkillInfo info = Managers.Data.SkillDict[skillId].info;
            int currentSkillPP = Managers.Player.MyPlayer.SkillPP[currentPokemonId][skillId];

            Button curSkillButton = GetButton(index);
            curSkillButton.interactable = currentSkillPP != 0;

            UIBattleSkillSelectButton skillButton = GetButton(index).gameObject.GetOrAddComponent<UIBattleSkillSelectButton>();
            skillButton.SkillSelectPopup = this;
            skillButton.SkillId = skillId;
        }
    }

    public void SetSelectSkillInfo(int skillId)
    {
        _selectedSkillId = skillId;

        // 이미지 설정
        Sprite[] spritesArray; 
        int skillType;

        const string typebgSpriteName = "Sprite/ui/type_bgs";
        spritesArray = Managers.Resource.LoadAll<Sprite>(typebgSpriteName);
        if (spritesArray == null)
        {
            Debug.Assert(false, $"Cannot Found {typebgSpriteName}");
        }

        skillType = (int)Managers.Data.SkillDict[skillId].info.SkillEffect[0].Type;
        GetImage((int)Images.Image_SkillAttribute).sprite = spritesArray[skillType];
        GetText((int)Texts.Text_SkillAttribute).text = Util.GetTypeName((Type)skillType);

        const string categorySpriteName = "Sprite/categories_legacy";
        spritesArray = Managers.Resource.LoadAll<Sprite>(categorySpriteName);
        if (spritesArray == null)
        {
            Debug.Assert(false, $"Cannot Found {categorySpriteName}");
        }

        ApplyType applyType = Managers.Data.SkillDict[skillId].info.SkillEffect[0].ApplyType;
        switch (applyType)
        {
            case ApplyType.Atk:
                skillType = 0;
                break;
            case ApplyType.Spa:
                skillType = 1;
                break;
            default:
                skillType = 2;
                break;
        }

        GetImage((int)Images.Image_SkillType).sprite = spritesArray[skillType];

        // 세부 정보
        int currentPokemonId = Managers.Player.MyPlayer.GetCurPokemonData().Id;
        SkillInfo info = Managers.Data.SkillDict[skillId].info;
        int currentSkillPP = Managers.Player.MyPlayer.SkillPP[currentPokemonId][skillId];

        GetText((int)Texts.Text_SkillPP).text = $"{currentSkillPP}/{info.Pp}";

        // 공격 스킬이 아닐 경우에는 위력을 -으로 설정함
        GetText((int)Texts.Text_SkillPower).text = (applyType == ApplyType.Atk || applyType == ApplyType.Spa) ? $"{info.SkillEffect[0].Value}" : "-";
        GetText((int)Texts.Text_SkillAccuracy).text = $"{info.SkillEffect[0].Accuracy}";
    }

    void OnClickSkillButton()
    {
        if (_selectedSkillId == Define.InValidNumber)
            Debug.Assert(false, "SelectedSkillId is InValid");

        C_Turn turnpacket = new C_Turn();
        turnpacket.PlayerId = Managers.Player.MyPlayer.Id;

        TurnInfo turnInfo = new TurnInfo();
        turnInfo.Action = ActionType.Fight;
        turnInfo.SkillId = _selectedSkillId;
        
        turnpacket.TurnInfo = turnInfo;

        Managers.Network.Send(turnpacket);
        Managers.UI.CloseAllPopupUI();
        UIBattleScene scene = Managers.UI.SceneUI.gameObject.GetComponent<UIBattleScene>();
    }
}
