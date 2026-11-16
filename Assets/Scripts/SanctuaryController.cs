using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRExplorer
{
    /// <summary>
    /// Interacción 4: Santuario Mágico.
    /// Al activar el gran cristal central del santuario, se iluminan los pilares y arcos,
    /// se detonan ráfagas de partículas místicas, se reproduce sonido triunfal,
    /// y se proclama el gran mensaje final de descubrimiento.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    [RequireComponent(typeof(AudioSource))]
    public class SanctuaryController : MonoBehaviour
    {
        [Header("Elementos del Santuario")]
        [SerializeField] private MeshRenderer mainCrystalRenderer;
        [SerializeField] private Light sanctuarySpotLight;
        [SerializeField] private List<Light> pillarLights = new List<Light>();
        [SerializeField] private List<MeshRenderer> runePillars = new List<MeshRenderer>();
        [SerializeField] private ParticleSystem sanctuaryAuraParticles;
        [SerializeField] private ParticleSystem sanctuaryBurstParticles;

        [Header("Colores Mágicos")]
        [ColorUsage(true, true)]
        [SerializeField] private Color activeEmissionColor = new Color(0.3f, 0.7f, 1f, 1f) * 3f;

        [Header("Audio")]
        [SerializeField] private AudioClip sanctuaryActivationSound;

        [Header("Piedras Flotantes")]
        [SerializeField] private List<Transform> floatingStones = new List<Transform>();

        private XRSimpleInteractable interactable;
        private AudioSource audioSource;
        private bool isSanctuaryActive = false;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            audioSource = GetComponent<AudioSource>();

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
                interactable.selectEntered.AddListener(OnCrystalSelected);
                interactable.activated.AddListener(OnCrystalActivated);
            }
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnCrystalSelected);
                interactable.activated.RemoveListener(OnCrystalActivated);
            }
        }

        private void OnCrystalSelected(SelectEnterEventArgs args) => TriggerSanctuary();
        private void OnCrystalActivated(ActivateEventArgs args) => TriggerSanctuary();

        public void TriggerSanctuary()
        {
            if (isSanctuaryActive) return;
            isSanctuaryActive = true;

            // 1. Activar material del cristal
            if (mainCrystalRenderer != null)
            {
                Material mat = mainCrystalRenderer.material;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", activeEmissionColor);
            }

            // 2. Encender luces
            if (sanctuarySpotLight != null)
            {
                sanctuarySpotLight.enabled = true;
                sanctuarySpotLight.color = activeEmissionColor;
            }

            foreach (var pLight in pillarLights)
            {
                if (pLight != null)
                {
                    pLight.enabled = true;
                    pLight.color = activeEmissionColor;
                }
            }

            // 3. Activar pilares
            foreach (var pRend in runePillars)
            {
                if (pRend != null)
                {
                    Material pMat = pRend.material;
                    pMat.EnableKeyword("_EMISSION");
                    pMat.SetColor("_EmissionColor", activeEmissionColor * 0.7f);
                }
            }

            // 4. Partículas
            if (sanctuaryAuraParticles != null) sanctuaryAuraParticles.Play();
            if (sanctuaryBurstParticles != null) sanctuaryBurstParticles.Play();

            // 5. Audio
            if (audioSource != null && (sanctuaryActivationSound != null || audioSource.clip != null))
            {
                if (sanctuaryActivationSound != null)
                    audioSource.PlayOneShot(sanctuaryActivationSound);
                else
                    audioSource.Play();
            }

            // 6. Mensaje en UI
            if (VRUIController.Instance != null)
            {
                VRUIController.Instance.ShowSanctuaryDiscovered();
            }

            StartCoroutine(AnimateSanctuaryAwakening());
        }

        private IEnumerator AnimateSanctuaryAwakening()
        {
            float elapsed = 0f;
            float duration = 3f;

            while (elapsed < duration)
            {
                float factor = elapsed / duration;
                if (sanctuarySpotLight != null)
                {
                    sanctuarySpotLight.intensity = Mathf.Lerp(0f, 8f, factor);
                }

                // Rotar suavemente el cristal
                if (mainCrystalRenderer != null)
                {
                    mainCrystalRenderer.transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Bucle perpetuo de rotación y levitación de piedras flotantes
            while (true)
            {
                if (mainCrystalRenderer != null)
                {
                    mainCrystalRenderer.transform.Rotate(Vector3.up, 20f * Time.deltaTime, Space.World);
                }

                for (int i = 0; i < floatingStones.Count; i++)
                {
                    if (floatingStones[i] != null)
                    {
                        float offset = Mathf.Sin(Time.time * 1.5f + i) * 0.002f;
                        floatingStones[i].position += new Vector3(0, offset, 0);
                        floatingStones[i].Rotate(Vector3.up, (15f + i * 5f) * Time.deltaTime, Space.Self);
                    }
                }

                yield return null;
            }
        }
    }
}
