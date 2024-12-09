using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Game
{
	public class PlayerManager
	{
		public static PlayerManager Instance { get; } = new PlayerManager();

		object _lock = new object();
		Dictionary<int, Dictionary<int, Player>> _roomAndPlayers = new Dictionary<int, Dictionary<int, Player>>();

		int _counter = 0;
		// [UNUSED(1)][TYPE(7)][ID(24)]

		public T Add<T>(int roomId) where T : GameObject, new()
		{
			T gameObject = new T();

			lock (_lock)
			{
				Dictionary<int, Player> dict;
				bool find = _roomAndPlayers.TryGetValue(roomId, out dict);

				if(!find)
                {
					dict = new Dictionary<int, Player>();
                    _roomAndPlayers.Add(roomId, dict);
                }

				gameObject.Id = GenerateId(gameObject.ObjectType);

				if (gameObject.ObjectType == GameObjectType.Player)
				{
					_roomAndPlayers[roomId].Add(gameObject.Id, gameObject as Player);
				}
			}

			return gameObject;
		}

		int GenerateId(GameObjectType type)
		{
			lock (_lock)
			{
				return ((int)type << 24) | (_counter++);
			}
		}

		public static GameObjectType GetObjectTypeById(int id)
		{
			int type = (id >> 24) & 0x7F;
			return (GameObjectType)type;
		}

		public bool Remove(int roomId, int objectId)
		{
			GameObjectType objectType = GetObjectTypeById(objectId);

			lock (_lock)
			{
				if (objectType == GameObjectType.Player)
					return _roomAndPlayers[roomId].Remove(objectId);
			}

			return false;
		}

		public Player Find(int roomId, int objectId)
		{
			GameObjectType objectType = GetObjectTypeById(objectId);

			lock (_lock)
			{
				if (objectType == GameObjectType.Player)
				{
					Player player = null;
					if (_roomAndPlayers[roomId].TryGetValue(objectId, out player))
						return player;
				}
			}

			return null;
        }

        public int GetCurrentTurnPlayerId(int roomId, bool includeCurrentTurnPlayer = false)
        {
			lock (_lock)
			{
				int nextTurnPlayerId = 0;
				int nextTurnPlayerPokemonSpeed = 0;
                foreach (var player in _roomAndPlayers[roomId])
                {
                    int playerId = player.Key;
                    int playerCurrentPokemonSpeed = player.Value.Pokemon[0].Info.Spe;

					if (includeCurrentTurnPlayer == false && (player.Value.Room.CurrentTurnPlayerId == playerId))
						continue;

                    if (nextTurnPlayerPokemonSpeed < playerCurrentPokemonSpeed)
                    {
                        nextTurnPlayerId = playerId;
						nextTurnPlayerPokemonSpeed = playerCurrentPokemonSpeed;
                    }
                    else if (nextTurnPlayerPokemonSpeed == playerCurrentPokemonSpeed && nextTurnPlayerId == player.Value.Room.CurrentTurnPlayerId)
                    {
                        nextTurnPlayerId = playerId;
                    }
                }

				return nextTurnPlayerId;
			}
        }
    }
}
