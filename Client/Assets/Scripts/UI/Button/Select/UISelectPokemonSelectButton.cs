using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISelectPokemonSelectButton : UICommonBase, ISelectHandler
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

    public UISelectPokemonScene SelectScene { get; set; }

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
        UISelectSelectionPopup popup = Managers.UI.ShowPopupUI<UISelectSelectionPopup>();
        popup.SelectScene = SelectScene;
        popup.Id = Id;
    }

    public void SetIconImage()
    {
        Image iconImg = GetObject((int)GameObjects.Icon).GetComponent<Image>();
        Sprite curPokemonSprite = Managers.Resource.Load<Sprite>($"Sprite/pokemon/icons/{Id}");
        iconImg.sprite = curPokemonSprite;
    }

    public void OnSelect(BaseEventData eventData)
    {
        GameObject go = Managers.Select.CurPanel;
        if (go == null) 
            return;

        UICommonBase uibase = go.GetComponent<UICommonBase>();
        if (uibase == null) 
            return;

        uibase._lastSelected = eventData.selectedObject.GetComponent<Button>();
        uibase.SetPointerPos(eventData.selectedObject.transform);

        SetSceneCurrentSelectPokemonInfo();
    }

    public void SetSceneCurrentSelectPokemonInfo()
    {
        SelectScene.SetCurrentSelectPokemonInfo(Id);
    }
}
