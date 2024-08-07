using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Login;

        GameObject player = Managers.Resource.Instantiate("Creature/MyPlayer");
        player.name = "Player";

        Managers.UI.ShowSceneUI<UI_MainScene>();
    }

    public override void Clear()
    {
        
    }
}
