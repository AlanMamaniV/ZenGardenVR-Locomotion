using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

namespace VRExplorer
{
    /// <summary>
    /// Interacción 1: Cristales Mágicos.
    /// Responde a selección/activación por rayo o proximidad cambiando de color,
    /// emitiendo partículas, reproduciendo sonido y enviando mensaje a la UI.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    [RequireComponent(typeof(AudioSource))]
    public class CrystalInteraction : MonoBehaviour
    {
        [Header("Configuración Visual")]
        [SerializeField] private MeshRenderer crystalRenderer;
        [ColorUsage(true, true)]
        [SerializeField] private Color activeEmissionColor = new Color(0.2f, 1f, 0.8f, 1f) * 2f;
        [SerializeField] private ParticleSystem magicParticles;
        [SerializeField] private Light crystalLight;

        [Header("Audio")]
        [SerializeField] private AudioClip interactionSound;

        [Header("Mensaje UI")]
        [SerializeField] private string crystalName = "Cristal Arcano";
        [SerializeField] private string discoveryMessage = "¡Cristal Mágico activado! Su energía resuena en la isla.";

        private XRSimpleInteractable interactable;
        private AudioSource audioSource;
        private Material crystalMaterial;
        private Color initialEmissionColor;
        private bool isActivated = false;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            audioSource = GetComponent<AudioSource>();

            if (crystalRenderer == null)
                crystalRenderer = GetComponent<MeshRenderer>();

            if (crystalRenderer != null)
            {
                crystalMaterial = crystalRenderer.material; // Clon de instancia para modificar emisión
                if (crystalMaterial.HasProperty("_EmissionColor"))
                {
                    initialEmissionColor = crystalMaterial.GetColor("_EmissionColor");
                }
            }

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1.0f; // 3D sound
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

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            ActivateCrystal();
        }

        private void OnActivated(ActivateEventArgs args)
        {
            ActivateCrystal();
        }

        public void ActivateCrystal()
        {
            if (isActivated) return;
            isActivated = true;

            // 1. Cambiar emisión/color
            if (crystalMaterial != null)
            {
                crystalMaterial.EnableKeyword("_EMISSION");
                crystalMaterial.SetColor("_EmissionColor", activeEmissionColor);
            }

            // 2. Encender luz
            if (crystalLight != null)
            {
                crystalLight.enabled = true;
                crystalLight.color = activeEmissionColor;
            }

            // 3. Partículas
            if (magicParticles != null)
            {
                magicParticles.Play();
            }

            // 4. Sonido
            if (audioSource != null && (interactionSound != null || audioSource.clip != null))
            {
                if (interactionSound != null)
                    audioSource.PlayOneShot(interactionSound);
                else
                    audioSource.Play();
            }

            // 5. Notificar a la UI
            if (VRUIController.Instance != null)
            {
                VRUIController.Instance.ShowNotification($"{crystalName}: {discoveryMessage}");
            }

            // Animación de pulso
            StartCoroutine(PulseAnimation());
        }

        private IEnumerator PulseAnimation()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * 1.25f;

            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = originalScale;
        }
    }
}
