using UnityEngine;

namespace OCDheim
{
    public class HoverInfo
    {
        private GameObject hoverInfoGo { get; }
        private Transform transform { get; }
        private TextMesh textMesh { get; }
        public bool enabled { get => hoverInfoGo.activeSelf; set  => hoverInfoGo.SetActive(value); }
        public string text { set => textMesh.text = value; }
        public Color color { set => textMesh.color = value; }

        public HoverInfo(Transform parentTransform)
        {
            hoverInfoGo = new GameObject();
            hoverInfoGo.transform.parent = parentTransform;
            transform = hoverInfoGo.transform;

            textMesh = hoverInfoGo.AddComponent<TextMesh>();
            textMesh.transform.localPosition = Vector3.zero;
            //Fix: normalize the secondary VFX scale away from the hoverInfo scale
            textMesh.transform.localScale = new Vector3(0.1f / parentTransform.localScale.x, 0.1f / parentTransform.localScale.y, 0.1f / parentTransform.localScale.z);
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 16;
        }

        public void RotateToPlayer()
        {
            var playerXAxisDirection = new Vector3(GameCamera.m_instance.transform.position.x, transform.position.y, GameCamera.m_instance.transform.position.z);
            transform.LookAt(playerXAxisDirection, Vector3.up);
            transform.Rotate(90f, 180f, 0f);
        }
    }
}
