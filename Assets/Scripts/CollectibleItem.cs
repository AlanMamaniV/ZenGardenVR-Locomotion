using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRExplorer
{
    /// <summary>
    /// Interacción 3: Objeto Coleccionable.
    /// Utiliza XRGrabInteractable para permitir al usuario tomarlo con el controlador VR.
    /// Al levantarlo, desencadena animación de brillo, reproduce sonido, actualiza el contador
    /// y notifica al usuario.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Rigidbody))]
    public class CollectibleItem : MonoBehaviour
    {
        [Header("Datos del Objeto")]
        [SerializeField] private string itemName = "Reliquia de la Isla";

        [Header("Efectos")]
        [SerializeField] private ParticleSystem collectParticles;
        [SerializeField] private AudioClip grabSound;
        [SerializeField] private MeshRenderer itemRenderer;
        [ColorUsage(true, true)]
        [SerializeField] private Color glowColor = new Color(1f, 0.84f, 0f, 1f) * 2f;

        private XRGrabInteractable grabInteractable;
        private AudioSource audioSource;
        private Rigidbody rb;
        private bool isRegistered = false;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            audioSource = GetComponent<AudioSource>();
            rb = GetComponent<Rigidbody>();

            if (itemRenderer == null)
                itemRenderer = GetComponent<MeshRenderer>();

            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f;
            }
        }

        private void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnGrabbed);
            }
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            }
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            if (isRegistered) return;
            isRegistered = true;

            // Audio
            if (audioSource != null && (grabSound != null || audioSource.clip != null))
            {
                if (grabSound != null) audioSource.PlayOneShot(grabSound);
                else audioSource.Play();
            }

            // Partículas
            if (collectParticles != null)
            {
                collectParticles.Play();
            }

            // Destello en material
            if (itemRenderer != null)
            {
                Material mat = itemRenderer.material;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", glowColor);
            }

            // Registrar en CollectibleManager
            if (CollectibleManager.Instance != null)
            {
                CollectibleManager.Instance.RegisterCollectibleFound(itemName);
            }
            else if (VRUIController.Instance != null)
            {
                VRUIController.Instance.ShowNotification($"¡Recogiste: {itemName}!");
            }

            StartCoroutine(CelebrationPulse());
        }

        private IEnumerator CelebrationPulse()
        {
            Vector3 baseScale = transform.localScale;
            float t = 0f;
            while (t < 0.3f)
            {
                transform.localScale = Vector3.Lerp(baseScale, baseScale * 1.3f, t / 0.3f);
                t += Time.deltaTime;
                yield return null;
            }
            t = 0f;
            while (t < 0.3f)
            {
                transform.localScale = Vector3.Lerp(baseScale * 1.3f, baseScale, t / 0.3f);
                t += Time.deltaTime;
                yield return null;
            }
            transform.localScale = baseScale;
        }
    }
}
