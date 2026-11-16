using System.Collections;
using UnityEngine;
using TMPro;

namespace VRExplorer
{
    /// <summary>
    /// Controlador central de la interfaz de usuario en el mundo 3D (World Space Canvas / Carteles).
    /// Muestra bienvenida, instrucciones, notificaciones dinámicas, progreso y victoria final.
    /// </summary>
    public class VRUIController : MonoBehaviour
    {
        public static VRUIController Instance { get; private set; }

        [Header("Textos UI Principales")]
        [SerializeField] private TextMeshProUGUI welcomeTitleText;
        [SerializeField] private TextMeshProUGUI welcomeDescText;
        [SerializeField] private TextMeshProUGUI instructionsText;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private TextMeshProUGUI counterText;
        [SerializeField] private TextMeshProUGUI progressText;

        [Header("Configuración")]
        [SerializeField] private float notificationDuration = 4.5f;

        private Coroutine notificationCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultTexts();
        }

        private void InitializeDefaultTexts()
        {
            if (welcomeTitleText != null)
                welcomeTitleText.text = "BIENVENIDO A LA ISLA MÁGICA";

            if (welcomeDescText != null)
                welcomeDescText.text = "Explora el entorno, descubre los cristales y encuentra los objetos ocultos.";

            if (instructionsText != null)
            {
                instructionsText.text =
                    "• Apunta hacia el terreno para teletransportarte.\n" +
                    "• Confirma el destino con el controlador.\n" +
                    "• Utiliza los controles para interactuar con los objetos.\n" +
                    "• Descubre el santuario mágico.";
            }

            if (counterText != null)
                counterText.text = "Objetos Mágicos: 0 / 3";

            if (progressText != null)
                progressText.text = "Progreso: 0% - Comienza tu viaje en la entrada";

            if (notificationText != null)
                notificationText.text = "Pista: Teletranspórtate por el camino hacia el Bosque Mágico.";
        }

        public void ShowNotification(string message)
        {
            if (notificationText == null) return;

            if (notificationCoroutine != null)
                StopCoroutine(notificationCoroutine);

            notificationCoroutine = StartCoroutine(DisplayNotificationRoutine(message));
        }

        private IEnumerator DisplayNotificationRoutine(string message)
        {
            notificationText.text = message;
            yield return new WaitForSeconds(notificationDuration);
            notificationText.text = "Continúa explorando la isla mágica...";
        }

        public void UpdateCollectibleCounter(int current, int total)
        {
            if (counterText != null)
            {
                counterText.text = $"Objetos Mágicos: {current} / {total}";
            }

            if (progressText != null)
            {
                float percent = (float)current / Mathf.Max(1, total) * 100f;
                progressText.text = $"Progreso de Colección: {percent:0}%";
            }
        }

        public void ShowSanctuaryDiscovered()
        {
            ShowNotification("¡¡SANTUARIO MÁGICO DESPERTADO!! ¡Has desbloqueado el núcleo arcano de la isla!");

            if (progressText != null)
            {
                progressText.text = "¡EXPLORACIÓN DE LA ISLA COMPLETADA CON ÉXITO!";
                progressText.color = new Color(0.2f, 1f, 0.4f);
            }
        }
    }
}
