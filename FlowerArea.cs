using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerArea : MonoBehaviour
{
    public const float AreaDiameter = 20f;

    private List<GameObject> flowerPlants;

    private Dictionary<Collider, Flower> nectarFlowerDictionary;

    public List<Flower> Flowers { get; private set; }
}
