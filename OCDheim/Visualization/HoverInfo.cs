using UnityEngine;

namespace OCDheim
{
    public class HoverInfo
    {
        private GameObject go { get; }
        private Transform transform { get; }
        private TextMesh textMesh { get; }
        public string text { get { return textMesh.text; } set { textMesh.text = value; } }
        public bool enabled { get { return go.activeSelf; } set { go.SetActive(value); } }
        public Color color { get { return textMesh.color; } set { textMesh.color = value; } }

        public HoverInfo(Transform parentTransform)
        {
            go = new GameObject();
            go.transform.parent = parentTransform;
            transform = go.transform;

            textMesh = go.AddComponent<TextMesh>();
            textMesh.transform.localPosition = Vector3.zero;
            //Fix: normalize the secondary VFX scale away from the hoverInfo scale
            textMesh.transform.localScale = new Vector3(0.1f / parentTransform.localScale.x, 0.1f / parentTransform.localScale.y, 0.1f / parentTransform.localScale.z);
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 16;
        }

        public void RotateToPlayer()
        {
            var playerXAxisDirection = new Vector3(GameCamera.instance.transform.position.x, transform.position.y, GameCamera.instance.transform.position.z);
            transform.LookAt(playerXAxisDirection, Vector3.up);
            transform.Rotate(90f, 180f, 0f);
        }
    }
}
