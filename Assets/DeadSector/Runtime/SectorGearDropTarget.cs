using UnityEngine;
using UnityEngine.EventSystems;

namespace DeadSector
{
    /// <summary>Rejects weapons/materials dropped onto armor slots.</summary>
    public sealed class SectorGearDropTarget : MonoBehaviour, IDropHandler
    {
        SectorModernUI owner;
        int targetIndex;

        public void Configure(SectorModernUI ui, int slot)
        {
            owner = ui;
            targetIndex = slot;
        }

        public void OnDrop(PointerEventData data)
        {
            if (owner == null || data.pointerDrag == null)
                return;

            SectorBagDragSource source =
                data.pointerDrag.GetComponent<SectorBagDragSource>();
            if (source != null)
                owner.EquipDraggedArmor(targetIndex, source.ItemId);
        }
    }
}
