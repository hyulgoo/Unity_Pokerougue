using Google.Protobuf.Protocol;
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
        Text_PokemonName00,
        Text_PokemonLevel00,

        Text_HP01,
        Text_PokemonName01,
        Text_PokemonLevel01,

        Text_HP02,
        Text_PokemonName02,
        Text_PokemonLevel02,

        Text_HP03,
        Text_PokemonName03,
        Text_PokemonLevel03,

        Text_HP04,
        Text_PokemonName04,
        Text_PokemonLevel04,

        Text_HP05,
        Text_PokemonName05,
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
        for (int index = 0; index < pokemonDataList.Length; index++)
        {
            if (pokemonDataList.Length <= index)
                break;

            GameObject pokemonInfoObject = GetObject((int)index);
            if (pokemonInfoObject == null)
            {
                Debug.Log("PokemonInfoObject Not Found");
                return;
            }

            Text pokemonNameText = Util.FindChild<Text>(pokemonInfoObject, "Text_Name", true);
            pokemonNameText.text = pokemonDataList[index].Name;

            Text pokemonLevelText = Util.FindChild<Text>(pokemonInfoObject, "Text_Level", true);
            pokemonNameText.text = pokemonDataList[index].Info.Level.ToString();

            Sprite curPokemonSprite = Managers.Resource.Load<Sprite>($"Sprite/pokemon/icons/{pokemonDataList[index].Id}");
            Image pokemonImage = Util.FindChild<Image>(pokemonInfoObject, "Image_Pokemon", true);
            pokemonImage.sprite = curPokemonSprite;

            // hp
            int curHp = Managers.Player.MyPlayer.GetCurPokemonInfo().Hp;
            int maxHp = Managers.Data.PokeonDict[pokemonDataList[index].Id].Info.Hp;

            Text pokemonHpText = Util.FindChild<Text>(pokemonInfoObject, "Text_HP", true);
            pokemonHpText.text = curHp.ToString();
            pokemonHpText.text += "/" + maxHp.ToString();
            
            float hpRatio = (float)curHp / (float)maxHp;
            Slider hpSlider = Util.FindChild<Slider>(pokemonInfoObject, "Slider", true);
            hpSlider.value = hpRatio;
            int hpstate = 2 - (int)(hpRatio / 0.34f);
            UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
            if (battleScene == null)
            {
                Debug.Log("BattleScene Not Found");
                return;
            }

            Image fillObj = Util.FindChild<Image>(pokemonInfoObject, "Fill", true);
            fillObj.sprite = battleScene.HpSprites[hpstate];
        }
    }
}
