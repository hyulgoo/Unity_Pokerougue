using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class UI_BattleScene : UI_Scene
{
    int _myMonsterId;
    int _enemyMonsterId;
    string _announceText = "";

    Sprite[] _hpbarSprites;
    int[] _HpbarSpriteNum = new int[(int)TargetType.End] { -1, -1 };

    enum GameObjects
    {
        MyHpbar,
        EnemyHpbar,
        End
    }

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

        Image_My,
        Image_Enemy,
    }

    enum Texts
    {
        Text_MyName,
        Text_EnemyName,
        Announce
    }

    public override void Init()
    {
        base.Init();

        BindObject(typeof(GameObjects));
        BindImage(typeof(Images));
        BindText(typeof(Texts));
    }

    public void SetPlayerInfo()
    {
        _hpbarSprites = Managers.Resource.LoadAll<Sprite>("Sprite/ui/overlay_hp");
        _myMonsterId = Managers.Object.MyPlayer.GetCurPokemonData().id;
        _enemyMonsterId = Managers.Object.Enemy.GetCurPokemonData().id;

        SetBattlePokemonInfo();
    }

    public void SetField(int type)
    {
        Arenas arenatype = (Arenas)type;
        // 필드를 불러옴
        const string path = "Sprite/arenas/";
        string arenaName = path + arenatype.ToString();

        string fieldName = arenaName + "_bg";
        GetImage((int)Images.BackGround).sprite = Managers.Resource.Load<Sprite>(fieldName);
        string myfoothold = arenaName + "_a";
        GetImage((int)Images.My_FootHold).sprite = Managers.Resource.Load<Sprite>(myfoothold);
        string enemyfoothold = arenaName + "_b";
        GetImage((int)Images.Enemy_FootHold).sprite = Managers.Resource.Load<Sprite>(enemyfoothold);

        if (GetImage((int)Images.BackGround).sprite == null)
            Debug.Log($"{fieldName}");
    }

    public void SetBattlePokemonInfo()
    {
        // 내 포켓몬 이름 설정
        GetText((int)Texts.Text_MyName).text = Managers.Data.MonsterDict[_myMonsterId].name;
        Sprite myImage = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/back/{_myMonsterId}")[0]; ;
        GetImage((int)Images.Image_My).sprite = myImage;

        // 상대방 포켓몬 이름 설정
        GetText((int)Texts.Text_EnemyName).text = Managers.Data.MonsterDict[_enemyMonsterId].name;
        Sprite enemyImage = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/{_enemyMonsterId}")[0];
        GetImage((int)Images.Image_Enemy).sprite = enemyImage;

        SetHPBar(targetRatio: 1f, isEnemy: true, setByHandler : false);
        SetHPBar(targetRatio: 1f, isEnemy: false, setByHandler : false);

        MyTurn();
    }

    public void MyTurn()
    {
        if (Managers.Object.MyTurn)
        {
            Managers.UI.ShowPopupUI<UI_SelectBehaviorPopup>();
            SetAnnounce($"{Managers.Data.MonsterDict[_myMonsterId].name}는 무엇을 할까?");
        }
        else
        {
            SetAnnounce("상대 차례를 기다리는 중");
        }
    }

    public void SetAnnounce(string announce, bool setByHandler = true)
    {
        _announceText = announce;
        StartCoroutine("SetText", setByHandler);
    }

    #region HP

    const float _fillSpeed = 0.5f;
    const float targetDiff = 0.01f;

    public void SetHPBar(float targetRatio, bool isEnemy = false, bool setByHandler = true)
    {
        string coroutineName = isEnemy ? "SetEnemyHPbar" : "SetMyHpbar";
        int target = isEnemy ? (int)GameObjects.EnemyHpbar : (int)GameObjects.MyHpbar;

        StartCoroutine(coroutineName, (targetRatio, target, setByHandler));
    }

    IEnumerator SetHPbar((float, int, bool) data)
    {
        float targetRatio = data.Item1;
        int target = data.Item2;
        bool setByHandle = data.Item3;
        float curRatio;
        
        // 수치가 목표 값에 도달할 때까지 반복
        while(targetRatio - GetObject(target).GetComponent<Slider>().value < targetDiff)
        {
            curRatio = GetObject(target).GetComponent<Slider>().value + (_fillSpeed * Time.deltaTime);
            GetObject(target).GetComponent<Slider>().value = curRatio;

            int hpstate = 2 - (int)(curRatio / 0.34f);
            if (_HpbarSpriteNum[target] != hpstate)
            {
                _HpbarSpriteNum[target] = hpstate;
                GameObject fillObj = Util.FindChild(GetObject(target), "Fill", true);
                fillObj.GetComponent<Image>().sprite = _hpbarSprites[hpstate];// 다음 프레임까지 대기
            }

            yield return null;
        }

        //if (setByHandle)
        //    Managers.Job.Excute();
    }

    #endregion

    IEnumerator SetText(bool setbyHandler = true)
    {
        GetText((int)Texts.Announce).text = "";
        for(int i = 0; i < _announceText.Length; ++i)
        {
            GetText((int)Texts.Announce).text += _announceText[i];
            yield return new WaitForSeconds(Managers.UI.ChatSpeed);
        }

        if (setbyHandler)
            Managers.Job.Excute();
    }
}
