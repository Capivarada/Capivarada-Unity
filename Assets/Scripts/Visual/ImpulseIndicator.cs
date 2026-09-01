using System.Collections.Generic;
using UnityEngine;

public class ImpulseIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject cubePrefab;

    [Header("Indicator")]
    [SerializeField] private float cubeSpacing = 0.5f;

    private readonly List<GameObject> cubes = new();

    private int currentCharge = 0;

    public int CurrentCharge => currentCharge;

    private void Awake()
    {
        UpdateIndicator();
    }

    public void SetCharge(int charge)
    {
        currentCharge = Mathf.Clamp(charge, 0, 10);

        UpdateIndicator();
    }

    private void UpdateIndicator()
    {
        ClearCubes();

        for (int i = 0; i < currentCharge; i++)
        {
            GameObject cube = Instantiate(
                cubePrefab,
                transform
            );

            cube.transform.localPosition =
                Vector3.forward * cubeSpacing * (i + 1);

            cube.transform.localRotation =
                Quaternion.identity;

            cubes.Add(cube);
        }
    }

    private void ClearCubes()
    {
        foreach (GameObject cube in cubes)
        {
            if (cube != null)
                Destroy(cube);
        }

        cubes.Clear();
    }
}