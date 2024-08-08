using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
	public class Item
	{
		public ItemInfo Info { get; } = new ItemInfo();

		public int ItemDbId
		{
			get { return Info.ItemDbId; }
			set { Info.ItemDbId = value; }
		}

		public int TemplateId
		{
			get { return Info.TemplateId; }
			set { Info.TemplateId = value; }
		}

		public int Count
		{
			get { return Info.Count; }
			set { Info.Count = value; }
		}

		public int Slot
		{
			get { return Info.Slot; }
			set { Info.Slot = value; }
		}

		public bool Equipped
		{
			get { return Info.Equipped; }
			set { Info.Equipped = value; }
		}

		public ItemType ItemType { get; private set; }
		public bool Stackable { get; protected set; }

		public Item(ItemType itemType)
		{
			ItemType = itemType;
		}

		public static Item MakeItem(ItemDb itemDb)
		{
			Item item = null;

			ItemData itemData = null;
			DataManager.ItemDict.TryGetValue(itemDb.TemplateId, out itemData);

			if (itemData == null)
				return null;

			switch (itemData.itemType)
			{
				case ItemType.Consumable:
					item = new Consumable(itemDb.TemplateId);
					break;
			}

			if (item != null)
			{
				item.ItemDbId = itemDb.ItemDbId;
				item.Count = itemDb.Count;
				item.Slot = itemDb.Slot;
				item.Equipped = itemDb.Equipped;
			}

			return item;
		}
	}

	public class Consumable : Item
	{
		public ConsumableType ConsumableType { get; private set; }
		public int MaxCount { get; set; }

		public Consumable(int templateId) : base(ItemType.Consumable)
		{
			Init(templateId);
		}

		void Init(int templateId)
		{
			ItemData itemData = null;
			DataManager.ItemDict.TryGetValue(templateId, out itemData);
			if (itemData.itemType != ItemType.Consumable)
				return;

			ConsumableData data = (ConsumableData)itemData;
			{
				TemplateId = data.id;
				Count = 1;
				MaxCount = data.maxCount;
				ConsumableType = data.consumableType;
				Stackable = (data.maxCount > 1);
			}
		}
	}
}
