using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBattleSkillSelectPopup : UICommonPopup
{
    int _selectedSkillId {  get; set; }

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
        if(Input.GetKey(KeyCode.Backspace))
        {
            ClosePopupUI();
            Managers.UI.ShowPopupUI<UIBattleBehaviorSelectPopup>();
        }
    }

    void SetSkillInfo()
    {
        int curpkmId = Managers.Player.MyPlayer.GetCurPokemonData().Id;

        for (int i = (int)Texts.Text_Skill_00; i <= (int)Texts.Text_Skill_03; ++i)
        {
            int skillId = Managers.Player.MyPlayer.GetCurPokemonData().Info.SkillId[i];
            string name = Managers.Data.SkillDict[skillId].name;
            // 스킬 이름 설정
            GetText(i).text = name;
            // 버튼에 스크립트 넣고 멤버로 info 넣어주기.
            UIBattleSkillSelectButton button = GetButton(i).gameObject.GetOrAddComponent<UIBattleSkillSelectButton>();
            button.SkillSelectPopup = this;
            button.SkillId = skillId;
        }
    }

    public void SetCurSkillInfo(int skillId)
    {
        _selectedSkillId = skillId;

        // 이미지 설정
        Sprite[] sprites; int type;
        sprites = Managers.Resource.LoadAll<Sprite>("Sprite/ui/type_bgs");
        type = (int)Managers.Data.SkillDict[skillId].info.SkillEffect[0].Type;
        GetImage((int)Images.Image_SkillAttribute).sprite = sprites[type];
        GetText((int)Texts.Text_SkillAttribute).text = Util.GetTypeName((Type)type);

        sprites = Managers.Resource.LoadAll<Sprite>("Sprite/categories_legacy");
        ApplyType category = Managers.Data.SkillDict[skillId].info.SkillEffect[0].ApplyType;

        switch (category)
        {
            case ApplyType.Atk:
                type = 0;
                break;
            case ApplyType.Spa:
                type = 1;
                break;
            default:
                type = 2;
                break;
        }

        GetImage((int)Images.Image_SkillType).sprite = sprites[type];

        // 세부 정보
        int pokemonid = Managers.Player.MyPlayer.GetCurPokemonData().Id;
        SkillInfo info = Managers.Data.SkillDict[skillId].info;
        int curpp = Managers.Player.MyPlayer.SkillPP[pokemonid][skillId];

        GetText((int)Texts.Text_SkillPP).text = $"{curpp}/{info.Pp}";

        // 공격 스킬이 아닐 경우에는 위력을 0으로 설정함
        if(category == ApplyType.Atk || category == ApplyType.Spa )
            GetText((int)Texts.Text_SkillPower).text = $"{info.SkillEffect[0].Value}";
        else
            GetText((int)Texts.Text_SkillPower).text = "0";

        GetText((int)Texts.Text_SkillAccuracy).text = $"{info.SkillEffect[0].Accuracy}";
    }

    void OnClickSkillButton()
    {
        C_Turn turnpacket = new C_Turn();
        turnpacket.PlayerId = Managers.Player.MyPlayer.Id;

        TurnInfo turnInfo = new TurnInfo();
        turnInfo.Action = ActionType.Fight;
        turnInfo.SkillNum = _selectedSkillId;
        
        turnpacket.TurnInfo = turnInfo;

        Managers.Network.Send(turnpacket);
        Managers.UI.CloseAllPopupUI();
        UIBattleScene scene = Managers.UI.SceneUI.gameObject.GetComponent<UIBattleScene>();
        string monsterName = Managers.Player.MyPlayer.GetCurPokemonName();
        string skillName = Managers.Data.SkillDict[_selectedSkillId].name;
        Managers.Job.Push(() => scene.SetAnnounce($"{monsterName}의 {skillName}!", true));
    }
}
