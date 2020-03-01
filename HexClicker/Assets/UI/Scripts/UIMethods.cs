using UnityEngine.EventSystems;

namespace HexClicker.UI
{
    public static class UIMethods
    {
        public static bool IsMouseOverUI => EventSystem.current.IsPointerOverGameObject();
    }
}
