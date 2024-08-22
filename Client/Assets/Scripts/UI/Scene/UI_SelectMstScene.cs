using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class UI_SelectMstScene : UI_Scene
{
    const int MaxCount = 3;
    int[] _pokemonList = new int[MaxCount];
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
            GameObject pokebtnobj = Managers.Resource.Instantiate("Add/Pokemon");
            pokebtnobj.transform.SetParent(GetObject((int)GameObjects.Content).transform);
            
            // 왠지 모르겠는데 Grid Layout Group에 들어가서 그런가 스케일이 1.15근처로 설정됨
            // 강제로 1로 맞춰줌.
            RectTransform rt = pokebtnobj.GetComponent<RectTransform>();
            rt.localScale = new Vector2(1, 1);

            UI_SelectPokemonButton selectbtn = pokebtnobj.GetComponent<UI_SelectPokemonButton>();
            selectbtn.Id = id;
            selectbtn.SelectMstScene = this;

            if (!first)
            {
                pokebtnobj.GetComponent<Button>().Select();
                pokebtnobj.GetComponent<UI_SelectPokemonButton>().SetParentCurMstInfo();
                first = true;
            }
        }
    }

    public bool SetMonster(int pokemonNum)
    {
        if (CurOrder >= MaxCount)
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
        Image curMstAnimator= GetObject((int)GameObjects.Image_Mst).GetOrAddComponent<Image>();
        // 현재 포켓몬의 애니메이션을 틀어줌
        Sprite[] sprites = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/{pokemonNumber}");
        curMstAnimator.sprite = sprites[0];

        // 현재 포켓몬의 정보를 나타냄
        PokemonData data = Managers.Data.MonsterDict[pokemonNumber];
        GetText((int)Texts.Text_MstNumber).text = $"{pokemonNumber}";
        GetText((int)Texts.Text_MstName).text = data.name;
        sprites = Managers.Resource.LoadAll<Sprite>("Sprite/ui/type_bgs");
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
                GetText(i).text = skilldata.name;
                int type = (int)skilldata.info.SkillEffect[0].Type;
                string spriteName = "type_bgs_" + type.ToString();
                Sprite typesprite = System.Array.Find(sprites, sprite => sprite.name == spriteName);
                skillImage.sprite = typesprite;
                skillImage.type = Image.Type.Sliced;
                skillImage.color = Color.white;
            }
        }           
    }

    void OnClickReadyButton()
    {
        // 서버로 내가 고른 list를 보내줌.
        C_SelectMst packet = new C_SelectMst();
        packet.MstList = ChangePokemonListToMstList();
        packet.PlayerId = Managers.Object.MyPlayer.Id;

        Managers.Network.Send(packet);

        Managers.UI.ShowPopupUI<UI_WaitingForRespondPopup>();
    }    

    PokemonList ChangePokemonListToMstList()
    {
        PokemonList list = new PokemonList();
        for (int i = 0; i < MaxCount; ++i)
        {
            switch (i)
            {
                case 0:
                    list.Pokemon0 = _pokemonList[i];
                    break;
                case 1:
                    list.Pokemon1 = _pokemonList[i];
                    break;
                case 2:
                    list.Pokemon2 = _pokemonList[i];
                    break;
                case 3:
                    list.Pokemon3 = _pokemonList[i];
                    break;
                case 4:
                    list.Pokemon4 = _pokemonList[i];
                    break;
                case 5:
                    list.Pokemon5 = _pokemonList[i];
                    break;
            }
        }
        return list;        
    }
}
