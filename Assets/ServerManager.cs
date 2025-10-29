using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerManager : MonoBehaviour
{
    void Start()
    {
        // === 데디케이티드 서버 빌드인지 확인 ===
        if (Application.isBatchMode)
        {
            // === 서버 경로 ===
            Debug.Log("--- SERVER BUILD DETECTED (Batch Mode) ---");

            // ⭐ [로그 추가] 1. 콜백 이벤트 등록
            // 서버가 시작되기 *전에* 콜백을 등록합니다.
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

            NetworkManager.Singleton.StartServer();
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        }
        else
        {
            // === 클라이언트 경로 ===
            Debug.Log("--- CLIENT BUILD DETECTED ---");
        }
    }

    // ⭐ [로그 추가] 2. 클라이언트 접속 시 호출될 함수
    private void HandleClientConnected(ulong clientId)
    {
        // 이 로그는 서버의 콘솔(-batchmode)에만 표시됩니다.
        Debug.Log($"[Server Log] Client connecting... Client ID: {clientId}");
        Debug.Log($"[Server Log] Total players now: {NetworkManager.Singleton.ConnectedClients.Count}");
    }

    // ⭐ [로그 추가] 3. 클라이언트 접속 해제 시 호출될 함수
    private void HandleClientDisconnect(ulong clientId)
    {
        // 이 로그는 서버의 콘솔(-batchmode)에만 표시됩니다.
        Debug.Log($"[Server Log] Client disconnected. Client ID: {clientId}");
        Debug.Log($"[Server Log] Total players now: {NetworkManager.Singleton.ConnectedClients.Count}");
    }
}
