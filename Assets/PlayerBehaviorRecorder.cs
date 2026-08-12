using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBehaviorRecorder : MonoBehaviour
{
    [Serializable]
    public class BehaviorEvent
    {
        public float time;
        public string action;
        public Vector3 position;
    }

    [SerializeField] private Camera mainCamera;

    private List<BehaviorEvent> events = new List<BehaviorEvent>();

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        // LPM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = mainCamera.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                RecordEvent("MOVE", hit.point);
            }
        }
    }

    private void RecordEvent(string action, Vector3 position)
    {
        BehaviorEvent behaviorEvent = new BehaviorEvent
        {
            time = Time.time,
            action = action,
            position = position
        };

        events.Add(behaviorEvent);

        Debug.Log(
            $"[PLAYER] {action} -> {position} @ {Time.time:F2}s"
        );
    }

    private void OnApplicationQuit()
    {
        SaveData();
    }

    public void SaveData()
    {
        string path = Path.Combine(
            Application.persistentDataPath,
            "player_behavior.json"
        );

        string json = JsonUtility.ToJson(
            new BehaviorData { events = events },
            true
        );

        File.WriteAllText(path, json);

        Debug.Log($"[PLAYER] Behavior saved to: {path}");
    }

    [Serializable]
    private class BehaviorData
    {
        public List<BehaviorEvent> events;
    }
}