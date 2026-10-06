using Google.Protobuf.Protocol;

namespace Server.Game.Room
{
    public partial class GameRoom : JobSerializer
    {
        public void HandleEquipItem(Player player, C_EquipItem equipPacket)
        {
            player?.HandleEquipItem(equipPacket);
        }
    }
}