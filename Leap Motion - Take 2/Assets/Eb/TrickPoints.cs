using Fingerboard;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TrickPointsData", menuName = "ScriptableObjects/TrickPointsData")]
public class TrickPoints : ScriptableObject
{
    [System.Serializable]
    public class TrickData
    {
        public SkateboardController.Trick trick;
        public int basePoints;
    }
    public TrickData[] trickData;
    public int defaultPoints;

    public int GetBasePoints(SkateboardController.Trick _trick)
    {
        foreach (var data in trickData)
        {
            if(_trick == data.trick)
            {
                return data.basePoints;
            }
        }

        return defaultPoints;
    }
}
