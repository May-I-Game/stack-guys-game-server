using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject dummyPrefab;

    private Dictionary<int, GameObject> dummies = new Dictionary<int, GameObject>();

    private CameraFollow cameraFollow;

    void HandleGameState(JObject obj)
    {
        // 더미 업데이트
        UpdateDummies(obj);
    }

    void UpdateDummies(JObject obj)
    {
        if (!obj.ContainsKey("dummies"))
            return;

        JArray dummiesArray = (JArray)obj["dummies"];
        HashSet<int> currentDummies = new HashSet<int>();

        foreach (JObject dummyData in dummiesArray)
        {
            int id = dummyData["id"].Value<int>();
            JArray pos = (JArray)dummyData["pos"];

            currentDummies.Add(id);

            Vector3 position = new Vector3(
                pos[0].Value<float>(),
                pos[1].Value<float>(),
                pos[2].Value<float>()
            );

            // 더미가 없으면 생성
            if (!dummies.ContainsKey(id))
            {
                CreateDummy(id, position);
            }
            else
            {
                // 위치 업데이트 (부드러운 보간)
                GameObject dummy = dummies[id];
                dummy.transform.position = Vector3.Lerp(
                    dummy.transform.position,
                    position,
                    Time.deltaTime * 10f
                );
            }
        }

        // 서버에 없는 더미 제거
        List<int> toRemove = new List<int>();
        foreach (var kvp in dummies)
        {
            if (!currentDummies.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (int id in toRemove)
        {
            Debug.Log($"Removing dummy {id}");
            Destroy(dummies[id]);
            dummies.Remove(id);
        }
    }

    void CreateDummy(int id, Vector3 position)
    {
        if (dummyPrefab == null)
        {
            Debug.LogWarning("Dummy Prefab is not assigned!");
            return;
        }

        GameObject dummyObj = Instantiate(dummyPrefab, position, Quaternion.identity);
        dummyObj.name = $"Dummy_{id}";

        dummies[id] = dummyObj;
    }

    public int GetDummyCount() => dummies.Count;
}
