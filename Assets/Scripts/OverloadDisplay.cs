using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class OverloadDisplay : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;

    [Header("Show Toggles")]
    [SerializeField] private bool showFPS = true;
    [SerializeField] private bool showDummyCount = true;
    [SerializeField] private bool showPing = true;

    [Header("References")]
    [SerializeField] private Text displayText;

    // FPS
    private float accum = 0.0f;
    private int frames = 0;
    private float timeleft;
    private float fps;

    // Ping (RTT)
    private float currentPing = 0f;

    // Dummy Count
    private int dummyCount = 0;

    private void Awake()
    {
        if (displayText == null)
            displayText = GetComponent<Text>();

        timeleft = updateInterval;
    }

    void Update()
    {
        // FPS 계산
        if (showFPS)
        {
            timeleft -= Time.deltaTime;
            accum += Time.timeScale / Time.deltaTime;
            ++frames;
        }

        // 업데이트 간격마다 모든 정보 갱신
        if (timeleft <= 0f)
        {
            if (showFPS)
            {
                fps = accum / frames;
                accum = 0f;
                frames = 0;
            }

            UpdateNetworkStats();
            UpdateDisplay();

            timeleft = updateInterval;
        }
    }

    private void UpdateNetworkStats()
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        if (transport == null)
            return;

        // Ping (RTT) 측정 - 클라이언트만
        if (showPing && NetworkManager.Singleton.IsClient)
        {
            currentPing = transport.GetCurrentRtt(0);
        }

        // Dummy Count
        if (showDummyCount)
        {
            dummyCount = DummyManager.Instance != null ? DummyManager.Instance.dummyCounter.Value : 0;
        }

        // Server Load는 OnNetworkTick에서 계산됨
    }

    void UpdateDisplay()
    {
        if (displayText == null)
            return;

        string display = "";

        // FPS
        if (showFPS)
        {
            Color fpsColor = Color.green;
            if (fps < 30)
                fpsColor = Color.red;
            else if (fps < 50)
                fpsColor = Color.yellow;

            display += $"<color=#{ColorUtility.ToHtmlStringRGB(fpsColor)}>FPS: {fps:F1}</color>\n";
        }

        // Ping
        if (showPing && NetworkManager.Singleton.IsClient)
        {
            Color pingColor = Color.green;
            if (currentPing > 50)
                pingColor = Color.red;
            else if (currentPing > 20)
                pingColor = Color.yellow;

            display += $"<color=#{ColorUtility.ToHtmlStringRGB(pingColor)}>Ping (RTT): {currentPing:F0} ms</color>\n";
        }

        // Dummy Count
        if (showDummyCount)
        {
            display += $"Dummy Count: {dummyCount}\n";
        }

        displayText.text = display.TrimEnd('\n');
    }
}