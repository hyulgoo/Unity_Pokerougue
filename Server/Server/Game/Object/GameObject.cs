using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Server.Game
{
	public class GameObject
	{
		public GameObjectType ObjectType { get; protected set; } = GameObjectType.Nonetype;
		public int Id
		{
			get { return Info.ObjectId; }
			set { Info.ObjectId = value; }
		}

		public GameRoom Room { get; set; }

		public ObjectInfo Info { get; set; } = new ObjectInfo();

		public GameObject()
		{
			//Info.StatInfo = Stat;
		}

		public virtual void Update()
		{

		}

		public virtual GameObject GetOwner()
		{
			return this;
		}
	}
}
