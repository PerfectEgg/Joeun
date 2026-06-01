using UnityEngine;

// ==========================================
// 싱글톤 코어 클래스
// 설명: 싱글톤 패턴을 구현합니다.
// ==========================================
public class SingletonCore<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindFirstObjectByType<T>();

                if (obj == null)
                {
                    var newObj = new GameObject("T");

                    instance = newObj.AddComponent<T>();
                }
            }
            
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}