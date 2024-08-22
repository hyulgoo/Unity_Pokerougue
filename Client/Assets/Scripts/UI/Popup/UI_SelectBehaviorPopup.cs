using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_SelectBehaviorPopup : UI_Popup
{
    enum Buttons
    {
        Battle,
        Pokemon,
        Ball,
        RunAway,
    }

    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.Battle).onClick.AddListener(OnClickBattleButton);
        GetButton((int)Buttons.Pokemon).onClick.AddListener(OnClickPokemonButton);
        GetButton((int)Buttons.Ball).onClick.AddListener(OnClickBallButton);
        GetButton((int)Buttons.RunAway).onClick.AddListener(OnClickRunAwayButton);
    }

    void OnClickBattleButton()
    {
        // SkillSelect popupÀ» ¶ç¿ò
        // Managers.UI.ShowPopupUI<UI_SkillSelectPopup>();
    }

    void OnClickPokemonButton()
    {
        // MonsterChangeSceneÀ» ¶ç¿ò
        // Managers.UI.ShowSceneUI<UI_MonsterChangeScene>();
    }

    void OnClickBallButton()
    {
    }

    void OnClickRunAwayButton()
    {
    }
}
