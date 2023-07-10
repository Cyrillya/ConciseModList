using Terraria.GameContent.UI.Elements;

namespace ConciseModList;

public class ImprovedUIList : UIList
{
    public override void RecalculateChildren()
    {
        base.RecalculateChildren();
        int itemCount = 0;
        float pixels = 0f;
        foreach (var u in _items) {
            if (itemCount >= 6f)
            {
                itemCount = 0;
                pixels += 96f;
            }

            // Filter Message
            if (u is UIPanel and not ConciseUIModItem) {
                pixels += u.Height.Pixels + 8f;
                continue;
            }
            
            u.Left.Pixels = itemCount * 96f;
            u.Top.Pixels = pixels;
            itemCount++;
            u.Recalculate();
        }

        _innerListHeight = pixels + 96f;
    }
}
