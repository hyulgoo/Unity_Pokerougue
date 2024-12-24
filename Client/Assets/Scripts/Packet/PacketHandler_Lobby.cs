using Data;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Drawing;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

partial class PacketHandler
{
	public static void S_CreatePlayerHandler(PacketSession session, IMessage packet)
    {
        S_CreatePlayer createOkPacket = (S_CreatePlayer)packet;

		if (createOkPacket.Player == null)
		{
			C_CreatePlayer createPacket = new C_CreatePlayer();
			createPacket.Name = Managers.Network.Name;
			Managers.Network.Send(createPacket);
		}
		else
		{
			C_EnterGame enterGamePacket = new C_EnterGame();
			enterGamePacket.Name = createOkPacket.Player.Name;
			Managers.Network.Send(enterGamePacket);
		}
	}

	public static void S_RequestSendOkHandler(PacketSession session, IMessage packet)
    {
        S_RequestSendOk requestSendOK = (S_RequestSendOk)packet;

		if(requestSendOK.SendOK == 1)
		{
			// 패킷 전송 성공
			Managers.UI.ClosePopupUI();
            UICommonWaitPopup popup = Managers.UI.ShowPopupUI<UICommonWaitPopup>();
			popup.Text = "상대 응답 대기 중";
		}
		else
		{
			// 대결 신청 패킷 보내기 실패
			Managers.UI.ClosePopupUI();
			UICommonAnnouncePopup popup = Managers.UI.ShowPopupUI<UICommonAnnouncePopup>();
			popup.SetAnnounceText("상대가 로비에 존재하지 않습니다");
		}
	}

    public static void S_RequestDuelHandler(PacketSession session, IMessage packet)
    {
        S_RequestDuel requestDuel = (S_RequestDuel)packet;
        UILobbyDualRespondPopup popup = Managers.UI.ShowPopupUI<UILobbyDualRespondPopup>("UICommonRespondPopup");
		if (popup == null)
		{
			Console.WriteLine("( UICommonRespondPopup )을 찾을 수 없습니다.");
		}

		popup.SetDuelRequestAnnounce(requestDuel.FromId);
    }

    public static void S_RespondDuelHandler(PacketSession session, IMessage packet)
    {
        S_RespondDuel respenDuel = (S_RespondDuel)packet;

		bool battleStart = respenDuel.DuelOK == 1 ? true : false;
		if(battleStart)
        {
            Managers.UI.ClosePopupUI();
            UICommonAnnouncePopup popup = Managers.UI.ShowPopupUI<UICommonAnnouncePopup>();
            popup.SetAnnounceText("게임이 곧 시작됩니다");

			// 인게임으로 전환
			Managers.Scene.LoadScene(Define.Scene.Select);
        }
        else
		{
			Managers.UI.ClosePopupUI();
            UICommonAnnouncePopup popup = Managers.UI.ShowPopupUI<UICommonAnnouncePopup>();
			popup.SetAnnounceText("상대가 거절하였습니다");
		}
    }
}