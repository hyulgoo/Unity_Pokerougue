using Google.Protobuf.Protocol;

namespace Server.Game
{
    public class GameObject
    {
        public GameObjectType ObjectType { get; protected set; } = GameObjectType.Nonetype;

        public int Id
        {
            get => Info.ObjectId;
            set => Info.ObjectId = value;
        }

        public Room.GameRoom Room { get; set; }

        public ObjectInfo Info { get; set; } = new ObjectInfo();

        public virtual void Update()
        {
        }

        public virtual GameObject GetOwner()
        {
            return this;
        }
    }
}