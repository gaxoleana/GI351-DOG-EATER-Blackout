using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Serializable]
    private class EnemySpawnDefinition
    {
        [SerializeField] private GameObject enemyPrefab;
        [Tooltip("Relative spawn chance. 70/25/5 means roughly 70%, 25%, and 5%.")]
        [SerializeField, Min(0f)] private float spawnWeight = 1f;

        public GameObject EnemyPrefab => enemyPrefab;
        public float SpawnWeight => spawnWeight;
    }

    [Serializable]
    private class WaveDefinition
    {
        [SerializeField] private EnemySpawnDefinition[] enemyTypes;
        [SerializeField, Min(1)] private int enemyCount = 3;
        [SerializeField, Min(0f)] private float spawnInterval = 0.5f;
        [SerializeField] private Transform[] spawnPoints;

        public EnemySpawnDefinition[] EnemyTypes => enemyTypes;
        public int EnemyCount => enemyCount;
        public float SpawnInterval => spawnInterval;
        public Transform[] SpawnPoints => spawnPoints;
    }

    [Header("Waves")]
    [SerializeField] private WaveDefinition[] waves;
    [SerializeField, Min(0f)] private float initialDelay = 1f;
    [SerializeField, Min(0f)] private float delayBetweenWaves = 2f;
    [SerializeField] private bool startOnAwake = true;

    [Header("UI")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text remainingEnemyText;
    [SerializeField] private string waveFormat = "WAVE {0}/{1}";
    [SerializeField] private string remainingEnemyFormat = "REMAINING ENEMY: {0}";

    [Header("Gate")]
    [Tooltip("Disabled until every configured wave is cleared.")]
    [SerializeField] private GameObject gate;

    private readonly List<GameObject> aliveEnemies = new();
    private int currentWaveIndex = -1;
    private bool isRunning;
    private bool isComplete;
    private int displayedWaveNumber = int.MinValue;
    private int displayedWaveCount = int.MinValue;
    private int displayedAliveEnemyCount = int.MinValue;

    public event Action<int, int> WaveStarted;
    public event Action<int, int> WaveCleared;
    public event Action AllWavesCleared;

    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int WaveCount => waves?.Length ?? 0;
    public int AliveEnemyCount => aliveEnemies.Count;
    public bool IsComplete => isComplete;

    private void Awake()
    {
        if (gate != null)
            gate.SetActive(false);
    }

    private void Start()
    {
        UpdateUI(true);

        if (startOnAwake)
            BeginWaves();
    }

    private void Update()
    {
        for (int index = aliveEnemies.Count - 1; index >= 0; index--)
        {
            if (aliveEnemies[index] == null)
                aliveEnemies.RemoveAt(index);
        }

        UpdateUI();
    }

    public void BeginWaves()
    {
        if (isRunning || isComplete)
            return;

        if (waves == null || waves.Length == 0)
        {
            Debug.LogWarning("WaveManager has no configured waves.", this);
            return;
        }

        UpdateUI(true);
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        isRunning = true;

        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        for (currentWaveIndex = 0; currentWaveIndex < waves.Length; currentWaveIndex++)
        {
            WaveDefinition wave = waves[currentWaveIndex];
            if (wave == null || !HasSpawnableEnemy(wave))
            {
                Debug.LogWarning($"Wave {CurrentWaveNumber} has no valid enemy types and was skipped.", this);
                continue;
            }

            WaveStarted?.Invoke(CurrentWaveNumber, WaveCount);
            UpdateUI(true);
            yield return SpawnWave(wave);
            yield return new WaitUntil(() => aliveEnemies.Count == 0);
            WaveCleared?.Invoke(CurrentWaveNumber, WaveCount);
            UpdateUI(true);

            if (currentWaveIndex < waves.Length - 1 && delayBetweenWaves > 0f)
                yield return new WaitForSeconds(delayBetweenWaves);
        }

        isRunning = false;
        isComplete = true;

        if (gate != null)
            gate.SetActive(true);

        AllWavesCleared?.Invoke();
    }

    private IEnumerator SpawnWave(WaveDefinition wave)
    {
        for (int enemyIndex = 0; enemyIndex < wave.EnemyCount; enemyIndex++)
        {
            GameObject enemyPrefab = SelectEnemyPrefab(wave.EnemyTypes);
            if (enemyPrefab == null)
                continue;

            Transform spawnPoint = GetSpawnPoint(wave, enemyIndex);
            GameObject enemy = Instantiate(
                enemyPrefab,
                spawnPoint != null ? spawnPoint.position : transform.position,
                spawnPoint != null ? spawnPoint.rotation : transform.rotation
            );
            aliveEnemies.Add(enemy);
            UpdateUI(true);

            if (wave.SpawnInterval > 0f && enemyIndex < wave.EnemyCount - 1)
                yield return new WaitForSeconds(wave.SpawnInterval);
        }
    }

    private void UpdateUI(bool force = false)
    {
        int waveNumber = CurrentWaveNumber;
        int waveCount = WaveCount;
        int aliveEnemyCount = AliveEnemyCount;

        if (!force && waveNumber == displayedWaveNumber &&
            waveCount == displayedWaveCount && aliveEnemyCount == displayedAliveEnemyCount)
            return;

        displayedWaveNumber = waveNumber;
        displayedWaveCount = waveCount;
        displayedAliveEnemyCount = aliveEnemyCount;

        if (waveText != null)
            waveText.text = string.Format(waveFormat, waveNumber, waveCount);

        if (remainingEnemyText != null)
            remainingEnemyText.text = string.Format(remainingEnemyFormat, aliveEnemyCount);
    }

    private static bool HasSpawnableEnemy(WaveDefinition wave)
    {
        if (wave.EnemyTypes == null)
            return false;

        foreach (EnemySpawnDefinition enemyType in wave.EnemyTypes)
        {
            if (enemyType?.EnemyPrefab != null && enemyType.SpawnWeight > 0f)
                return true;
        }

        return false;
    }

    private static GameObject SelectEnemyPrefab(EnemySpawnDefinition[] enemyTypes)
    {
        float totalWeight = 0f;

        foreach (EnemySpawnDefinition enemyType in enemyTypes)
        {
            if (enemyType?.EnemyPrefab != null && enemyType.SpawnWeight > 0f)
                totalWeight += enemyType.SpawnWeight;
        }

        if (totalWeight <= 0f)
            return null;

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        foreach (EnemySpawnDefinition enemyType in enemyTypes)
        {
            if (enemyType?.EnemyPrefab == null || enemyType.SpawnWeight <= 0f)
                continue;

            randomValue -= enemyType.SpawnWeight;
            if (randomValue <= 0f)
                return enemyType.EnemyPrefab;
        }

        return null;
    }

    private static Transform GetSpawnPoint(WaveDefinition wave, int enemyIndex)
    {
        Transform[] spawnPoints = wave.SpawnPoints;
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        return spawnPoints[enemyIndex % spawnPoints.Length];
    }
}