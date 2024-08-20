using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class UI_SelectMstScene : UI_Scene
{
    int[] _pokemonList = new int[6];
    public int CurOrder { get; set; } = 0;

    enum Texts
    {
        Text_SkillName_0,
        Text_SkillName_1,
        Text_SkillName_2,
        Text_SkillName_3,
        Text_MstName,
        Text_MstNumber,
    }

    enum GameObjects
    {
        // 전체 몬스터 목록
        Scroll_Rect,
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

        float width = GetObject((int)GameObjects.Scroll_Rect).GetComponent<RectTransform>().rect.width;
        width *= 0.95f;
        GetObject((int)GameObjects.Content).GetComponent<GridLayoutGroup>().cellSize = new Vector2(width / 5, width / 5);

        SetMonsterList();
    }

    void SetMonsterList()
    {
        bool first = false;

        foreach (int id in Managers.Data.MonsterDict.Keys)
        {
            // 몬스터 아이콘을 넣어줌.
            GameObject pokebtn = Managers.Resource.Instantiate("Add/Pokemon");
            pokebtn.transform.SetParent(GetObject((int)GameObjects.Content).transform);
            RectTransform rt = pokebtn.GetComponent<RectTransform>();
            rt.localScale = new Vector2(1, 1);

            UI_SelectPokemonButton selectbtn = pokebtn.GetComponent<UI_SelectPokemonButton>();
            selectbtn.Id = id;
            selectbtn.SelectMstScene = this;

            if (!first)
            {
                pokebtn.GetComponent<Button>().Select();
                first = true;
            }
        }
    }

    public bool SetMonster(int pokemonNum)
    {
        if (CurOrder >= 6)
            return false;
        _pokemonList[CurOrder] = pokemonNum;
        int order = (int)GameObjects.Image_SelectedMst_0 + CurOrder;
        CurOrder++;

        Image iconImg = GetObject(order).GetComponent<Image>();
        Sprite curMstSprite = Managers.Resource.Load<Sprite>($"Sprite/pokemon/icons/{pokemonNum}");
        iconImg.sprite = curMstSprite;
        return true;
    }

    public void SetCurMstInfo(int pokemonNumber)
    {
        Animator curMstAnimator= GetObject((int)GameObjects.Image_Mst).GetOrAddComponent<Animator>();
        // 현재 포켓몬의 애니메이션을 틀어줌
        // curMstAnimator.Play("");

        // 현재 포켓몬의 정보를 나타냄
        PokemonData data = Managers.Data.MonsterDict[pokemonNumber];
        GetText((int)Texts.Text_MstNumber).text = $"{pokemonNumber}";
        GetText((int)Texts.Text_MstName).text = data.name;
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprite/ui/type_bgs");
        for (int i = 0; i < data.info.SkillId.Count; ++i)
        {
            SkillData skilldata = Managers.Data.SkillDict[data.info.SkillId[i]];
            Image skillImage = GetText(i).gameObject.GetComponentInParent<Image>();
            if (skilldata == null)
            {
                skillImage.color = Color.clear;
                break;
            }
            else
            {
                skillImage.color = Color.white; 
                GetText(i).text = skilldata.name;
                int type = (int)skilldata.info.SkillEffect[0].Type;
                string spriteName = "type_bgs_" + type.ToString();
                Sprite typesprite = System.Array.Find(sprites, sprite => sprite.name == spriteName);
                skillImage.sprite = typesprite;
                skillImage.type = Image.Type.Sliced;
            }
        }           
    }

    void OnClickReadyButton()
    {
        // 선택이 끝났으니 다음 UI 출력
    }
}
