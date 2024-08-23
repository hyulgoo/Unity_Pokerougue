using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;

class PacketManager
{
	#region Singleton
	static PacketManager _instance = new PacketManager();
	public static PacketManager Instance { get { return _instance; } }
	#endregion

	PacketManager()
	{
		Register();
	}

	Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>> _onRecv = new Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>>();
	Dictionary<ushort, Action<PacketSession, IMessage>> _handler = new Dictionary<ushort, Action<PacketSession, IMessage>>();
		
	public Action<PacketSession, IMessage, ushort> CustomHandler { get; set; }

	public void Register()
	{		
		_onRecv.Add((ushort)MsgId.SEnterGame, MakePacket<S_EnterGame>);
		_handler.Add((ushort)MsgId.SEnterGame, PacketHandler.S_EnterGameHandler);		
		_onRecv.Add((ushort)MsgId.SLeaveGame, MakePacket<S_LeaveGame>);
		_handler.Add((ushort)MsgId.SLeaveGame, PacketHandler.S_LeaveGameHandler);		
		_onRecv.Add((ushort)MsgId.SConnected, MakePacket<S_Connected>);
		_handler.Add((ushort)MsgId.SConnected, PacketHandler.S_ConnectedHandler);		
		_onRecv.Add((ushort)MsgId.SSpawn, MakePacket<S_Spawn>);
		_handler.Add((ushort)MsgId.SSpawn, PacketHandler.S_SpawnHandler);		
		_onRecv.Add((ushort)MsgId.SDespawn, MakePacket<S_Despawn>);
		_handler.Add((ushort)MsgId.SDespawn, PacketHandler.S_DespawnHandler);		
		_onRecv.Add((ushort)MsgId.SLogin, MakePacket<S_Login>);
		_handler.Add((ushort)MsgId.SLogin, PacketHandler.S_LoginHandler);		
		_onRecv.Add((ushort)MsgId.SCreatePlayer, MakePacket<S_CreatePlayer>);
		_handler.Add((ushort)MsgId.SCreatePlayer, PacketHandler.S_CreatePlayerHandler);		
		_onRecv.Add((ushort)MsgId.SItemList, MakePacket<S_ItemList>);
		_handler.Add((ushort)MsgId.SItemList, PacketHandler.S_ItemListHandler);		
		_onRecv.Add((ushort)MsgId.SAddItem, MakePacket<S_AddItem>);
		_handler.Add((ushort)MsgId.SAddItem, PacketHandler.S_AddItemHandler);		
		_onRecv.Add((ushort)MsgId.SEquipItem, MakePacket<S_EquipItem>);
		_handler.Add((ushort)MsgId.SEquipItem, PacketHandler.S_EquipItemHandler);		
		_onRecv.Add((ushort)MsgId.SChangeStat, MakePacket<S_ChangeStat>);
		_handler.Add((ushort)MsgId.SChangeStat, PacketHandler.S_ChangeStatHandler);		
		_onRecv.Add((ushort)MsgId.SPing, MakePacket<S_Ping>);
		_handler.Add((ushort)MsgId.SPing, PacketHandler.S_PingHandler);		
		_onRecv.Add((ushort)MsgId.SRequestSendOk, MakePacket<S_RequestSendOk>);
		_handler.Add((ushort)MsgId.SRequestSendOk, PacketHandler.S_RequestSendOkHandler);		
		_onRecv.Add((ushort)MsgId.SRequestDuel, MakePacket<S_RequestDuel>);
		_handler.Add((ushort)MsgId.SRequestDuel, PacketHandler.S_RequestDuelHandler);		
		_onRecv.Add((ushort)MsgId.SRespondDuel, MakePacket<S_RespondDuel>);
		_handler.Add((ushort)MsgId.SRespondDuel, PacketHandler.S_RespondDuelHandler);		
		_onRecv.Add((ushort)MsgId.SStartBattle, MakePacket<S_StartBattle>);
		_handler.Add((ushort)MsgId.SStartBattle, PacketHandler.S_StartBattleHandler);
	}

	public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer)
	{
		ushort count = 0;

		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		Action<PacketSession, ArraySegment<byte>, ushort> action = null;
		if (_onRecv.TryGetValue(id, out action))
			action.Invoke(session, buffer, id);
	}

	void MakePacket<T>(PacketSession session, ArraySegment<byte> buffer, ushort id) where T : IMessage, new()
	{
		T pkt = new T();
		pkt.MergeFrom(buffer.Array, buffer.Offset + 4, buffer.Count - 4);

		if (CustomHandler != null)
		{
			CustomHandler.Invoke(session, pkt, id);
		}
		else
		{
			Action<PacketSession, IMessage> action = null;
			if (_handler.TryGetValue(id, out action))
				action.Invoke(session, pkt);
		}
	}

	public Action<PacketSession, IMessage> GetPacketHandler(ushort id)
	{
		Action<PacketSession, IMessage> action = null;
		if (_handler.TryGetValue(id, out action))
			return action;
		return null;
	}
}