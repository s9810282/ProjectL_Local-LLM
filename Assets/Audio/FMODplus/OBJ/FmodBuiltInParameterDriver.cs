using UnityEngine;
using FMOD;
using FMOD.Studio;

/// <summary>
/// Built-In / Automatic parameter가 필요한 FMOD EventInstance에
/// 3D attributes를 지속적으로 공급하는 런타임 드라이버.
/// 
/// 핵심 역할:
/// - FmodEventPlayer가 생성한 현재 인스턴스를 가져온다.
/// - Transform 위치/속도를 FMOD ATTRIBUTES_3D로 변환한다.
/// - Distance / Speed 같은 automatic 파라미터가 계산될 수 있도록 한다.
/// </summary>
public class FmodBuiltInParameterDriver : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private FmodEventPlayer player;
    [SerializeField] private int slotIndex = 0;

    [Header("Drive Options")]
    [SerializeField] private bool drive3DAttributes = true;
    [SerializeField] private bool updateEveryFrame = true;

    [Header("2D Mapping")]
    [Tooltip("2D 게임 좌표를 FMOD 3D 좌표로 매핑할 때 사용할 Z 값")]
    [SerializeField] private float worldZ = 0f;

    [Tooltip("true면 XY 평면을 그대로 FMOD XY에 넣고 Z는 고정값 사용")]
    [SerializeField] private bool useXYPlane = true;

    [Header("Velocity")]
    [SerializeField] private bool estimateVelocity = true;
    [SerializeField, Min(0.001f)] private float velocitySampleInterval = 0.05f;

    [Header("Orientation")]
    [SerializeField] private Vector3 forwardVector = Vector3.forward;
    [SerializeField] private Vector3 upVector = Vector3.up;

    private Vector3 lastPosition;
    private Vector3 estimatedVelocity;

    private float sampleTimer;

    private void Awake()
    {
        lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (!updateEveryFrame)
            return;
        TickDriver(Time.deltaTime);
    }

    public void ManualTick(float deltaTime)
    {
        TickDriver(deltaTime);
    }

    private void TickDriver(float deltaTime)
    {
        if (player == null || !drive3DAttributes)
            return;

        UpdateVelocity(deltaTime);

        if (!player.TryGetCurrentInstance(slotIndex, out EventInstance instance))
            return;

        if (!instance.isValid())
            return;

        ATTRIBUTES_3D attributes = Build3DAttributes(transform.position, estimatedVelocity);
        instance.set3DAttributes(attributes);
    }

    private void UpdateVelocity(float deltaTime)
    {
        if (!estimateVelocity)
        {
            estimatedVelocity = Vector3.zero;
            lastPosition = transform.position;
            sampleTimer = 0f;
            return;
        }

        sampleTimer += deltaTime;
        if (sampleTimer < Mathf.Max(0.001f, velocitySampleInterval))
            return;

        Vector3 current = transform.position;
        estimatedVelocity = (current - lastPosition) / sampleTimer;
        lastPosition = current;
        sampleTimer = 0f;
    }

    private ATTRIBUTES_3D Build3DAttributes(Vector3 position, Vector3 velocity)
    {
        Vector3 mappedPosition;
        Vector3 mappedVelocity;

        if (useXYPlane)
        {
            mappedPosition = new Vector3(position.x, position.y, worldZ);
            mappedVelocity = new Vector3(velocity.x, velocity.y, 0f);
        }
        else
        {
            mappedPosition = position;
            mappedVelocity = velocity;
        }

        Vector3 normalizedForward = forwardVector.sqrMagnitude > 0.0001f ? forwardVector.normalized : Vector3.forward;
        Vector3 normalizedUp = upVector.sqrMagnitude > 0.0001f ? upVector.normalized : Vector3.up;

        ATTRIBUTES_3D attributes = new ATTRIBUTES_3D
        {
            position = ToFmodVector(mappedPosition),
            velocity = ToFmodVector(mappedVelocity),
            forward = ToFmodVector(normalizedForward),
            up = ToFmodVector(normalizedUp)
        };

        return attributes;
    }

    private VECTOR ToFmodVector(Vector3 v)
    {
        return new VECTOR
        {
            x = v.x,
            y = v.y,
            z = v.z
        };
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.15f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + forwardVector.normalized * 0.5f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + upVector.normalized * 0.4f);
    }
#endif
}