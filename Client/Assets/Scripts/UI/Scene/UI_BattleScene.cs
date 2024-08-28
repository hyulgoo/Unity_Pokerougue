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

        SetHPBar(0, true);
        SetHPBar(0, false);
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
    bool _isEnemy = false;

    public void SetHPBar(int diff, bool isEnemy = false)
    {
        _isEnemy = isEnemy;
        PokemonData data = isEnemy ? Managers.Data.MonsterDict[_enemyMonsterId]: Managers.Data.MonsterDict[_myMonsterId];
        int maxHp = data.info.Hp;
        _targetValue = maxHp - diff;
        if(isEnemy)
        {
            StartCoroutine(SetEnemyHPbar());
        }
        else
        {
            StartCoroutine(SetMyHpbar());
        }
    }

    IEnumerator SetEnemyHPbar()
    {
        float curvalue = _prevValue;
        float maxHP = (float)Managers.Data.MonsterDict[_enemyMonsterId].info.Hp;

        // 수치가 목표 값에 도달할 때까지 반복
        do
        {
            // 현재 수치와 50 감소한 값을 계산
            float nextValue = Mathf.Max(curvalue - 50f, _targetValue);

            // 1초 동안의 선형 보간 수행
            float elapsedTime = 0f;
            while (elapsedTime < 1f)
            {
                elapsedTime += Time.deltaTime;
                curvalue = Mathf.Lerp(curvalue, nextValue, elapsedTime / 1f);

                // 여기서 원하는 작업 수행 (예: UI 업데이트)
                int target = _isEnemy ? (int)GameObjects.EnemyHpbar : (int)GameObjects.MyHpbar;
                float newvalue = curvalue / maxHP;
                GetObject(target).GetComponent<Slider>().value = newvalue;

                Sprite hpsprite = null;
                if (GetObject(target).GetComponent<Slider>().value < 0.33f)
                {
                    hpsprite = _hpbarSprites[2];
                }
                else if (GetObject(target).GetComponent<Slider>().value > 0.66f)
                {
                    hpsprite = _hpbarSprites[0];
                }
                else
                {
                    hpsprite = _hpbarSprites[1];
                }

                Util.FindChild(GetObject(target), "Fill", true).GetComponent<Image>().sprite = hpsprite;
                // 다음 프레임까지 대기
                yield return null;
            }

            // 정확히 다음 값으로 설정
            curvalue = nextValue;
        } while (curvalue > _targetValue);
    }

    IEnumerator SetMyHpbar()
    {
        float curvalue = _prevValue;
        float maxHP = (float)Managers.Data.MonsterDict[_myMonsterId].info.Hp;

        // 수치가 목표 값에 도달할 때까지 반복
        do
        {
            // 현재 수치와 50 감소한 값을 계산
            float nextValue = Mathf.Max(curvalue - 50f, _targetValue);

            // 1초 동안의 선형 보간 수행
            float elapsedTime = 0f;
            while (elapsedTime < 1f)
            {
                elapsedTime += Time.deltaTime;
                curvalue = Mathf.Lerp(curvalue, nextValue, elapsedTime / 1f);

                // 여기서 원하는 작업 수행 (예: UI 업데이트)
                int target = _isEnemy ? (int)GameObjects.EnemyHpbar : (int)GameObjects.MyHpbar;
                float newvalue = curvalue / maxHP;
                GetObject(target).GetComponent<Slider>().value = newvalue;

                Sprite hpsprite = null;
                if (GetObject(target).GetComponent<Slider>().value < 0.33f)
                {
                    hpsprite = _hpbarSprites[2];
                }
                else if (GetObject(target).GetComponent<Slider>().value > 0.66f)
                {
                    hpsprite = _hpbarSprites[0];
                }
                else
                {
                    hpsprite = _hpbarSprites[1];
                }

                Util.FindChild(GetObject(target), "Fill", true).GetComponent<Image>().sprite = hpsprite;
                // 다음 프레임까지 대기
                yield return null;
            }

            // 정확히 다음 값으로 설정
            curvalue = nextValue;
        } while (curvalue > _targetValue);
    }

    #endregion
}
