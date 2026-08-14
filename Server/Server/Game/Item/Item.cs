using Google.Protobuf.Protocol;
using Server.Data;
using Server.DB;

namespace Server.Game
{
    public class Item
    {
        public Item(ItemType itemType)
        {
            ItemType = itemType;
        }

        public ItemInfo Info { get; } = new ItemInfo();

        public int ItemDbId
        {
            get => Info.ItemDbId;
            private set => Info.ItemDbId = value;
        }

        public int TemplateId
        {
            get => Info.TemplateId;
            set => Info.TemplateId = value;
        }

        public int Count
        {
            get => Info.Count;
            set => Info.Count = value;
        }

        public int Slot
        {
            get => Info.Slot;
            set => Info.Slot = value;
        }

        public bool Equipped
        {
            get => Info.Equipped;
            set => Info.Equipped = value;
        }

        public ItemType ItemType { get; private set; }
        public bool Stackable { get; protected set; }

        public static Item MakeItem(ItemDb itemDb)
        {
            Item item = null;

            DataManager.ItemDict.TryGetValue(itemDb.TemplateId, out ItemData itemData);

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
        public Consumable(int templateId) : base(ItemType.Consumable)
        {
            Init(templateId);
        }

        public ConsumableType ConsumableType { get; private set; }
        public int MaxCount { get; set; }

        private void Init(int templateId)
        {
            ItemData itemData = null;
            DataManager.ItemDict.TryGetValue(templateId, out itemData);
            if (itemData != null && itemData.itemType != ItemType.Consumable)
                return;

            ConsumableData data = (ConsumableData)itemData;
            {
                if (data != null)
                {
                    TemplateId = data.id;
                    Count = 1;
                    MaxCount = data.maxCount;
                    ConsumableType = data.consumableType;
                    Stackable = data.maxCount > 1;
                }
            }
        }
    }
}