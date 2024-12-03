using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Stat : UICommonBase
{
	enum Images
	{
		Slot_Helmet,
		Slot_Armor,
		Slot_Boots,
		Slot_Weapon,
		Slot_Shield
	}

	enum Texts
	{
		NameText,
		AttackValueText,
		DefenceValueText
	}

	bool _init = false;
	public override void Init()
	{
		Bind<Image>(typeof(Images));
		Bind<Text>(typeof(Texts));

		_init = true;
		RefreshUI();
	}

	public void RefreshUI()
	{
		if (_init == false)
			return;

		// 우선은 다 가린다
		Get<Image>((int)Images.Slot_Helmet).enabled = false;
		Get<Image>((int)Images.Slot_Armor).enabled = false;
		Get<Image>((int)Images.Slot_Boots).enabled = false;
		Get<Image>((int)Images.Slot_Weapon).enabled = false;
		Get<Image>((int)Images.Slot_Shield).enabled = false;

		// 채워준다
		foreach (Item item in Managers.Inven.Items.Values)
		{
			if (item.Equipped == false)
				continue;

			ItemData itemData = null;
			Managers.Data.ItemDict.TryGetValue(item.TemplateId, out itemData);
			Sprite icon = Managers.Resource.Load<Sprite>(itemData.iconPath);
		}
	}
}
