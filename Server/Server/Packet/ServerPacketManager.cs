using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;

namespace Server.Packet
{
    internal class PacketManager
    {
        private readonly Dictionary<ushort, Action<PacketSession, IMessage>> _handler =
            new Dictionary<ushort, Action<PacketSession, IMessage>>();

        private readonly Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>> _onRecv =
            new Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>>();

        private PacketManager(Action<PacketSession, IMessage, ushort> customHandler)
        {
            CustomHandler = customHandler;
            Register();
        }

        private PacketManager()
        {
            Register();
        }

        private Action<PacketSession, IMessage, ushort> CustomHandler { get; }

        private void Register()
        {
            _onRecv.Add((ushort)MsgId.CLogin, MakePacket<C_Login>);
            _handler.Add((ushort)MsgId.CLogin, PacketHandler.C_LoginHandler);
            _onRecv.Add((ushort)MsgId.CEnterGame, MakePacket<C_EnterGame>);
            _handler.Add((ushort)MsgId.CEnterGame, PacketHandler.C_EnterGameHandler);
            _onRecv.Add((ushort)MsgId.CCreatePlayer, MakePacket<C_CreatePlayer>);
            _handler.Add((ushort)MsgId.CCreatePlayer, PacketHandler.C_CreatePlayerHandler);
            _onRecv.Add((ushort)MsgId.CEquipItem, MakePacket<C_EquipItem>);
            _handler.Add((ushort)MsgId.CEquipItem, PacketHandler.C_EquipItemHandler);
            _onRecv.Add((ushort)MsgId.CPong, MakePacket<C_Pong>);
            _handler.Add((ushort)MsgId.CPong, PacketHandler.C_PongHandler);
            _onRecv.Add((ushort)MsgId.CRequestDuel, MakePacket<C_RequestDuel>);
            _handler.Add((ushort)MsgId.CRequestDuel, PacketHandler.C_RequestDuelHandler);
            _onRecv.Add((ushort)MsgId.CRespondDuel, MakePacket<C_RespondDuel>);
            _handler.Add((ushort)MsgId.CRespondDuel, PacketHandler.C_RespondDuelHandler);
            _onRecv.Add((ushort)MsgId.CSelectPokemon, MakePacket<C_SelectPokemon>);
            _handler.Add((ushort)MsgId.CSelectPokemon, PacketHandler.C_SelectPokemonHandler);
            _onRecv.Add((ushort)MsgId.CTurn, MakePacket<C_Turn>);
            _handler.Add((ushort)MsgId.CTurn, PacketHandler.C_TurnHandler);
            _onRecv.Add((ushort)MsgId.CTurnEnd, MakePacket<C_TurnEnd>);
            _handler.Add((ushort)MsgId.CTurnEnd, PacketHandler.C_TurnEndHandler);
            _onRecv.Add((ushort)MsgId.CChangePokemon, MakePacket<C_ChangePokemon>);
            _handler.Add((ushort)MsgId.CChangePokemon, PacketHandler.C_ChangePokemonHandler);
        }

        public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer)
        {
            ushort count = 0;

            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            count += 2;
            ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;

            if (_onRecv.TryGetValue(id, out Action<PacketSession, ArraySegment<byte>, ushort> action))
                action.Invoke(session, buffer, id);
        }

        private void MakePacket<T>(PacketSession session, ArraySegment<byte> buffer, ushort id) where T : IMessage, new()
        {
            T pkt = new T();
            pkt.MergeFrom(buffer.Array, buffer.Offset + 4, buffer.Count - 4);

            if (CustomHandler != null)
            {
                CustomHandler.Invoke(session, pkt, id);
            }
            else
            {
                if (_handler.TryGetValue(id, out Action<PacketSession, IMessage> action))
                    action.Invoke(session, pkt);
            }
        }

        public Action<PacketSession, IMessage> GetPacketHandler(ushort id)
        {
            return _handler.GetValueOrDefault(id);
        }

        #region Singleton

        public static PacketManager Instance { get; } = new PacketManager();

        #endregion
    }
}