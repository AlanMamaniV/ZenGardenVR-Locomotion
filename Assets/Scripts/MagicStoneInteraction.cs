using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRExplorer
{
    /// <summary>
    /// Interacción 2: Piedra Mágica (Runestone).
    /// Al seleccionarla con el rayo o interactuador, se ilumina intensamente,
    /// altera su apariencia y reproduce una animación de elevación y rotación.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    [RequireComponent(typeof(AudioSource))]
    public class MagicStoneInteraction : MonoBehaviour
    {
        [Header("Configuración Visual")]
        [SerializeField] private MeshRenderer stoneRenderer;
        [ColorUsage(true, true)]
        [SerializeField] private Color runeGlowColor = new Color(0.9f, 0.4f, 1f, 1f) * 2.5f;
        [SerializeField] private Light stoneLight;
        [SerializeField] private ParticleSystem magicDust;

        [Header("Audio")]
        [SerializeField] private AudioClip humSound;

        [Header("UI Feedback")]
        [SerializeField] private string stoneName = "Piedra Rúnica";
        [SerializeField] private string feedbackMessage = "Las runas ancestrales despiertan con luz púrpura.";

        private XRSimpleInteractable interactable;
        private AudioSource audioSource;
        private Material stoneMaterial;
        private bool isActivated = false;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            audioSource = GetComponent<AudioSource>();

            if (stoneRenderer == null)
                stoneRenderer = GetComponent<MeshRenderer>();

            if (stoneRenderer != null)
            {
                stoneMaterial = stoneRenderer.material;
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }
        }

        private void OnEnable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.AddListener(OnSelectEntered);
                interactable.activated.AddListener(OnActivated);
            }
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnSelectEntered);
                interactable.activated.RemoveListener(OnActivated);
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args) => ActivateStone();
        private void OnActivated(ActivateEventArgs args) => ActivateStone();

        public void ActivateStone()
        {
            if (isActivated) return;
            isActivated = true;

            // Iluminar material
            if (stoneMaterial != null)
            {
                stoneMaterial.EnableKeyword("_EMISSION");
                stoneMaterial.SetColor("_EmissionColor", runeGlowColor);
            }

            // Luz
            if (stoneLight != null)
            {
                stoneLight.enabled = true;
                stoneLight.color = runeGlowColor;
            }

            // Polvo mágico
            if (magicDust != null)
            {
                magicDust.Play();
            }

            // Audio
            if (audioSource != null && (humSound != null || audioSource.clip != null))
            {
                if (humSound != null) audioSource.PlayOneShot(humSound);
                else audioSource.Play();
            }

            // Notificación UI
            if (VRUIController.Instance != null)
            {
                VRUIController.Instance.ShowNotification($"{stoneName}: {feedbackMessage}");
            }

            // Animación de levitación y giro suave
            StartCoroutine(LevitateAndRotate());
        }

        private IEnumerator LevitateAndRotate()
        {
            Vector3 startPos = transform.localPosition;
            Vector3 targetPos = startPos + Vector3.up * 0.4f;

            float elapsed = 0f;
            float duration = 1.0f;

            while (elapsed < duration)
            {
                transform.localPosition = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0, 1, elapsed / duration));
                transform.Rotate(Vector3.up, 60f * Time.deltaTime, Space.World);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Flotación continua
            while (true)
            {
                float floatOffset = Mathf.Sin(Time.time * 2f) * 0.05f;
                transform.localPosition = targetPos + new Vector3(0, floatOffset, 0);
                transform.Rotate(Vector3.up, 25f * Time.deltaTime, Space.World);
                yield return null;
            }
        }
    }
}
