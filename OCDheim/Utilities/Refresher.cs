using UnityEngine;

namespace OCDheim.Utilities
{
    public interface IRefresherable
    {
        void Refresh();
    }

    public class Refresher : MonoBehaviour
    {
        private IRefresherable refresherable { get; set; }

        private Refresher() { }

        public static Refresher Of(IRefresherable refresherable)
        {
            var refresherGo = new GameObject();
            var refresher = refresherGo.AddComponent<Refresher>();
            refresher.refresherable = refresherable;

            return refresher;
        }

        public void Update() => refresherable.Refresh();
    }
}
