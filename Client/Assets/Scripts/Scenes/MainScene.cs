using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Login;

        Screen.SetResolution(800, 600, false);

        GameObject player = Managers.Resource.Instantiate("Creature/MyPlayer");
        player.name = "Player";

        Managers.UI.ShowSceneUI<UIMainScene>();
    }

    public override void Clear() {}
}
