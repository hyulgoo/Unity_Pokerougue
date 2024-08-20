using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_SelectPokemonButton : UI_Base, ISelectHandler
{
    int _id;
    public int Id
    {
        get { return _id; }
        set
        {
            _id = value;
            SetIconImage();
        }
    }

    public UI_SelectMstScene SelectMstScene { get; set; }

    enum GameObjects
    {
        Icon
    }

    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClickButton);
    }

    void OnClickButton()
    {
        UI_SelectfromSelectionPopup popup = Managers.UI.ShowPopupUI<UI_SelectfromSelectionPopup>();
        popup.MstScene = SelectMstScene;
        popup.Id = Id;
    }

    public void SetIconImage()
    {
        Image iconImg = GetObject((int)GameObjects.Icon).GetComponent<Image>();
        Sprite curMstSprite = Managers.Resource.Load<Sprite>($"Sprite/pokemon/icons/{Id}");
        iconImg.sprite = curMstSprite;
    }

    public void OnSelect(BaseEventData eventData)
    {
        GameObject go = Managers.Select.CurPanel;

        if (go == null) return;
        UI_Base uibase = go.GetComponent<UI_Base>();

        if (uibase == null) return;
        uibase._lastSelected = eventData.selectedObject.GetComponent<Button>();
        uibase.SetPointerPos(eventData.selectedObject.transform);

        // ¹öÆ° ¼±ÅÃ ½Ã CurPokemon Info¸¦ ¶ç¿öÁÜ.
        UI_SelectMstScene parentScene = GetComponentInParent<UI_SelectMstScene>();
        parentScene.SetCurMstInfo(Id);
    }
}
