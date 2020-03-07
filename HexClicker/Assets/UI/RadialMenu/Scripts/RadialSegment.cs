using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace HexClicker.UI.Menus
{
    public class RadialSegment : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private UnityEvent onClick;

        public MeshFilter MeshFilter { get; private set; }
        public MeshRenderer MeshRenderer { get; private set; }
        public SpriteRenderer Icon => icon;

        public Mesh Mesh
        {
            get
            {
                if (MeshFilter == null)
                    MeshFilter = GetComponent<MeshFilter>();
                return MeshFilter.sharedMesh;
            }
            set
            {
                if (MeshFilter == null)
                    MeshFilter = GetComponent<MeshFilter>();
                MeshFilter.sharedMesh = value;
            }
        }

        public void Invoke()
        {
            onClick.Invoke();
        }
    }
}
