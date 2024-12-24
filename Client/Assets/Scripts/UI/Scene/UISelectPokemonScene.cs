using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class UISelectPokemonScene : UICommonScene
{
    int _maxPokemonCount = 1;
    public int MaxCount { set { _maxPokemonCount = value; } }
    List<int> _pokemonList = new List<int>();
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

        BindText(typeof(Texts));
        BindObject(typeof(GameObjects));
        BindButton(typeof(Buttons));

        GetButton((int)Buttons.Btn_Ready).onClick.AddListener(OnClickReadyButton);

        float width = GetObject((int)GameObjects.Scroll_Rect).GetComponent<RectTransform>().rect.width;
        width *= 0.95f;
        GetObject((int)GameObjects.Content).GetComponent<GridLayoutGroup>().cellSize = new Vector2(width / 5, width / 5);

        SetPokemonList();
    }

    void SetPokemonList()
    {
        bool isFirst = false;

        foreach (int id in Managers.Data.PokeonDict.Keys)
        {
            GameObject pokemonButtonObject = Managers.Resource.Instantiate("Add/Pokemon");
            pokemonButtonObject.transform.SetParent(GetObject((int)GameObjects.Content).transform);
            
            // 왠지 모르겠는데 Grid Layout Group에 들어가서 그런가 스케일이 1.15근처로 설정됨
            // 강제로 1로 맞춰줌.
            RectTransform rt = pokemonButtonObject.GetComponent<RectTransform>();
            rt.localScale = new Vector2(1, 1);

            UISelectPokemonSelectButton selectbtn = pokemonButtonObject.GetComponent<UISelectPokemonSelectButton>();
            selectbtn.Id = id;
            selectbtn.SelectScene = this;

            if (isFirst == false)
            {
                pokemonButtonObject.GetComponent<Button>().Select();
                pokemonButtonObject.GetComponent<UISelectPokemonSelectButton>().SetSceneCurrentSelectPokemonInfo();
                isFirst = true;
            }
        }
    }

    public bool PickPokemon(int pokemonNum)
    {
        if (CurOrder >= _maxPokemonCount)
            return false;

        _pokemonList.Add(pokemonNum);
        int order = (int)GameObjects.Image_SelectedMst_0 + CurOrder;
        CurOrder++;

        Image iconImg = GetObject(order).GetComponent<Image>();
        Sprite curMstSprite = Managers.Resource.Load<Sprite>($"Sprite/pokemon/icons/{pokemonNum}");
        iconImg.sprite = curMstSprite;
        return true;
    }

    public void SetCurrentSelectPokemonInfo(int pokemonNumber)
    {
        Image curMstAnimator= GetObject((int)GameObjects.Image_Mst).GetOrAddComponent<Image>();

        // TODO : 현재 포켓몬의 애니메이션을 틀어줌
        Sprite[] sprites = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/{pokemonNumber}");
        curMstAnimator.sprite = sprites[0];

        // 현재 포켓몬의 정보를 나타냄
        PokemonData data = Managers.Data.PokeonDict[pokemonNumber];
        GetText((int)Texts.Text_MstNumber).text = $"{pokemonNumber}";
        GetText((int)Texts.Text_MstName).text = data.Name;
        sprites = Managers.Resource.LoadAll<Sprite>("Sprite/ui/type_bgs");
        for (int i = 0; i < data.Info.SkillId.Count; ++i)
        {
            SkillData skilldata = Managers.Data.SkillDict[data.Info.SkillId[i]];
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
        if (_pokemonList.Count != _maxPokemonCount)
        {
            UICommonAnnouncePopup Announcepopup = Managers.UI.ShowPopupUI<UICommonAnnouncePopup>();
            Announcepopup.SetAnnounceText("아직 포켓몬을 전부 선택하지 않았습니다!");
            return;
        }

        // 서버로 내가 고른 list를 보내줌.
        C_SelectPokemon packet = new C_SelectPokemon();
        packet.PlayerId = Managers.Player.MyPlayer.Id;

        foreach (int pokemonNumber in _pokemonList)
            packet.PokemonList.Add(pokemonNumber);

        Managers.Network.Send(packet);

        UICommonWaitPopup popup = Managers.UI.ShowPopupUI<UICommonWaitPopup>();
        popup.Text = "상대방을 기다리는 중";
    }    
}
