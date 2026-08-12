using UnityEngine;

public class UnitSelectable : MonoBehaviour
{
    public void Select()
    {
        Debug.Log("[PLAYER] Unit selected");
    }

    public void Deselect()
    {
        Debug.Log("[PLAYER] Unit deselected");
    }
}