using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_BattleScene : UI_Scene
{
    PokemonData _myMonsterData;
    PokemonData _enemyMonsterData;

    Sprite[] _hpbar;
    enum GameObjects
    {
        MyHPbar,
        EnemyHPbar
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
    }

    public override void Init()
    {
        base.Init();

        Bind<GameObject>(typeof(GameObjects));
        Bind<Image>(typeof(Images));
        Bind<Text>(typeof(Texts));
    }

    public void SetPlayerInfo()
    {
        _hpbar = Managers.Resource.LoadAll<Sprite>("Sprite/ui/overlay_hp");
        _myMonsterData = Managers.Object.MyPlayer.GetCurMonsterData();
        _enemyMonsterData = Managers.Object.Opponent.GetCurMonsterData();

        SetBattleMonsterInfo();
    }

    public void SetField(int type)
    {
        // 필드를 불러옴
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
        GetText((int)Texts.Text_MyName).text = _myMonsterData.name;
        Sprite myImage = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/back/{_myMonsterData.id}")[0]; ;
        GetImage((int)Images.Image_My).sprite = myImage;

        // 상대방 포켓몬 이름 설정
        GetText((int)Texts.Text_EnemyName).text = _enemyMonsterData.name;
        Sprite enemyImage = Managers.Resource.LoadAll<Sprite>($"Sprite/pokemon/{_enemyMonsterData.id}")[0];
        GetImage((int)Images.Image_Enemy).sprite = enemyImage;
    }

    public void MyTurn()
    {
        Managers.UI.ShowPopupUI<UI_SelectBehaviorPopup>();
    }

    float _startValue;
    float _targetValue;
    bool _isEnemy = false;

    public void SetHPBar(int diff, bool isEnemy = false)
    {
        _isEnemy = isEnemy;
        PokemonData data = isEnemy ? _enemyMonsterData : _myMonsterData;
        _startValue = (float)data.info.Hp;
        _targetValue = _startValue - (float)diff;
        StartCoroutine(HPChangeAnimation());
    }

    IEnumerator HPChangeAnimation()
    {
        float curvalue = _startValue;
        int targetNumber = _isEnemy ? _enemyMonsterData.id : _myMonsterData.id;
        float maxHP = (float)Managers.Data.MonsterDict[targetNumber].info.Hp;

        // 수치가 목표 값에 도달할 때까지 반복
        while (curvalue > _targetValue)
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
                int target = _isEnemy ? (int)GameObjects.EnemyHPbar : (int)GameObjects.MyHPbar;
                GetObject(target).GetComponent<Slider>().value = curvalue / maxHP;

                // 다음 프레임까지 대기
                yield return null; 
            }

            // 정확히 다음 값으로 설정
            curvalue = nextValue;
        }
    }
}
