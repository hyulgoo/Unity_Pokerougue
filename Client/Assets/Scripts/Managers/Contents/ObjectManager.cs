using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectManager
{
	public MyPlayerController MyPlayer { get; set; }
	public PlayerController Opponent { get; set; }

	public int ArenaType { get; set; }

	public bool MyTurn { get; set; }

	Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();
	
	public static GameObjectType GetObjectTypeById(int id)
	{
		int type = (id >> 24) & 0x7F;
		return (GameObjectType)type;
	}

	public void Add(ObjectInfo info, bool myPlayer = false)
	{
		if (MyPlayer != null && MyPlayer.Id == info.ObjectId)
			return;
		if (_objects.ContainsKey(info.ObjectId))
			return;

		GameObjectType objectType = GetObjectTypeById(info.ObjectId);
		if (objectType == GameObjectType.Player)
		{
			if (myPlayer)
			{
				GameObject go = Managers.Resource.Instantiate("Creature/MyPlayer");
				go.name = info.Name;
				_objects.Add(info.ObjectId, go);

				MyPlayer = go.GetComponent<MyPlayerController>();
				MyPlayer.Id = info.ObjectId;
			}
			else
			{
				GameObject go = Managers.Resource.Instantiate("Creature/Player");
				go.name = info.Name;
				_objects.Add(info.ObjectId, go);

				PlayerController pc = go.GetComponent<PlayerController>();
				pc.Id = info.ObjectId;

				if(Managers.Scene.CurrentScene.SceneType == Define.Scene.Select)
				{
					Opponent = pc;
				}
			}
		}
	}

	public void Remove(int id)
	{
		if (MyPlayer != null && MyPlayer.Id == id)
			return;
		if (_objects.ContainsKey(id) == false)
			return;

		GameObject go = FindById(id);
		if (go == null)
			return;

		_objects.Remove(id);
		Managers.Resource.Destroy(go);
	}

	public GameObject FindById(int id)
	{
		GameObject go = null;
		_objects.TryGetValue(id, out go);
		return go;
	}

	public GameObject Find(Func<GameObject, bool> condition)
	{
		foreach (GameObject obj in _objects.Values)
		{
			if (condition.Invoke(obj))
				return obj;
		}

		return null;
	}

	public void Clear()
	{
		foreach (GameObject obj in _objects.Values)
			Managers.Resource.Destroy(obj);
		_objects.Clear();
		MyPlayer = null;
		Opponent = null;
	}

	public KeyValuePair<int, GameObject>[] GetObjects()
	{
		KeyValuePair<int, GameObject>[] list = _objects.ToArray();
		return list;
	}

	public string GetPlayerName(int objectid)
	{
		return _objects[objectid].name;
	}
}
