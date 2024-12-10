using Google.Protobuf.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIBattlePokemonChangePopup : UICommonPopup
{    
    enum Images
    {
        Image_Pokemon00,
        Image_Pokemon01,
        Image_Pokemon02,
        Image_Pokemon03,
        Image_Pokemon04,
        Image_Pokemon05,

        Image_HPBar00,
        Image_HPBar01,
        Image_HPBar02,
        Image_HPBar03,
        Image_HPBar04,
        Image_HPBar05,
    }

    enum Texts
    {
        Text_HP00,
        Text_HP01,
        Text_HP02,
        Text_HP03,
        Text_HP04,
        Text_HP05,

        Text_PokemonName00,
        Text_PokemonName01,
        Text_PokemonName02,
        Text_PokemonName03,
        Text_PokemonName04,
        Text_PokemonName05,

        Text_PokemonLevel00,
        Text_PokemonLevel01,
        Text_PokemonLevel02,
        Text_PokemonLevel03,
        Text_PokemonLevel04,
        Text_PokemonLevel05,
    }

    enum Sliders
    {
        Slider_HPBar00,
        Slider_HPBar01,
        Slider_HPBar02,
        Slider_HPBar03,
        Slider_HPBar04,
        Slider_HPBar05,
    }

    public override void Init()
    {
        base.Init();

        BindImage(typeof(Images));
        BindText(typeof(Texts));
        Bind<Slider>(typeof(Sliders));
        SetPokemonBtnList();
    }

    private void SetPokemonBtnList()
    {
        PokemonData[] pokemonDataList = Managers.Player.MyPlayer.GetAllPokemonData();
        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        if (battleScene == null)
        {
            Debug.Log("BattleScene Not Found");
            return;
        }

        for (int index = 0; index < Define.PokemonMaxCount; index++)
        {
            Image pokemonImage = GetImage((int)index);
            TMP_Text pokemonHpText = GetText((int)index);
            TMP_Text pokemonNameText = GetText((int)index + Define.PokemonMaxCount);
            TMP_Text pokemonLevelText = GetText((int)index + Define.PokemonMaxCount * 2);
            Slider hpSlider = Get<Slider>((int)index);
            GameObject hpSliderObject = hpSlider.gameObject;

            if (index < pokemonDataList.Length)
            {
                // Image
                Sprite curPokemonSprite = Managers.Resource.Load<Sprite>($"Sprite/pokemon/icons/{pokemonDataList[index].Id}");
                pokemonImage.sprite = curPokemonSprite;
                pokemonImage.color = Color.white;

                // Texts
                int curHp = pokemonDataList[index].Info.Hp;
                int maxHp = Managers.Data.PokeonDict[pokemonDataList[index].Id].Info.Hp;

                pokemonHpText.text = curHp.ToString();
                pokemonHpText.text += "/" + maxHp.ToString();
                pokemonNameText.text = pokemonDataList[index].Name;
                pokemonLevelText.text = pokemonDataList[index].Info.Level.ToString();

                // Slider            
                float hpRatio = (float)curHp / (float)maxHp;
                hpSlider.value = hpRatio;
                int hpstate = 2 - (int)(hpRatio / 0.34f);

                Image fillObj = Util.FindChild<Image>(hpSliderObject, "Fill", true);
                fillObj.sprite = battleScene.HpSprites[hpstate];
            }
            else
            {
                pokemonImage.color = Color.clear;
                pokemonHpText.text = "";
                pokemonNameText.text = "";
                pokemonLevelText.text = "";
                hpSlider.value = 0f;
            }
        }
    }
}
