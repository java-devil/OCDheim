using UnityEngine;

namespace OCDheim
{
    public class Overlay
    {
        private GameObject go { get; }
        private ParticleSystem.Particle[] particles { get; } = new ParticleSystem.Particle[2];
        public Transform transform { get; }
        public ParticleSystem ps { get; }
        public ParticleSystemRenderer psr { get; }
        public ParticleSystem.MainModule psm { get; }
        public bool enabled { get { return go.activeSelf; } set { go.SetActive(value); } }
        public Vector3 position { get { return transform.position; } set { transform.position = value; } }
        public Vector3 locPosition { get { return transform.localPosition; } set { transform.localPosition = value; } }
        public Quaternion rotation { get { return transform.rotation; } set { transform.rotation = value; } }
        public Color color { get { ps.GetParticles(particles, 2); return particles[1].GetCurrentColor(ps); } }
        public Color startColor { get { return ps.startColor; } set { ps.startColor = value; } }
        public float startSize { get { return psm.startSize.constant; } set { var psMain = ps.main; psMain.startSize = value; } }
        public float startSpeed { get { return psm.startSize.constant; } set { var psMain = ps.main; psMain.startSpeed = value; } }
        public float startLifetime { get { return psm.startLifetime.constant; } set { var psMain = ps.main; psMain.startLifetime = value; } }
        public bool sizeOverLifetimeEnabled { get { return ps.sizeOverLifetime.enabled; } set { var psSizeOverLifetime = ps.sizeOverLifetime; psSizeOverLifetime.enabled = value; } }
        public ParticleSystem.MinMaxCurve sizeOverLifetime { get { return ps.sizeOverLifetime.size; } set { var psSizeOverLifetime = ps.sizeOverLifetime; psSizeOverLifetime.size = value; } }

        public Overlay(Transform transform)
        {
            this.transform = transform;

            go = transform.gameObject;
            ps = transform.GetComponentInChildren<ParticleSystem>();
            psr = transform.GetComponentInChildren<ParticleSystemRenderer>();
        }
    }
}
