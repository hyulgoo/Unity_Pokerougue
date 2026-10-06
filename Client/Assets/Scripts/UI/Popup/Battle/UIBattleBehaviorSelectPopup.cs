using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBattleBehaviorSelectPopup : UICommonPopup
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

        InputMode = false;

        BindButton(typeof(Buttons));
        
        GetButton((int)Buttons.Battle).onClick.AddListener(OnClickBattleButton);
        GetButton((int)Buttons.Pokemon).onClick.AddListener(OnClickPokemonButton);
        GetButton((int)Buttons.Ball).onClick.AddListener(OnClickBallButton);
        GetButton((int)Buttons.RunAway).onClick.AddListener(OnClickRunAwayButton);
    }

    void OnClickBattleButton()
    {
        Managers.UI.ClosePopupUI();

        UIBattleSkillSelectPopup skillSelectPopup =  Managers.UI.ShowPopupUI<UIBattleSkillSelectPopup>();
        skillSelectPopup.InitSound = InitSounds.Select;
    }

    void OnClickPokemonButton()
    {
        Managers.UI.ClosePopupUI();

        UIBattleScene battleSceene = Managers.UI.SceneUI.GetComponent<UIBattleScene>();
        if (battleSceene == null)
        {
            Debug.Assert(false, "Fail to Find BattleScene!!");
        }

        UIBattlePokemonChangePopup pokemonChangePopup = Managers.UI.ShowPopupUI<UIBattlePokemonChangePopup>();
        pokemonChangePopup.BattleScene = battleSceene;
        pokemonChangePopup.MustChange = false;
        pokemonChangePopup.InitSound = InitSounds.Popup;
    }

    void OnClickBallButton()
    {
        // 포획 기능(구현계획 없음)
    }

    void OnClickRunAwayButton()
    {
        Managers.UI.ClosePopupUI();

        // 항복 패킷 보내기
        C_Turn turnPacket = new C_Turn();
        turnPacket.TurnInfo = new TurnInfo();
        turnPacket.TurnInfo.Action = ActionType.Runaway;
        turnPacket.PlayerId = Managers.Player.MyPlayer.Id;

        Managers.Network.Send(turnPacket);
    }
}
