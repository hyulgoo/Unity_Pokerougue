using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICommonScene : UICommonBase
{
    public override void Init()
	{
		Managers.UI.SetCanvas(gameObject, false);

		Screen.SetResolution(600, 480, false);
	}
}
