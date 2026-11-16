using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace VRExplorer
{
    /// <summary>
    /// Gestor de Teletransporte para VR Explorer.
    /// Garantiza la correcta configuración de TeleportationArea en el terreno y caminos,
    /// asegurando transiciones suaves, retículas visuales reactivas y confort anti-náuseas.
    /// </summary>
    public class TeleportManager : MonoBehaviour
    {
        [Header("Configuración de Teletransporte")]
        [SerializeField] private TeleportationProvider teleportationProvider;

        [Header("Superficies Transitables")]
        [SerializeField] private GameObject islandTerrain;
        [SerializeField] private GameObject pathwayGroup;

        private void Start()
        {
            ValidateAndSetupTeleportation();
        }

        public void ValidateAndSetupTeleportation()
        {
            if (teleportationProvider == null)
            {
                teleportationProvider = Object.FindFirstObjectByType<TeleportationProvider>();
            }

            // Asegurar que el terreno principal disponga de TeleportationArea
            if (islandTerrain != null)
            {
                EnsureTeleportArea(islandTerrain);
            }

            if (pathwayGroup != null)
            {
                EnsureTeleportArea(pathwayGroup);
            }
        }

        public static TeleportationArea EnsureTeleportArea(GameObject targetObj)
        {
            if (targetObj == null) return null;

            TeleportationArea area = targetObj.GetComponent<TeleportationArea>();
            if (area == null)
            {
                area = targetObj.AddComponent<TeleportationArea>();
            }

            // Asegurar que tenga Collider para Raycast
            Collider col = targetObj.GetComponent<Collider>();
            if (col == null)
            {
                col = targetObj.AddComponent<MeshCollider>();
            }

            // En XRI 3.6.1, WorldSpaceUp conserva la orientación horizontal y alinea el vector Up
            area.matchOrientation = MatchOrientation.WorldSpaceUp;

            return area;
        }
    }
}
