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
        Managers.Sound.Play("bgm/menu", Define.Sound.Bgm);
    }

    void Update() {}

    public override void Clear() {}
}
