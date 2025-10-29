using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// [필수] 이 스크립트는 Rigidbody가 필요하다고 명시
[RequireComponent(typeof(Rigidbody))]
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Nickname UI")]
    [SerializeField] protected Canvas nicknameCanvas;
    [SerializeField] protected Text nicknameText;

    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected float jumpForce = 8f; // [추가] 점프 힘

    protected Rigidbody rb;
    protected MeshRenderer meshRenderer;

    // 카메라 참조 (로컬 플레이어 전용)
    protected CameraFollow cameraFollow;

    // C++의 gameWorld_.setPlayerInput()을 위해 클라이언트가 보낸 입력 값
    protected Vector3 serverInputMovement = Vector3.zero;
    protected bool serverJumpQueued = false;

    // ===================================================================
    // 1. 상태 동기화 (C++의 GAME_STATE)
    // ===================================================================

    // NetworkVariableReadPermission.Everyone : 모두가 읽을 수 있음
    // NetworkVariableWritePermission.Server : 오직 서버만 쓸 수 있음

    protected NetworkVariable<Vector3> serverPosition = new NetworkVariable<Vector3>(
        Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    protected NetworkVariable<Quaternion> serverRotation = new NetworkVariable<Quaternion>(
        Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // 닉네임 동기화
    protected NetworkVariable<FixedString64Bytes> playerNickname = new NetworkVariable<FixedString64Bytes>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // 색상 동기화
    protected NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ===================================================================
    // 2. NGO 생명주기 (로컬 플레이어 설정)
    // ===================================================================
    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();

        // [수정] 서버에서는 Rigidbody를 켜고, 클라(원격)에서는 끕니다. (물리 충돌 방지)
        if (IsServer)
        {
            rb.isKinematic = false;
        }
        else
        {
            rb.isKinematic = true; // 물리 계산을 끕니다 (서버가 보내준 위치로만 이동)
            rb.useGravity = false; // 중력도 끕니다
        }

        playerNickname.OnValueChanged += OnNicknameChanged;
        playerColor.OnValueChanged += OnColorChanged;

        UpdateNicknameUI(playerNickname.Value.ToString());
        ApplyColor(playerColor.Value);

        // IsOwner: 이 오브젝트가 '내 것'인지 (로컬 플레이어인지) 확인
        if (IsOwner)
        {
            // 이 오브젝트가 '내 것'이라면 카메라가 따라가도록 설정
            if (Camera.main != null)
            {
                cameraFollow = Camera.main.GetComponent<CameraFollow>();
                if (cameraFollow != null)
                {
                    cameraFollow.SetTarget(this.transform);
                }
            }

            // '내' 정보를 서버로 전송합니다.
            string nickname = PlayerPrefs.GetString("PlayerNickname", "Player");
            Color color = new Color(
                PlayerPrefs.GetFloat("PlayerColorR", 1f),
                PlayerPrefs.GetFloat("PlayerColorG", 1f),
                PlayerPrefs.GetFloat("PlayerColorB", 1f)
            );

            SubmitInitialDataServerRpc(nickname, color);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (playerNickname != null) playerNickname.OnValueChanged -= OnNicknameChanged;
        if (playerColor != null) playerColor.OnValueChanged -= OnColorChanged;
    }

    protected virtual void Update()
    {
        // 닉네임 빌보드 (모든 플레이어)
        if (nicknameCanvas != null && Camera.main != null)
        {
            nicknameCanvas.transform.LookAt(Camera.main.transform);
            nicknameCanvas.transform.Rotate(0, 180, 0);
        }

        // (IsOwner)입력을 받고, 서버로 전송
        if (IsOwner)
        {
            HandleInput();
        }
    }

    // ===================================================================
    // 4. 서버 로직 (C++의 gameWorld_ 로직)
    // ===================================================================
    protected virtual void FixedUpdate()
    {
        if (IsServer)
        {
            // 1. 저장된 입력(serverInputMovement)을 기반으로 위치 계산
            Vector3 newVelocity = serverInputMovement * moveSpeed;
            newVelocity.y = rb.linearVelocity.y;

            // 3. ⭐(핵심 수정)⭐ 서버 자신의 transform.position도 갱신 (다음 FixedUpdate를 위해)
            rb.linearVelocity = newVelocity;

            // 점프
            if (serverJumpQueued)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                serverJumpQueued = false;
            }

            // 2. '권위있는' NetworkVariable 갱신
            // (이 값이 클라이언트의 'ApplyServerState'로 전송됩니다)
            serverPosition.Value = rb.position;

            // 4. 회전 값 갱신
            if (serverInputMovement.magnitude > 0.1f)
            {
                Quaternion newRot = Quaternion.LookRotation(serverInputMovement);

                // 6. ⭐(핵심 수정)⭐ 서버 자신의 transform.rotation도 갱신
                rb.MoveRotation(newRot);

                // 5. NetworkVariable 갱신 (클라이언트 전송)
                serverRotation.Value = newRot;
            }
        }

        // 클라이언트는 서버상태를 적용하기만
        else
        {
            ApplyServerState();
        }
    }

    // 닉네임이 바뀔 때 UI 업데이트
    private void OnNicknameChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        UpdateNicknameUI(newValue.ToString());
    }

    protected void UpdateNicknameUI(string nickname)
    {
        if (nicknameText != null)
        {
            nicknameText.text = nickname;
        }
    }

    // [추가] 색상이 변경되면 이 함수가 호출됨
    private void OnColorChanged(Color previousValue, Color newValue)
    {
        ApplyColor(newValue);
    }

    protected void ApplyColor(Color color)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = color;
        }
    }

    /// <summary>
    /// (IsOwner) 입력을 처리하고, 서버 RPC를 호출하고, 로컬 예측을 위해 입력을 저장합니다.
    /// </summary>
    private void HandleInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 movement = Vector3.zero;

        if (horizontal != 0 || vertical != 0)
        {
            Vector3 camMovement = GetCameraRelativeMovement(horizontal, vertical);
            movement = camMovement.normalized; // 정규화
        }

        // 입력 값을 '서버'로 전송합니다.
        SubmitInputServerRpc(movement);

        // 점프 입력 처리
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump"))
        {
            SubmitJumpServerRpc();
        }
    }

    // 서버 정보로 위치와 회전을 보간
    private void ApplyServerState()
    {
        Vector3 serverPos = serverPosition.Value;
        Quaternion serverRot = serverRotation.Value;

        // 모든 클라이언트 (로컬 플레이어 포함)는 서버 위치로 Lerp합니다.
        // isKinematic = true 이므로 Lerp가 물리 엔진과 충돌하지 않습니다.
        rb.MovePosition(Vector3.Lerp(rb.position, serverPos, Time.fixedDeltaTime * 10f));
        rb.MoveRotation(Quaternion.Lerp(transform.rotation, serverRot, Time.fixedDeltaTime * 10f));
    }

    // ===================================================================
    // 5. 서버 RPC (C++의 handleMessage)
    // ===================================================================

    // [ServerRpc]
    // 클라이언트가 호출 -> '서버'에서만 실행됨

    [ServerRpc]
    private void SubmitInitialDataServerRpc(string nickname, Color color)
    {
        playerNickname.Value = nickname;
        playerColor.Value = color;

        Debug.Log($"[Server Log] Player (ID: {OwnerClientId}) JOINED with nickname: {nickname}");
        Debug.Log($"[Server Log] Player (ID: {OwnerClientId}) JOINED with color: {color}");
        Debug.Log($"[Server Log] Total players: {NetworkManager.Singleton.ConnectedClients.Count}");
    }

    [ServerRpc]
    private void SubmitInputServerRpc(Vector3 movement)
    {
        // 받은 입력을 FixedUpdate가 사용할 수 있도록 저장
        serverInputMovement = movement;
    }

    [ServerRpc]
    private void SubmitJumpServerRpc()
    {
        Debug.Log($"[Server Log] Player {OwnerClientId} JUMP!");
        serverJumpQueued = true;
    }

    // =CI. 유틸리티 (기존 코드 동일)
    Vector3 GetCameraRelativeMovement(float horizontal, float vertical)
    {
        if (cameraFollow == null)
        {
            if (Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();
            if (cameraFollow == null) return new Vector3(horizontal, 0, vertical);
        }

        float cameraRotation = cameraFollow.CurrentRotationY;
        float angleRad = cameraRotation * Mathf.Deg2Rad;
        float rotatedX = horizontal * Mathf.Cos(angleRad) - vertical * Mathf.Sin(angleRad);
        float rotatedZ = horizontal * Mathf.Sin(angleRad) + vertical * Mathf.Cos(angleRad);
        return new Vector3(rotatedX, 0, rotatedZ);
    }
}
