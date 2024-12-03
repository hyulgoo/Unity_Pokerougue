using Google.Protobuf.Protocol;
using TMPro;
using UnityEngine;

public class UILobbyDualRespondPopup : UICommonReplyPopup
{
    int _enemyId;

    public override void Init()
    {
        base.Init();

        GetButton((int)Buttons.Btn_Apply).gameObject.GetComponentInChildren<TMP_Text>().text = "수락";
        GetButton((int)Buttons.Btn_Deny).gameObject.GetComponentInChildren<TMP_Text>().text = "거절";
    }

    public void SetDuelRequestAnnounce(int enemyId)
    {
        // 상대 Id 지정
        _enemyId = enemyId;

        // 상대방의 name을 알아내고 ~가 대결을 신청했다는 창을 띄움
        GameObject enemyPlayer = Managers.Player.FindById(enemyId);
        string announce = enemyPlayer.GetComponent<CommonPlayerController>().name;
        announce += "님이  \n 대결을 신청하였습니다";
        GetText((int)Texts.Announce).text = announce;
    }

    protected override void OnClickApplyButton()
    {
        // 대결 신청에 응답 패킷 전송
        C_RespondDuel respondDuelPacket = new C_RespondDuel();
        respondDuelPacket.DuelOK = 1;
        respondDuelPacket.FromId = Managers.Player.MyPlayer.Id;
        respondDuelPacket.ToId = _enemyId;
        Managers.Network.Send(respondDuelPacket);

        //대결 신청 창을 닫음
        ClosePopupUI();

        // 서버 응답 대기창 생성
        UICommonWaitPopup popup = Managers.UI.ShowPopupUI<UICommonWaitPopup>();
        popup.Text = "서버 응답 대기 중";
    }

    protected override void OnClickDenyButton()
    {
        // 대결 신청에 응답 패킷 전송
        C_RespondDuel respondDuelPacket = new C_RespondDuel();
        respondDuelPacket.DuelOK = 0;
        respondDuelPacket.FromId = Managers.Player.MyPlayer.Id;
        respondDuelPacket.ToId = _enemyId;
        Managers.Network.Send(respondDuelPacket);

        //대결 신청 창을 닫음
        ClosePopupUI();
    }
}
