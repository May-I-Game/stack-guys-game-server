using Unity.Netcode;
using UnityEngine;

// PlayerController를 상속받아 더미 전용 로직을 추가
public class DummyPlayer : NetworkPlayer
{
    private Vector3 currentDirection;
    private float nextDirectionChangeTime;
    private float nextJumpTime;

    public void InitializeDummy(string dummyName, Color dummyColor)
    {
        playerNickname.Value = dummyName;
        playerColor.Value = dummyColor;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            ChangeDirection();
            ResetJumpTimer();
        }
    }

    // 부모의 Update()가 IsOwner일 때 이 함수를 호출
    protected override void HandleInput()
    {
        SimulateMovement();
        SimulateJump();
    }

    // 더미의 이동 입력을 시뮬레이션
    public void SimulateMovement()
    {
        if (Time.time >= nextDirectionChangeTime)
        {
            ChangeDirection();
        }
        SubmitInputServerRpc(currentDirection);
    }

    // 더미의 점프를 시뮬레이션
    public void SimulateJump()
    {
        if (Time.time >= nextJumpTime)
        {
            SubmitJumpServerRpc();
            ResetJumpTimer();
        }
    }

    private void ChangeDirection()
    {
        float randomAngle = Random.Range(0f, 360f);
        currentDirection = Quaternion.Euler(0, randomAngle, 0) * Vector3.forward;
        nextDirectionChangeTime = Time.time + Random.Range(3f, 5f);
    }

    private void ResetJumpTimer()
    {
        nextJumpTime = Time.time + Random.Range(3f, 5f);
    }
}
