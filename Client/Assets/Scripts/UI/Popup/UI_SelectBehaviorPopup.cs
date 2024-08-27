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

        BindButton(typeof(Buttons));

        GetButton((int)Buttons.Battle).onClick.AddListener(OnClickBattleButton);
        GetButton((int)Buttons.Pokemon).onClick.AddListener(OnClickPokemonButton);
        GetButton((int)Buttons.Ball).onClick.AddListener(OnClickBallButton);
        GetButton((int)Buttons.RunAway).onClick.AddListener(OnClickRunAwayButton);
    }

    void OnClickBattleButton()
    {
        // SkillSelect popup을 띄움
        Managers.UI.ShowPopupUI<UI_SkillSelectPopup>();
        Managers.UI.ClosePopupUI();
    }

    void OnClickPokemonButton()
    {
        // MonsterChangeScene을 띄움
        // Managers.UI.ShowSceneUI<UI_MonsterChangeScene>();
    }

    void OnClickBallButton()
    {
    }

    void OnClickRunAwayButton()
    {
        // 항복 패킷 보내기
    }
}
