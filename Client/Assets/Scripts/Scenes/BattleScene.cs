using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Battle;
        Managers.UI.ShowSceneUI<UIBattleScene>();
    }

    public override void Clear() {}
}
