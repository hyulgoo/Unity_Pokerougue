using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Managers : MonoBehaviour
{
    static Managers s_instance; // 유일성이 보장된다
    public static Managers Instance { get { Init(); return s_instance; } } // 유일한 매니저를 갖고온다

    #region Contents
    InventoryManager _inven = new InventoryManager();
    MapManager _map = new MapManager();
    PlayerManager _player = new PlayerManager();
    NetworkManager _network = new NetworkManager();
    WebManager _web = new WebManager();
    UISelectManager _select = new UISelectManager();
    JobManager _job = new JobManager();

    public static InventoryManager Inven { get { return Instance._inven; } }
    public static MapManager Map { get { return Instance._map; } }
    public static PlayerManager Player { get { return Instance._player; } }
    public static NetworkManager Network { get { return Instance._network; } }
    public static WebManager Web { get { return Instance._web; } }
    public static UISelectManager Select { get { return Instance._select; } }
    public static JobManager Job { get { return Instance._job; } }
	#endregion

	#region Core
	DataManager _data = new DataManager();
    PoolManager _pool = new PoolManager();
    ResourceManager _resource = new ResourceManager();
    SceneManagerEx _scene = new SceneManagerEx();
    SoundManager _sound = new SoundManager();
    UIManager _ui = new UIManager();

    public static DataManager Data { get { return Instance._data; } }
    public static PoolManager Pool { get { return Instance._pool; } }
    public static ResourceManager Resource { get { return Instance._resource; } }
    public static SceneManagerEx Scene { get { return Instance._scene; } }
    public static SoundManager Sound { get { return Instance._sound; } }
    public static UIManager UI { get { return Instance._ui; } }
	#endregion

	void Start()
    {
        Init();
	}

    void Update()
    {
        _network.Update();
        _job.Update();
    }

    static void Init()
    {
        if (s_instance == null)
        {
			GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            DontDestroyOnLoad(go);
            s_instance = go.GetComponent<Managers>();

            s_instance._data.Init();
            s_instance._pool.Init();
            s_instance._sound.Init();
        }		
	}

    public static void Clear()
    {
        Sound.Clear();
        Scene.Clear();
        UI.Clear();
        Pool.Clear();
        Player.Clear();
    }
}
