using UnityEngine;
using Unity.Cinemachine;

public class PanicCameraShake : MonoBehaviour
{
    [Header("Shake")]
    [SerializeField] private float maximumAmplitude = 0.38f;
    [SerializeField] private float shakeFrequency = 25f;

    private Vector3 originalLocalPosition;
    private CinemachineBasicMultiChannelPerlin cinemachineNoise;
    private float intensity;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;

        CinemachineCamera virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        if (virtualCamera == null)
            return;

        cinemachineNoise = virtualCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        if (cinemachineNoise == null)
            cinemachineNoise = virtualCamera.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();

        if (cinemachineNoise.NoiseProfile == null)
            cinemachineNoise.NoiseProfile = CreatePanicNoiseProfile();
    }

    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);

        if (cinemachineNoise != null)
        {
            cinemachineNoise.AmplitudeGain = maximumAmplitude * intensity;
            cinemachineNoise.FrequencyGain = shakeFrequency / 15f;
            cinemachineNoise.enabled = intensity > 0.001f;
        }
    }

    private void LateUpdate()
    {
        if (cinemachineNoise != null)
            return;

        if (intensity <= 0f)
        {
            transform.localPosition = originalLocalPosition;
            return;
        }

        float time = Time.time * shakeFrequency;
        Vector3 noise = new Vector3(
            Mathf.PerlinNoise(time, 0f) - 0.5f,
            Mathf.PerlinNoise(0f, time) - 0.5f,
            0f
        );

        transform.localPosition = originalLocalPosition + noise * (maximumAmplitude * intensity);
    }

    private void OnDisable()
    {
        if (cinemachineNoise != null)
        {
            cinemachineNoise.AmplitudeGain = 0f;
            cinemachineNoise.enabled = false;
        }

        transform.localPosition = originalLocalPosition;
    }

    private static NoiseSettings CreatePanicNoiseProfile()
    {
        NoiseSettings profile = ScriptableObject.CreateInstance<NoiseSettings>();
        profile.PositionNoise = new[]
        {
            new NoiseSettings.TransformNoiseParams
            {
                X = new NoiseSettings.NoiseParams { Frequency = 1.2f, Amplitude = 0.6f },
                Y = new NoiseSettings.NoiseParams { Frequency = 1.6f, Amplitude = 0.4f },
                Z = new NoiseSettings.NoiseParams { Frequency = 1.4f, Amplitude = 0.2f }
            }
        };
        profile.OrientationNoise = new[]
        {
            new NoiseSettings.TransformNoiseParams
            {
                X = new NoiseSettings.NoiseParams { Frequency = 1.4f, Amplitude = 4f },
                Y = new NoiseSettings.NoiseParams { Frequency = 1.8f, Amplitude = 3f },
                Z = new NoiseSettings.NoiseParams { Frequency = 2.1f, Amplitude = 5f }
            }
        };
        return profile;
    }
}
