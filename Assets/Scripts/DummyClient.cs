using NativeWebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using UnityEngine; // Debug.Log, Vector3, Random 등을 위해 필요합니다.

/// <summary>
/// 서버 테스트를 위한 개별 더미 클라이언트 클래스입니다.
/// MonoBehaviour가 아니며, DummyClientManager에 의해 관리됩니다.
/// </summary>
public class DummyClient
{
    private WebSocket websocket;
    private string serverUrl;
    private string nickname;
    private int playerId = -1;
    private float[] color;

    private bool isConnected = false;
    private bool hasJoined = false;

    // 입력 시뮬레이션용
    private float lastInputTime = 0f;
    private float inputInterval = 0.1f; // 1초에 10번 입력 전송
    private Vector3 currentMovement = Vector3.zero;

    public DummyClient(string url, string name)
    {
        this.serverUrl = url;
        this.nickname = name;

        // 랜덤 색상 생성
        this.color = new float[] { UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value };

        Connect();
    }

    private async void Connect()
    {
        websocket = new WebSocket(serverUrl);

        websocket.OnOpen += OnOpen;
        websocket.OnMessage += OnMessage;
        websocket.OnClose += OnClose;
        websocket.OnError += OnError;

        try
        {
            await websocket.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError($"[Dummy {nickname}] Connection Exception: {e.Message}");
        }
    }

    private void OnOpen()
    {
        Debug.Log($"[Dummy {nickname}] Connected.");
        isConnected = true;
        SendJoinRequest();
    }

    private void SendJoinRequest()
    {
        var joinRequest = new
        {
            type = 1, // JOIN_REQUEST
            nickname = this.nickname,
            color = this.color
        };

        string json = JsonConvert.SerializeObject(joinRequest);
        SendMessage(json);
        Debug.Log($"[Dummy {nickname}] Sent Join Request.");
    }

    private void OnMessage(byte[] data)
    {
        string json = Encoding.UTF8.GetString(data);

        try
        {
            JObject obj = JObject.Parse(json);
            int msgType = obj["type"].Value<int>();

            if (msgType == 2) // JOIN_RESPONSE
            {
                HandleJoinResponse(obj);
            }
            // 이 더미 클라이언트는 GAME_STATE(4)는 무시합니다.
        }
        catch (Exception e)
        {
            Debug.LogError($"[Dummy {nickname}] JSON Parse Error: {e.Message}\n{json}");
        }
    }

    private void HandleJoinResponse(JObject obj)
    {
        bool success = obj["success"].Value<bool>();

        if (success)
        {
            this.playerId = obj["playerId"].Value<int>();
            hasJoined = true;
            Debug.Log($"[Dummy {nickname}] Joined successfully as Player {playerId}.");

            // 접속 성공 시 무작위 이동 시작
            StartSimulatingMovement();
        }
        else
        {
            Debug.LogError($"[Dummy {nickname}] Join failed: {obj["message"]}");
            Close(); // 접속 실패 시 연결 종료
        }
    }

    private void StartSimulatingMovement()
    {
        // 간단히 랜덤한 방향으로 계속 움직이도록 설정
        currentMovement = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized;
    }

    /// <summary>
    /// DummyClientManager가 매 프레임 호출해줘야 합니다.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (websocket == null) return;

        // 메시지 큐 처리 (필수)
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
#endif

        if (!hasJoined || !isConnected) return;

        // 주기적으로 입력 전송
        lastInputTime += deltaTime;
        if (lastInputTime >= inputInterval)
        {
            lastInputTime = 0f;

            // 5% 확률로 이동 방향 변경
            if (UnityEngine.Random.value < 0.05f)
            {
                StartSimulatingMovement();
            }

            SendInput(currentMovement);
        }
    }

    private void SendInput(Vector3 movement)
    {
        if (playerId < 0) return;

        var input = new
        {
            type = 3, // PLAYER_INPUT
            playerId = this.playerId,
            x = movement.x,
            y = movement.y,
            z = movement.z
        };

        string json = JsonConvert.SerializeObject(input);
        SendMessage(json);
    }

    private void SendMessage(string message)
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            websocket.SendText(message);
        }
    }

    private void OnError(string e)
    {
        Debug.LogError($"[Dummy {nickname}] Error: {e}");
    }

    private void OnClose(WebSocketCloseCode code)
    {
        Debug.Log($"[Dummy {nickname}] Disconnected: {code}");
        isConnected = false;
        hasJoined = false;
    }

    /// <summary>
    /// 외부에서 이 더미 클라이언트의 연결을 종료시킬 때 호출합니다.
    /// </summary>
    public async void Close()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            await websocket.Close();
        }
        websocket = null;
        isConnected = false;
        hasJoined = false;
    }
}
