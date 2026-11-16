using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace VRExplorer
{
    /// <summary>
    /// Administrador de configuración VR en tiempo de ejecución.
    /// Valida altura de cámara, presencia de XR Origin, interacción y ergonomía de confort.
    /// </summary>
    public class VRSetupManager : MonoBehaviour
    {
        [Header("Referencias XR")]
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private TeleportationProvider teleportationProvider;
        [SerializeField] private SnapTurnProvider snapTurnProvider;

        [Header("Ergonomía y Prevención de Cinetosis")]
        [SerializeField] private bool disableContinuousMove = true;
        [SerializeField] private float comfortCameraHeight = 1.7f;

        private void Awake()
        {
            if (xrOrigin == null)
            {
                xrOrigin = Object.FindFirstObjectByType<XROrigin>();
            }

            if (teleportationProvider == null)
            {
                teleportationProvider = Object.FindFirstObjectByType<TeleportationProvider>();
            }

            if (snapTurnProvider == null)
            {
                snapTurnProvider = Object.FindFirstObjectByType<SnapTurnProvider>();
            }

            ApplyComfortSettings();
        }

        private void ApplyComfortSettings()
        {
            if (xrOrigin != null)
            {
                // En modo habitación o sentado, aseguramos configuración estándar de altura
                if (xrOrigin.CameraYOffset <= 0.1f)
                {
                    xrOrigin.CameraYOffset = comfortCameraHeight;
                }
            }

            // Desactivar cualquier locomoción continua si existiera para cumplir estrictamente la regla anti-náuseas
            if (disableContinuousMove)
            {
                var continuousMove = Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider>(FindObjectsSortMode.None);
                foreach (var cmp in continuousMove)
                {
                    cmp.enabled = false;
                }
            }

            // Snap Turn cómodo a 45 grados para evitar rotaciones continuas generadoras de mareo
            if (snapTurnProvider != null)
            {
                snapTurnProvider.turnAmount = 45f;
            }
        }
    }
}
