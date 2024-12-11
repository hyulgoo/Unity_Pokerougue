using UnityEngine;
using UnityEngine.EventSystems;

public class UIBattlePokemonChangeSelectButton : UICommonButton
{
    private int _selectedChangePokemonId = Define.InValidNumber;
    public int SelectedChangePokemonId { set { _selectedChangePokemonId = value; } }

    private UIBattlePokemonChangePopup _pokemonChangePopup = null;
    public UIBattlePokemonChangePopup PokemonChangePopup { set { _pokemonChangePopup = value; } }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);

        _pokemonChangePopup.SelectedChangePokemonId = _selectedChangePokemonId;
    }
}
