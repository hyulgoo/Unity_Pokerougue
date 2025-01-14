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
        Managers.Sound.Play("bgm/battle_rival", Define.Sound.Bgm);
    }

    public override void Clear() {}
}
