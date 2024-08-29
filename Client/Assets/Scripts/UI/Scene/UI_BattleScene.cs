using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class UI_BattleScene : UI_Scene
{
    int _myMonsterId;
    int _enemyMonsterId;

    Sprite[] _hpbarSprites;

    enum GameObjects
    {
        MyHpbar,
        EnemyHpbar
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
        Text
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
        _myMonsterId = Managers.Object.MyPlayer.GetCurMonsterData().id;
        _enemyMonsterId = Managers.Object.Enemy.GetCurMonsterData().id;

        SetBattleMonsterInfo();
    }

    public void SetField(int type)
    {
        Arenas arenatype = (Arenas)type;
        // 필드를 불러옴
        string path = "Sprite/arenas/";
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

    public void SetBattleMonsterInfo()
    {
        // 내 포켓몬 이름 설정
        GetText((int)Texts.Text_MyName).text = Managers.Data.MonsterDict[_myMonsterId].name;
        Sprite myImage = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/back/{_myMonsterId}")[0]; ;
        GetImage((int)Images.Image_My).sprite = myImage;

        // 상대방 포켓몬 이름 설정
        GetText((int)Texts.Text_EnemyName).text = Managers.Data.MonsterDict[_enemyMonsterId].name;
        Sprite enemyImage = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/{_enemyMonsterId}")[0];
        GetImage((int)Images.Image_Enemy).sprite = enemyImage;

        SetHPBar(0, isEnemy: true);
        SetHPBar(0, isEnemy: false);
        MyTurn();
    }

    public void MyTurn()
    {
        if (Managers.Object.MyTurn)
        {
            Managers.UI.ShowPopupUI<UI_SelectBehaviorPopup>();
            SetText($"{Managers.Data.MonsterDict[_myMonsterId].name}는 무엇을 할까?");
        }
        else
        {
            SetText("상대 차례를 기다리는 중");
        }
    }

    public void SetText(string text)
    {
        GetText((int)Texts.Text).text = text;
    }

    #region HP

    int _prevValue;
    int _targetValue;
    int _enemyprevValue;
    int _enemytargetValue;

    public void SetHPBar(int diff, bool isEnemy = false)
    {
        PokemonData data = isEnemy ? Managers.Data.MonsterDict[_enemyMonsterId] : Managers.Data.MonsterDict[_myMonsterId];
        int maxHp = data.info.Hp;
        if (isEnemy)
        {
            _enemytargetValue = maxHp - diff;
            StartCoroutine(SetEnemyHPbar());
        }
        else
        {
            _targetValue = maxHp - diff;
            StartCoroutine(SetMyHpbar());
        }
    }

    IEnumerator SetEnemyHPbar()
    {
        float curvalue = _enemyprevValue;
        float maxHP = (float)Managers.Data.MonsterDict[_enemyMonsterId].info.Hp;
        float nextValue = 0;
        float diff = 0;
        float fillSpeed = 75 * Time.deltaTime;

        // 수치가 목표 값에 도달할 때까지 반복
        do
        {
            curvalue += fillSpeed;

            // 현재 수치와 50 감소한 값을 계산
            nextValue = Mathf.Max(curvalue - 50f, _enemytargetValue);

            // 여기서 원하는 작업 수행 (예: UI 업데이트)
            float newvalue = curvalue / maxHP;
            GetObject((int)GameObjects.EnemyHpbar).GetComponent<Slider>().value = newvalue;

            int hpstate = 2 - (int)(newvalue / 0.34f);
            GameObject fillObj = Util.FindChild(GetObject((int)GameObjects.EnemyHpbar), "Fill", true);
            fillObj.GetComponent<Image>().sprite = _hpbarSprites[hpstate];// 다음 프레임까지 대기

            diff = _enemytargetValue - curvalue;

            yield return null;
        } while (diff > 1f);

        GetObject((int)GameObjects.EnemyHpbar).GetComponent<Slider>().value = _enemytargetValue / maxHP;
    }

    IEnumerator SetMyHpbar()
    {
        float curvalue = _prevValue;
        float maxHP = (float)Managers.Data.MonsterDict[_myMonsterId].info.Hp;
        float nextValue = 0;
        float diff = 0;
        float fillSpeed = 50 * Time.deltaTime;

        // 수치가 목표 값에 도달할 때까지 반복
        do
        {
            curvalue += fillSpeed;

            // 현재 수치와 50 감소한 값을 계산
            nextValue = Mathf.Max(curvalue - 50f, _targetValue);

            // 여기서 원하는 작업 수행 (예: UI 업데이트)
            float newvalue = curvalue / maxHP;
            GetObject((int)GameObjects.MyHpbar).GetComponent<Slider>().value = newvalue;

            int hpstate = 2 - (int)(newvalue / 0.34f);
            GameObject fillObj = Util.FindChild(GetObject((int)GameObjects.MyHpbar), "Fill", true);
            fillObj.GetComponent<Image>().sprite = _hpbarSprites[hpstate];// 다음 프레임까지 대기

            diff = _targetValue - curvalue;

            yield return null;
        } while (diff > 1f);

        GetObject((int)GameObjects.MyHpbar).GetComponent<Slider>().value = _targetValue / maxHP;
    }

    #endregion
}
