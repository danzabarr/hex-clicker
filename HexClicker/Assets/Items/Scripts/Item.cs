using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Item", menuName ="Items/Basic")]
public class Item : ScriptableObject
{
    [SerializeField] private new string name;
    [SerializeField, TextArea] private string description;
    [SerializeField] private string emoticon;
    [SerializeField] private Texture2D icon;
    [SerializeField] private GameObject prefab;
    [SerializeField] private int maxStorageStack = 64;
    [SerializeField] private int maxCarryStack = 16;
    [SerializeField] private bool splittable = true;
    public string Name => name;
    public string Emoticon => emoticon;
    public Texture2D Icon => icon;
    public string Description => description;
    public GameObject Prefab => prefab;
    public int MaxStorageStack => maxStorageStack;
    public int MaxCarryStack => maxCarryStack;
    public bool Splittable => splittable;

    [ReadOnly] public int Quantity;
}
