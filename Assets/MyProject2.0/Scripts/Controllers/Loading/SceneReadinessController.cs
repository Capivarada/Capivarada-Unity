using System;
using UnityEngine;

/// <summary>Optional scene-level readiness signal used by LoadingController after activation.</summary>
public sealed class SceneReadinessController : MonoBehaviour
{
    [SerializeField] private bool readyOnStart = true;
    public bool IsReady { get; private set; }
    public event Action Ready;

    private void Start()
    {
        if (readyOnStart) MarkReady();
    }

    public void MarkReady()
    {
        if (IsReady) return;
        IsReady = true;
        Ready?.Invoke();
    }
}
