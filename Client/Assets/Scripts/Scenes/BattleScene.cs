using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Battle;

        UI_BattleScene scene = Managers.UI.ShowSceneUI<UI_BattleScene>();
    }

    public override void Clear()
    {
        throw new System.NotImplementedException();
    }
}
