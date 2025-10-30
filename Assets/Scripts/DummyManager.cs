using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DummyManager : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject dummyPrefab; // 더미 프리팹 (NetworkObject 포함)

    [Header("Client UI Settings")]
    [SerializeField]
    [Tooltip("한 번에 생성할 더미 수")]
    private int numberOfDummiesToSpawn = 10;

    // 서버에 스폰된 '더미' 목록 (서버 전용)
    private Dictionary<ulong, List<DummyPlayer>> spawnedDummiesByClient = new();
    private int dummyCounter = 0;
    private int nextDummyId = 0;

    // 싱글톤 (편의상)
    public static DummyManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        // 서버에서만 콜백을 등록합니다.
        if (!IsServer) return;

        if (dummyPrefab == null)
        {
            Debug.LogError("[DummyManager] Dummy Prefab is not set!", this);
            return;
        }

        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    // ===================================================================
    // 클라이언트 UI 입력 처리 (DummyUIManager 역할)
    // ===================================================================
    private void Update()
    {
        // 로컬 플레이어 클라이언트만 입력을 처리하고 서버로 RPC 요청
        if (!IsClient) return;

        // --- 테스트용 UI ---
        // 1 키: 더미 1개 생성 요청
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SpawnDummiesServerRpc(1); // 서버로 요청
            Debug.Log("[Client Log] Requested to spawn 1 dummy.");
        }

        // 2 키: 더미 많이 생성 요청
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SpawnDummiesServerRpc(numberOfDummiesToSpawn); // 서버로 요청
            Debug.Log($"[Client Log] Requested to spawn {numberOfDummiesToSpawn} dummies.");
        }

        // 3 키: 모든 더미 삭제 요청
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            DeleteAllDummiesServerRpc(); // 서버로 요청
            Debug.Log("[Client Log] Requested to delete all dummies.");
        }
    }

    // ===================================================================
    // 서버 RPC (클라이언트 -> 서버 요청)
    // ===================================================================

    [ServerRpc(RequireOwnership = false)]
    private void SpawnDummiesServerRpc(int count, ServerRpcParams rpcParams = default)
    {
        // 서버에서만 실행되는 실제 스폰 로직 호출
        SpawnDummiesInternal(count, rpcParams.Receive.SenderClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeleteAllDummiesServerRpc(ServerRpcParams rpcParams = default)
    {
        // 서버에서만 실행되는 실제 삭제 로직 호출
        DeleteAllDummiesInternal(rpcParams.Receive.SenderClientId);
    }

    // ===================================================================
    // 서버 로직 (ServerDummyManager 역할)
    // ===================================================================

    // 서버만 실행: 실제 더미 스폰 처리
    private void SpawnDummiesInternal(int count, ulong requesterClientId)
    {
        if (!IsServer) return;

        Debug.Log($"[Server Log] Spawning {count} dummies requested by Client {requesterClientId}");

        if (dummyPrefab == null)
        {
            Debug.LogError("DummyPrefab is not assigned in CombinedDummyManager!");
            return;
        }

        if (!spawnedDummiesByClient.ContainsKey(requesterClientId))
        {
            spawnedDummiesByClient[requesterClientId] = new List<DummyPlayer>();
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
            GameObject dummyInstance = Instantiate(dummyPrefab, spawnPosition, Quaternion.identity);
            NetworkObject networkObject = dummyInstance.GetComponent<NetworkObject>();
            DummyPlayer dummyController = dummyInstance.GetComponent<DummyPlayer>();

            if (networkObject != null && dummyController != null)
            {
                networkObject.SpawnWithOwnership(requesterClientId, true);

                string dummyName = $"Dummy{nextDummyId++}";
                // 랜덤 색상 생성 (Hue만 랜덤하게 하여 너무 어둡거나 밝지 않게)
                Color randomColor = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f);

                dummyController.InitializeDummy(dummyName, randomColor);
                spawnedDummiesByClient[requesterClientId].Add(dummyController);
                dummyCounter++;
            }
            else
            {
                Debug.LogError("Dummy Prefab is missing NetworkObject or DummyPlayer component!");
                Destroy(dummyInstance);
            }
        }
        Debug.Log($"[Server Log] Spawned {count} dummies. Total dummies: {dummyCounter}");
    }

    // 서버만 실행: 실제 더미 삭제 처리
    private void DeleteAllDummiesInternal(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"[Server Log] Deleting all dummies requested by Client {clientId}");

        if (spawnedDummiesByClient.TryGetValue(clientId, out List<DummyPlayer> objectsToDespawn))
        {
            foreach (DummyPlayer dummy in objectsToDespawn)
            {
                if (dummy != null && dummy.NetworkObject != null && dummy.NetworkObject.IsSpawned)
                {
                    dummy.NetworkObject.Despawn(true);
                    dummyCounter--;
                }
            }
            spawnedDummiesByClient.Remove(clientId);
        }

        Debug.Log($"[Server Log] Client {clientId}'s All dummies deleted.");
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"[Server Log] Client {clientId} disconnected. Despawning objects.");

        // 이 클라이언트가 소유했던 모든 오브젝트(플레이어 + 더미)를 찾아 디스폰(파괴)합니다.
        DeleteAllDummiesInternal(clientId);
    }

    public override void OnNetworkDespawn()
    {
        // 서버에서만 콜백 해제
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }
}
