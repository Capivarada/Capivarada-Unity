using UnityEngine;
using System.Collections;

namespace ArenadasAlmas.Enemy
{
    public class EnemyVisualController : MonoBehaviour
    {
        [SerializeField] private EnemyDummy enemyDummy;
        [SerializeField] private Material originalMaterial;
        [SerializeField] private Material extractionMaterial;
        [SerializeField] private float blinkInterval = 0.1f;

        private MeshRenderer meshRenderer;
        private Coroutine blinkCoroutine;

        private void OnEnable()
        {
            if (enemyDummy == null)
                enemyDummy = GetComponent<EnemyDummy>();

            meshRenderer = GetComponent<MeshRenderer>();

            if (originalMaterial == null && meshRenderer != null)
                originalMaterial = meshRenderer.material;

            if (enemyDummy != null)
            {
                enemyDummy.OnExtractionStarted += OnExtractionStarted;
                enemyDummy.OnExtractionEnded += OnExtractionEnded;
                enemyDummy.OnEnemyDeath += OnEnemyDeath;
            }
        }

        private void OnDisable()
        {
            if (enemyDummy != null)
            {
                enemyDummy.OnExtractionStarted -= OnExtractionStarted;
                enemyDummy.OnExtractionEnded -= OnExtractionEnded;
                enemyDummy.OnEnemyDeath -= OnEnemyDeath;
            }
        }

        private void OnExtractionStarted()
        {
            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);

            blinkCoroutine = StartCoroutine(BlinkRoutine());
        }

        private void OnExtractionEnded()
        {
            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);

            ResetMaterial();
        }

        private void OnEnemyDeath()
        {
            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);
        }

        private IEnumerator BlinkRoutine()
        {
            while (enemyDummy.IsBeingExtracted)
            {
                SetMaterialToBlack();
                yield return new WaitForSeconds(blinkInterval);

                if (!enemyDummy.IsBeingExtracted)
                    break;

                ResetMaterial();
                yield return new WaitForSeconds(blinkInterval);
            }

            ResetMaterial();
        }

        private void SetMaterialToBlack()
        {
            if (meshRenderer != null)
            {
                Material blackMaterial = new Material(Shader.Find("Standard"));
                blackMaterial.color = Color.black;
                meshRenderer.material = blackMaterial;
            }
        }

        private void ResetMaterial()
        {
            if (meshRenderer != null && originalMaterial != null)
                meshRenderer.material = originalMaterial;
        }
    }
}
