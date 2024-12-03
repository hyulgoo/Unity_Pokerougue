using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISelectSelectionPopup : UICommonPopup
{
    public UISelectPokemonScene MstScene { get; set; }

    public int Id { get; set; }

    enum Buttons
    {
        AddParty,
        TechManage,
        AddFavorite,
        ChangeNickname,
        Cancle            
    }

    public override void Init()
    {
        base.Init();
        BindButton(typeof(Buttons));

        GetButton((int)Buttons.AddParty).onClick.AddListener(OnClickAddPartyButton);
        GetButton((int)Buttons.TechManage).onClick.AddListener(OnClickTechManageButton);
        GetButton((int)Buttons.AddFavorite).onClick.AddListener(OnClickAddFavoriteButton);
        GetButton((int)Buttons.ChangeNickname).onClick.AddListener(OnClickChangeNicknameButton);
        GetButton((int)Buttons.Cancle).onClick.AddListener(OnClickCancleButton);
    }

    void OnClickAddPartyButton()
    {
        if(!MstScene.PickPokemon(Id))
        {
            UICommonAnnouncePopup popup = Managers.UI.ShowPopupUI<UICommonAnnouncePopup>();
            popup.SetAnnounceText("더이상 고를 수 없습니다");
        }
        ClosePopupUI();
    }

    void OnClickTechManageButton()
    {
        ClosePopupUI();
    }

    void OnClickAddFavoriteButton()
    {
        ClosePopupUI();
    }

    void OnClickChangeNicknameButton()
    {
        ClosePopupUI();
    }

    void OnClickCancleButton()
    {
        ClosePopupUI();
    }
}
