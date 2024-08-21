using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_BattleScene : UI_Scene
{
    Sprite[] _hpbar;
    enum Images
    {
        Image_EnemyLevel_0,
        Image_EnemyLevel_00,
        Image_EnemyLevel_000,

        Image_MyLevel_0,
        Image_MyLevel_00,
        Image_MyLevel_000,

        BackGround,
        Enemy_FootHold,
        My_FootHold,
    }

    enum Texts
    {
        Text_MyName,
        Text_EnemyName,
    }

    public override void Init()
    {
        base.Init();

        Bind<Image>(typeof(Images));
        Bind<Text>(typeof(Texts));

        SetRandomField();

        _hpbar = Managers.Resource.LoadAll<Sprite>("Sprite/ui/overlay_hp");
    }

    void SetRandomField()
    {
        // 랜덤으로 필드를 불러옴
        int type = Random.Range(0, (int)Arenas.End);
        Arenas arenatype = (Arenas)type;
        string path = "Sprite/arenas/";
        string arenaName = path + arenatype.ToString();

        string fieldName = arenaName + "_bg";
        GetImage((int)Images.BackGround).sprite = Managers.Resource.Load<Sprite>(fieldName);
        string myfoothold = arenaName + "_a";
        GetImage((int)Images.My_FootHold).sprite = Managers.Resource.Load<Sprite>(myfoothold);
        string enemyfoothold = arenaName + "_b";
        GetImage((int)Images.Enemy_FootHold).sprite = Managers.Resource.Load<Sprite>(enemyfoothold);
    }

    public void SetBattleMonsterInfo()
    {
        // 내 포켓몬 이름 설정
        //GetText((int)Texts.Text_MyName).text = Managers.Object.MyPlayer.GetCurMonsterName();

        // 상대방 포켓몬 이름 설정
        //GetText((int)Texts.Text_EnemyName).text = Managers.Object.
    }
}
