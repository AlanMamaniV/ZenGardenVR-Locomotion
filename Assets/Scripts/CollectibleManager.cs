using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRExplorer
{
    /// <summary>
    /// Gestor global de coleccionables en la isla.
    /// Registra objetos con XRGrabInteractable, actualiza contadores y notifica a la UI.
    /// </summary>
    public class CollectibleManager : MonoBehaviour
    {
        public static CollectibleManager Instance { get; private set; }

        [Header("Estado de Colección")]
        [SerializeField] private int totalCollectibles = 3;
        private int collectedCount = 0;

        public event Action<int, int> OnCollectibleCountChanged;
        public event Action OnAllCollectiblesFound;

        public int CollectedCount => collectedCount;
        public int TotalCollectibles => totalCollectibles;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterCollectibleFound(string itemName)
        {
            collectedCount++;
            OnCollectibleCountChanged?.Invoke(collectedCount, totalCollectibles);

            if (VRUIController.Instance != null)
            {
                VRUIController.Instance.UpdateCollectibleCounter(collectedCount, totalCollectibles);
                VRUIController.Instance.ShowNotification($"¡Encontraste: {itemName}! ({collectedCount}/{totalCollectibles})");
            }

            if (collectedCount >= totalCollectibles)
            {
                OnAllCollectiblesFound?.Invoke();
                if (VRUIController.Instance != null)
                {
                    VRUIController.Instance.ShowNotification("¡Has descubierto todos los objetos mágicos de la isla!");
                }
            }
        }
    }
}
