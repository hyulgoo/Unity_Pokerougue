using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIBattlePokemonChangeButton : UICommonButton, IDeselectHandler
{
    [SerializeField]
    bool isCurrentPokemonSelectButton = false;

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);

        ChangeButtonSprite(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ChangeButtonSprite(false);
    }

    void ChangeButtonSprite(bool isOnSelect)
    {
        Image imageComponent = GetComponent<Image>();
        if (imageComponent == null)
        {
            Debug.Assert(false, "Image Component를 찾을 수 없습니다!");
            return;
        }

        if (isCurrentPokemonSelectButton)
            imageComponent.sprite = Managers.Resource.LoadAll<Sprite>("Sprite/ui/party_slot_main")[isOnSelect ? 1 : 0];
        else
            imageComponent.sprite = Managers.Resource.LoadAll<Sprite>("Sprite/ui/party_slot")[isOnSelect ? 1 : 0];
    }
}
