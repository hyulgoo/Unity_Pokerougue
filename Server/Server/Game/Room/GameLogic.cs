using System.Collections.Generic;

namespace Server.Game.Room
{
    public class GameLogic : JobSerializer
    {
        private readonly Dictionary<int, Room.GameRoom> _rooms = new Dictionary<int, Room.GameRoom>();
        private int _roomId = 1;
        public static GameLogic Instance { get; } = new GameLogic();

        public void Update()
        {
            Flush();

            foreach (Room.GameRoom room in _rooms.Values) room.Update();
        }

        public Room.GameRoom Add()
        {
            Room.GameRoom gameRoom = new Room.GameRoom();
            gameRoom.Push(gameRoom.Init);

            gameRoom.RoomId = _roomId;
            _rooms.Add(_roomId, gameRoom);
            _roomId++;

            return gameRoom;
        }

        public bool Remove(int roomId)
        {
            return _rooms.Remove(roomId);
        }

        public Room.GameRoom Find(int roomId)
        {
            return _rooms.GetValueOrDefault(roomId);
        }
    }
}