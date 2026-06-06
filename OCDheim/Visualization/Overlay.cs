using UnityEngine;

namespace OCDheim
{
    public class Overlay
    {
        public ParticleSystemRenderer psr { get; }

        public bool enabled { set => overlayGo.SetActive(value); }
        public Vector3 worldPosition => transform.position;
        public Vector3 localPosition { get => transform.localPosition; set => transform.localPosition = value; }
        public Vector3 scale { set => transform.localScale = value; }
        public Color color { get { ps.GetParticles(particles, 2); return particles[1].GetCurrentColor(ps); } }
        public Color startColor
        {
            get => ps.main.startColor.color;
            set
            {
                var psMain = ps.main;
                var psmStartColor = ps.main.startColor;
                psmStartColor.color = value;
                psMain.startColor = psmStartColor;
            }
        }
        
        public float startSize { set { var psMain = ps.main; psMain.startSize = value; } }
        public float startSpeed { set { var psMain = ps.main; psMain.startSpeed = value; } }
        public float startLifetime { set { var psMain = ps.main; psMain.startLifetime = value; } }
        public bool sizeOverLifetimeEnabled { set { var psSizeOverLifetime = ps.sizeOverLifetime; psSizeOverLifetime.enabled = value; } }
        public ParticleSystem.MinMaxCurve sizeOverLifetime { set { var psSizeOverLifetime = ps.sizeOverLifetime; psSizeOverLifetime.size = value; } }

        private GameObject overlayGo { get; }
        private Transform transform { get; }
        private ParticleSystem ps { get; }
        private ParticleSystem.Particle[] particles { get; } = new ParticleSystem.Particle[2];

        public Overlay(Transform transform)
        {
            this.transform = transform;

            overlayGo = transform.gameObject;
            ps = transform.GetComponentInChildren<ParticleSystem>();
            psr = transform.GetComponentInChildren<ParticleSystemRenderer>();
        }
    }
}
