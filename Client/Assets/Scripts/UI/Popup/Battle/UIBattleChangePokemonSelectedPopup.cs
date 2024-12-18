using Google.Protobuf.Protocol;
using UnityEngine;

public class UIBattleChangePokemonSelectedPopup : UICommonPopup
{
    enum Buttons
    {
        Btn_ChangePokemon,
        Btn_CheckStat,
        Btn_ChangeNickName,
        Btn_ReleasePokemon,
        Btn_Cancle,
    }

    UIBattlePokemonChangePopup _changePokemonSelectedPopup = null;
    public UIBattlePokemonChangePopup ChangePokemonSelectedPopup { set { _changePokemonSelectedPopup = value; } }

    public override void Init()
    {
        base.Init();

        BindButton(typeof(Buttons));

        SetButtonEvent();
    }

    void SetButtonEvent()
    {
        GetButton((int)Buttons.Btn_ChangePokemon).onClick.AddListener(OnClickChangePokemonButton);
        GetButton((int)Buttons.Btn_CheckStat).onClick.AddListener(OnClickCheckStateButton);
        GetButton((int)Buttons.Btn_ChangeNickName).onClick.AddListener(OnClickChangeNickNameButton);
        GetButton((int)Buttons.Btn_ReleasePokemon).onClick.AddListener(OnClickReleasePokemonButton);
        GetButton((int)Buttons.Btn_Cancle).onClick.AddListener(OnClickCancleButton);
    }

    void OnClickChangePokemonButton()
    {
        ClosePopupUI();
        _changePokemonSelectedPopup.ChangePokemon();
    }

    void OnClickCheckStateButton()
    {
        // TODO : 선택 포켓몬 스탯창 띄우기
    }

    void OnClickChangeNickNameButton()
    {
        // TODO : 선택 포켓몬 닉네임 바꾸기(아마 안할듯)
    }

    void OnClickReleasePokemonButton()
    {
        // TODO : 선택 포켓몬 놓아주기(아마 안할듯)
    }

    void OnClickCancleButton()
    {
        ClosePopupUI();
    }
}
