using Unity.Netcode;
using UnityEngine;

public class NetworkDummy : NetworkBehaviour
{
    private NetworkVariable<Vector3> serverPosition = new NetworkVariable<Vector3>(
            Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void FixedUpdate()
    {
        // 서버가 아니면 (클라이언트면) 서버 위치로 부드럽게 보간
        if (!IsServer)
        {
            transform.position = Vector3.Lerp(transform.position, serverPosition.Value, Time.deltaTime * 10f);
        }
    }

    // 서버 전용: 위치 설정 (FixedUpdate 등에서 호출)
    public void SetPosition(Vector3 newPosition)
    {
        if (IsServer)
        {
            transform.position = newPosition; // 서버는 즉시 이동
            serverPosition.Value = newPosition; // 클라이언트로 전파
        }
    }
}
