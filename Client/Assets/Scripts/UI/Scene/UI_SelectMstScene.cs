using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SelectMstScene : UI_Scene
{
    enum Texts
    {
        Text_MstName,
        Text_MstNumber,
        Text_SkillName_0,
        Text_SkillName_1,
        Text_SkillName_2,
        Text_SkillName_3,
    }

    enum GameObjects
    {
        // 전체 몬스터 목록
        Content,

        // 현재 보고있는 몬스터의 스킬 및 이미지
        Image_Mst,
        Image_Skill_0,
        Image_Skill_1,
        Image_Skill_2,
        Image_Skill_3,

        // 선택된 몬스터 6마리
        Image_SelectedMst_0,
        Image_SelectedMst_1,
        Image_SelectedMst_2,
        Image_SelectedMst_3,
        Image_SelectedMst_4,
        Image_SelectedMst_5,
    }

    enum Buttons
    {
        Btn_Ready,
    }

    public override void Init()
    {
        base.Init();

        Bind<TMP_Text>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));
        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.Btn_Ready).onClick.AddListener(OnClickReadyButton);
    }

    void SetCurMstInfo(int pokemonNumber)
    {
        Animator curMstAnimator= GetObject((int)GameObjects.Image_Mst).GetOrAddComponent<Animator>();
        // 현재 포켓몬의 애니메이션을 틀어줌
        // curMstAnimator.Play("");
        PokemonData data = Managers.Data.MonsterDict[pokemonNumber];
        GetText((int)Texts.Text_MstNumber).text = $"{pokemonNumber}";
        GetText((int)Texts.Text_MstName).text = data.name;

        for(int i = (int)Texts.Text_SkillName_0; i < data.info.SkillId.Count; ++i)
        {
            SkillInfo info = Managers.Data.SkillDict[i];
            GetText(i).text = data.info.SkillId
        }
    }

    void OnClickReadyButton()
    {
        // 선택이 끝났으니 다음 UI 출력
    }
}
