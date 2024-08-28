using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Game
{
	public class ObjectManager
	{
		public static ObjectManager Instance { get; } = new ObjectManager();

		object _lock = new object();
		Dictionary<int, Dictionary<int, Player>> _players = new Dictionary<int, Dictionary<int, Player>>();

		int _counter = 0;
		// [UNUSED(1)][TYPE(7)][ID(24)]
		Dictionary<int, Stack<int>> _turn = new Dictionary<int, Stack<int>>();

		public T Add<T>(int roomId) where T : GameObject, new()
		{
			T gameObject = new T();

			lock (_lock)
			{
				Dictionary<int, Player> dict;
				bool find = _players.TryGetValue(roomId, out dict);

				if(!find)
                {
					dict = new Dictionary<int, Player>();
                    _players.Add(roomId, dict);
                }

				gameObject.Id = GenerateId(gameObject.ObjectType);

				if (gameObject.ObjectType == GameObjectType.Player)
				{
					_players[roomId].Add(gameObject.Id, gameObject as Player);
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
					return _players[roomId].Remove(objectId);
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
					if (_players[roomId].TryGetValue(objectId, out player))
						return player;
				}
			}

			return null;
        }

        public int GetTurn(int roomId)
        {
			lock (_lock)
			{
                Stack<int> stack;
				bool find = _turn.TryGetValue(roomId, out stack);
				if(!find)
				{
					_turn.Add(roomId, new Stack<int>());
				}

				if (_turn[roomId].Count == 0)
				{
					Dictionary<int, int> speedlist = new Dictionary<int, int>();
					foreach (var player in _players[roomId])
					{
						int pokemonid = player.Value.PokemonList.Pokemon[0];
						int speed = DataManager.MonsterDict[pokemonid].info.Spe;
						speedlist.Add(speed, player.Key);
					}

					foreach (var key in speedlist.Keys.OrderBy(k => k))
					{
						_turn[roomId].Push(speedlist[key]);
                    }
				}

				return _turn[roomId].Pop();
			}
        }

		public void ClearTurn(int roomId)
		{
            _turn[roomId].Clear();
		}
    }
}
