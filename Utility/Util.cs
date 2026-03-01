using UnityEngine;
using TMPro;
public static class Util
{
    public static TextMeshProUGUI CreateWorldText(Transform parent, string text, Vector2 position, int fontSize, Color color)
    {
        return CreateWorldText(parent, text, position, fontSize, color, TextAnchor.MiddleCenter, TextAlignment.Center);
    }

    public static TextMeshProUGUI CreateWorldText(Transform parent, string text, Vector2 position, int fontSize, Color color, TextAnchor textAnchor, TextAlignment textAlignment)
    {
        //GameObject gameObject = new GameObject("World_Text", typeof(TextMeshPro));
        Transform worldText = GameObject.Instantiate(PrefabReference.instance.worldText, PrefabReference.instance.folderDynamicObjects);
        worldText.SetParent(parent, false);
        worldText.position = position;//new Vector3(position.x, position.y, 1);
        TextMeshProUGUI tmp = worldText.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        //textMeshPro.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;     
        return tmp;
    }
    public static Vector2 GetMouseWorldPosition()
    {
        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    public static Quaternion LookAt(Vector2 dir)
    {
        return Quaternion.Euler(0, 0, UtilMath.GetAngleFromDirection(dir) - 90);
    }




}
