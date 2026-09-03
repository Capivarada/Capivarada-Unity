using System;
using UnityEngine;

public class LeaveGame : MonoBehaviour
{
    private void Awake()
    {
        #if UNITY_EDITOR || UNITY_WEBGL
            this.gameObject.SetActive(false);
        #endif
    }

    public void Leave()
    {
        Application.Quit();
    }

}