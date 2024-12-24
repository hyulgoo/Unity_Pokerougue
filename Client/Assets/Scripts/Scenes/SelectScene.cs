using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectScene : BaseScene
{
    void Start() {}

    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Select;
        Managers.UI.ShowSceneUI<UISelectPokemonScene>();
    }

    void Update() {}

    public override void Clear() {}
}
