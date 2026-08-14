using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game.Object.Lecture
{
    public class Projectile : GameObject
    {
        public Projectile()
        {
            ObjectType = GameObjectType.Projectile;
        }

        public SkillData Data { get; set; }

        public override void Update()
        {
        }
    }
}