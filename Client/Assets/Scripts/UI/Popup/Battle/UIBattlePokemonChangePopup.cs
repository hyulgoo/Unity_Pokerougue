using Google.Protobuf.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIBattlePokemonChangePopup : UICommonPopup
{
    enum Buttons
    {
        Btn_PokemonList00,
        Btn_PokemonList01,
        Btn_PokemonList02,
        Btn_PokemonList03,
        Btn_PokemonList04,
        Btn_PokemonList05,

        Btn_Cancle,
    }

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

        Text_Announce,
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

    public int SelectedChangePokemonId { get; set; }
    public UIBattleScene BattleScene { get; set; }
    public bool MustChange { get; set; }

    public override void Init()
    {
        base.Init();

        BindButton(typeof(Buttons));
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        Bind<Slider>(typeof(Sliders));
        SetPokemonButtonList();

        GetButton((int)Buttons.Btn_Cancle).onClick.AddListener(OnClickCancleButton);
    }

    private void SetPokemonButtonList()
    {
        PokemonData[] pokemonDataList = Managers.Player.MyPlayer.GetAllPokemonData();
        UIBattleScene battleScene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        if (battleScene == null)
            Debug.Assert(false, "BattleScene Not Found");

        for (int pokemonIndex = 0; pokemonIndex < Define.PokemonMaxCount; pokemonIndex++)
        {
            Image pokemonImage = GetImage(pokemonIndex);
            TMP_Text pokemonHpText = GetText(pokemonIndex);
            TMP_Text pokemonNameText = GetText(pokemonIndex + Define.PokemonMaxCount);
            TMP_Text pokemonLevelText = GetText(pokemonIndex + Define.PokemonMaxCount * 2);
            Slider hpSlider = Get<Slider>(pokemonIndex);

            if (pokemonIndex < pokemonDataList.Length)
            {
                Button pokemonButton = GetButton(pokemonIndex);
                GameObject hpSliderObject = hpSlider.gameObject;

                // Button
                pokemonButton.onClick.AddListener(OnClickChangePokemonButton);

                UIBattlePokemonChangeSelectButton pokemonChangeSelectButton = pokemonButton.gameObject.GetOrAddComponent<UIBattlePokemonChangeSelectButton>();
                if (pokemonChangeSelectButton == null)
                    Debug.Assert(false, $"Connot Found UIBattlePokemonChangeSelectButton");

                pokemonChangeSelectButton.PokemonChangePopup = this;
                pokemonChangeSelectButton.SelectedChangePokemonId = pokemonDataList[pokemonIndex].Id;

                // Image
                string curPokemonSpritePath = $"Sprite/pokemon/icons/{pokemonDataList[pokemonIndex].Id}";
                Sprite curPokemonSprite = Managers.Resource.Load<Sprite>(curPokemonSpritePath);
                if (curPokemonSprite == null)
                    Debug.Assert(false, $"Connot Found {curPokemonSpritePath}");

                pokemonImage.sprite = curPokemonSprite;
                pokemonImage.color = Color.white;

                // Texts
                int curHp = pokemonDataList[pokemonIndex].Info.Hp;
                int maxHp = Managers.Data.PokeonDict[pokemonDataList[pokemonIndex].Id].Info.Hp;

                pokemonHpText.text = curHp.ToString();
                pokemonHpText.text += "/" + maxHp.ToString();
                pokemonNameText.text = pokemonDataList[pokemonIndex].Name;
                pokemonLevelText.text = pokemonDataList[pokemonIndex].Info.Level.ToString();

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
                pokemonHpText.text = string.Empty;
                pokemonNameText.text = string.Empty;
                pokemonLevelText.text = string.Empty;
                hpSlider.value = 0f;
            }
        }
    }

    public void SetAnnounceText(string text)
    {
        if (string.IsNullOrEmpty(text))
            Debug.Assert(false, "BattlePokemonChangePopup AnnounceText is Empty!!");

        GetText((int)Texts.Text_Announce).text = text;
    }

    public void ChangePokemon()
    {
        if (SelectedChangePokemonId == Define.InValidNumber)
            Debug.Assert(false, "SelectedChangePokemonId is InValid!!");

        {
            C_ChangePokemon packet = new C_ChangePokemon();
            packet.PlayerId = Managers.Player.MyPlayer.Id;
            packet.ChangeByFallDown = MustChange;
            packet.ChangePokemonId = SelectedChangePokemonId;

            Managers.Network.Send(packet);
        }

        string selectedChangePokemonName = Managers.Data.PokeonDict[SelectedChangePokemonId].Name;
        Managers.Job.Push(() => { BattleScene.SetAnnounce($"가라! {selectedChangePokemonName}!", true); });

        ClosePopupUI();
    }

    public void OnClickChangePokemonButton()
    {
        PokemonInfo selectPokemonInfo = Managers.Player.MyPlayer.GetPokemonInfoById(SelectedChangePokemonId);
        if (selectPokemonInfo == null)
            Debug.Assert(false, $"Fail to Find SelectedPokemon By Id{SelectedChangePokemonId}");

        if (SelectedChangePokemonId == Managers.Player.MyPlayer.GetCurPokemonData().Id)
        {
            UICommonAnnouncePopup announcePopup = Managers.UI.ShowPopupUI<UICommonAnnouncePopup>();
            announcePopup.InitSound = InitSounds.Error;
            announcePopup.SetAnnounceText("현재 출전 중인 포켓몬으로는 교체할 수 없습니다!");
            return;
        }

        if (selectPokemonInfo.Hp <= 0)
        {
            UICommonAnnouncePopup announcePopup = Managers.UI.ShowPopupUI<UICommonAnnouncePopup>();
            announcePopup.InitSound = InitSounds.Error;
            announcePopup.SetAnnounceText("기절 상태의 포켓몬으로는 교체할 수 없습니다.");
            return;
        }

        UIBattleChangePokemonSelectedPopup changePokemonSelectedPopup = Managers.UI.ShowPopupUI<UIBattleChangePokemonSelectedPopup>();
        changePokemonSelectedPopup.ChangePokemonSelectedPopup = this;
        changePokemonSelectedPopup.InitSound = InitSounds.Select;
    }

    void OnClickCancleButton()
    {
        if (Managers.Player.MyPlayer.GetCurPokemonInfo().Hp <= 0)
        {
            UICommonAnnouncePopup announcePopup = Managers.UI.ShowPopupUI<UICommonAnnouncePopup>();
            announcePopup.InitSound = InitSounds.Error;
            announcePopup.SetAnnounceText("포켓몬을 반드시 교체해야 합니다!");
            return;
        }

        ClosePopupUI();

        Managers.UI.ShowPopupUI<UIBattleBehaviorSelectPopup>();
    }
}
