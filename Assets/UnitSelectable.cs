using UnityEngine;

public class UnitSelectable : MonoBehaviour
{
    public GameObject selectionRing;

    public void Select()
    {
        if (selectionRing != null)
            selectionRing.SetActive(true);
    }

    public void Deselect()
    {
        if (selectionRing != null)
            selectionRing.SetActive(false);
    }
}