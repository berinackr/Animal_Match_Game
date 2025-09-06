using UnityEngine;

[CreateAssetMenu(menuName = "Match3/GemType")]
public class GemType : ScriptableObject
{
    public string id;
    public Sprite sprite;
    public int score = 10;
}
