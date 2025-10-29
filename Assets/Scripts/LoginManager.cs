using TMPro;
using Unity.Netcode; // Netcode 네임스페이스 추가
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/**
 * Netcode (데디케이티드 서버) 흐름에 맞게 수정된 LoginManager.
 * 1. "Join" 버튼은 씬을 로드하는 대신 StartClient()를 호출합니다.
 * 2. 서버 만원(Full) 등의 에러를 PlayerPrefs가 아닌 NetworkManager의 콜백으로 처리합니다.
 */
public class LoginManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button joinButton;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Color Selection")]
    [SerializeField] private Button[] colorButtons;
    [SerializeField] private Image selectedColorIndicator;

    // (색상 배열은 기존과 동일)
    private Color[] availableColors = new Color[]
    {
        new Color(1.0f, 0.3f, 0.3f), // 빨강
        new Color(0.3f, 1.0f, 0.3f), // 초록
        new Color(0.3f, 0.3f, 1.0f), // 파랑
        new Color(1.0f, 1.0f, 0.3f), // 노랑
        new Color(1.0f, 0.3f, 1.0f), // 마젠타
        new Color(0.3f, 1.0f, 1.0f), // 청록
        new Color(1.0f, 0.6f, 0.3f), // 주황
        new Color(0.6f, 0.3f, 1.0f)  // 보라
    };

    private int selectedColorIndex = 0;

    private void Start()
    {
        joinButton.onClick.AddListener(OnJoinButtonClicked);
        nicknameInput.onSubmit.AddListener((text) => OnJoinButtonClicked());

        SetupColorButtons();
        selectedColorIndex = PlayerPrefs.GetInt("PlayerColorIndex", 0);
        UpdateColorIndicator();

        // C++ 서버의 에러 처리는 제거합니다.
        // if (PlayerPrefs.HasKey("ServerFullError")) { ... }

        // (중요) Netcode 연결 콜백을 등록합니다.
        // NetworkManager가 DontDestroyOnLoad이므로, 이 스크립트가 파괴되기 전에
        // 콜백을 등록하고 해제하는 것이 좋습니다.
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }
    }

    private void OnDestroy()
    {
        // 씬이 파괴될 때 등록한 콜백을 해제합니다.
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }

    /// <summary>
    /// Netcode 서버로부터 연결이 끊겼을 때 (또는 거부당했을 때) 호출됩니다.
    /// </summary>
    private void HandleClientDisconnect(ulong clientId)
    {
        // 이 콜백은 메인 스레드에서 호출되지 않을 수 있으므로,
        // UI 로직은 Update 등에서 처리하거나 MainThread 디스패처를 써야 하지만,
        // 여기서는 간단히 에러 메시지를 표시합니다.

        // NetworkManager가 씬을 자동으로 언로드하고 LoginScene으로 돌아오게 할 수도 있습니다.
        // 지금은 "서버 만원" 에러 처리를 대체합니다.

        // 서버가 "연결 승인(Connection Approval)"에서 보낸 "Reason"을 가져옵니다.
        string reason = "Connection failed.";
        if (NetworkManager.Singleton.DisconnectReason != null && NetworkManager.Singleton.DisconnectReason != "")
        {
            reason = NetworkManager.Singleton.DisconnectReason;
        }

        ShowError(reason);
        joinButton.interactable = true; // "Join" 버튼 다시 활성화
    }

    private void OnJoinButtonClicked()
    {
        string nickname = nicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            ShowError("Please enter a nickname!");
            return;
        }
        if (nickname.Length < 2 || nickname.Length > 16)
        {
            ShowError("Nickname must be 2-16 characters!");
            return;
        }

        // 닉네임과 색상 저장 (이 로직은 PlayerController가 사용하므로 OK)
        PlayerPrefs.SetString("PlayerNickname", nickname);
        PlayerPrefs.SetInt("PlayerColorIndex", selectedColorIndex);
        Color selectedColor = availableColors[selectedColorIndex];
        PlayerPrefs.SetFloat("PlayerColorR", selectedColor.r);
        PlayerPrefs.SetFloat("PlayerColorG", selectedColor.g);
        PlayerPrefs.SetFloat("PlayerColorB", selectedColor.b);
        PlayerPrefs.Save();

        Debug.Log($"Saved: {nickname}, Color Index: {selectedColorIndex}");

        // UI를 "연결 중..." 상태로 변경
        joinButton.interactable = false;
        ShowError("Connecting...");

        // (핵심 수정) 씬을 로드하는 대신, 서버에 연결을 시도합니다.
        // NetworkManager가 LoginScene에 DontDestroyOnLoad로 존재해야 합니다.
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.StartClient();
        }
        else
        {
            ShowError("NetworkManager not found!");
            joinButton.interactable = true;
        }
    }

    // (이하 SetupColorButtons, OnColorSelected, UpdateColorIndicator, ShowError, HideError는 기존과 동일)

    private void SetupColorButtons()
    {
        if (colorButtons == null || colorButtons.Length == 0)
        {
            Debug.LogWarning("Color buttons not assigned!");
            return;
        }

        for (int i = 0; i < colorButtons.Length && i < availableColors.Length; i++)
        {
            int index = i; // 클로저를 위한 로컬 변수
            colorButtons[i].GetComponent<Image>().color = availableColors[i];
            colorButtons[i].onClick.AddListener(() => OnColorSelected(index));
        }
    }

    private void OnColorSelected(int colorIndex)
    {
        selectedColorIndex = colorIndex;
        UpdateColorIndicator();
        Debug.Log($"Selected color index: {colorIndex}");
    }

    private void UpdateColorIndicator()
    {
        if (selectedColorIndicator != null && selectedColorIndex < availableColors.Length)
        {
            selectedColorIndicator.color = availableColors[selectedColorIndex];
        }

        if (colorButtons != null)
        {
            for (int i = 0; i < colorButtons.Length; i++)
            {
                Outline outline = colorButtons[i].GetComponent<Outline>();
                if (outline == null)
                {
                    outline = colorButtons[i].gameObject.AddComponent<Outline>();
                }

                if (i == selectedColorIndex)
                {
                    outline.effectColor = Color.white;
                    outline.effectDistance = new Vector2(3, 3);
                    outline.enabled = true;
                }
                else
                {
                    outline.enabled = false;
                }
            }
        }
    }

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);

            // 연결 실패 시에는 HideError를 호출하지 않도록 Invoke 제거
            // Invoke(nameof(HideError), 3f); 
        }
    }

    private void HideError()
    {
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }
}
