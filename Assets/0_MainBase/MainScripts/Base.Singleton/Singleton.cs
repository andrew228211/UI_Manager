using Unity.VisualScripting;
using UnityEngine;
namespace Base.Singleton
{
    public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;
        [Header("Singleton Settings")]
        [SerializeField] private bool _dontDestroyOnLoad = true;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                     if (_instance == null)
                    {
                        Debug.Log("Create new singleton: " + typeof(T).ToString() + " in scene");
                        var go = new GameObject { name = typeof(T).Name };
                        _instance = go.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        private  void Awake()
        {
            if (_instance == null)
            {
               _instance = this as T; 
                OnAwake();
            }
            else
            {
                if (_dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
                else
                {
                    Debug.LogWarning("Singleton instance of " + typeof(T).ToString() + " already exists. Destroying duplicate.");
                    Destroy(gameObject);
                }
            }
        }

        protected abstract void OnAwake();

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

    }
}

