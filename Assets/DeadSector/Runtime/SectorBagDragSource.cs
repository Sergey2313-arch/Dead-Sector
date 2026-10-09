using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeadSector
{
    /// <summary>Drag an inventory cell onto a matching gear slot.</summary>
    public sealed class SectorBagDragSource : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        SectorModernUI owner;
        int slotIndex;
        Image sourceImage;

        public string ItemId =>
            owner != null ? owner.InventoryItemAt(slotIndex) : "";

        public void Configure(SectorModernUI ui, int index)
        {
            owner = ui;
            slotIndex = index;
            sourceImage = GetComponent<Image>();
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (owner == null || string.IsNullOrEmpty(ItemId))
                return;

            if (sourceImage != null)
                sourceImage.raycastTarget = false;
            owner.BeginItemDrag(ItemId, data.position);
        }

        public void OnDrag(PointerEventData data)
        {
            if (owner != null)
                owner.MoveItemDrag(data.position);
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (sourceImage != null)
                sourceImage.raycastTarget = true;
            if (owner != null)
                owner.EndItemDrag();
        }

        void OnDisable()
        {
            if (sourceImage != null)
                sourceImage.raycastTarget = true;
            if (owner != null)
                owner.EndItemDrag();
        }
    }

}
