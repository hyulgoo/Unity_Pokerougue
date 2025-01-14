using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Lobby;
        Managers.UI.ShowSceneUI<UILobbyScene>();
        Managers.Sound.Play("bgm/menu", Define.Sound.Bgm);
    }

    public override void Clear() {}
}
