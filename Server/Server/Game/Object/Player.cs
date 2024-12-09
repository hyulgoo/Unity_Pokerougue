using Google.Protobuf.Collections;
using Google.Protobuf.Protocol;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DB;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public class Player : GameObject
	{
		public int PlayerDbId { get; set; }
		public ClientSession Session { get; set; }
		public Inventory Inven { get; private set; } = new Inventory();
		public List<PokemonData> Pokemon { get; set; } = new List<PokemonData>();
        public Player() { ObjectType = GameObjectType.Player; }

		public int[] GetPokemonList()
		{
			int[] list = new int[Pokemon.Count];
			for (int i = 0; i < Pokemon.Count; i++)
				list[i] = Pokemon[i].Id;

			return list;
		}

		public void OnLeaveGame()
		{
			// TODO
			// DB 연동?
			// -- 피가 깎일 때마다 DB 접근할 필요가 있을까?
			// 1) 서버 다운되면 아직 저장되지 않은 정보 날아감
			// 2) 코드 흐름을 다 막아버린다 !!!!
			// - 비동기(Async) 방법 사용?
			// - 다른 쓰레드로 DB 일감을 던져버리면 되지 않을까?
			// -- 결과를 받아서 이어서 처리를 해야 하는 경우가 많음.
			// -- 아이템 생성

			DbTransaction.SavePlayerStatus_Step1(this, Room);
		}

		public void HandleEquipItem(C_EquipItem equipPacket)
		{
			Item item = Inven.Get(equipPacket.ItemDbId);
			if (item == null)
				return;

			if (item.ItemType == ItemType.Consumable)
				return;

			// 착용 요청이라면, 겹치는 부위 해제
			if (equipPacket.Equipped)
			{
				Item unequipItem = null;

				if (unequipItem != null)
				{
					// 메모리 선적용
					unequipItem.Equipped = false;

					// DB에 Noti
					DbTransaction.EquipItemNoti(this, unequipItem);

					// 클라에 통보
					S_EquipItem equipOkItem = new S_EquipItem();
					equipOkItem.ItemDbId = unequipItem.ItemDbId;
					equipOkItem.Equipped = unequipItem.Equipped;
					Session.Send(equipOkItem);
				}
			}

			{
				// 메모리 선적용
				item.Equipped = equipPacket.Equipped;

				// DB에 Noti
				DbTransaction.EquipItemNoti(this, item);

				// 클라에 통보
				S_EquipItem equipOkItem = new S_EquipItem();
				equipOkItem.ItemDbId = equipPacket.ItemDbId;
				equipOkItem.Equipped = equipPacket.Equipped;
				Session.Send(equipOkItem);
			}
		}
	}
}
