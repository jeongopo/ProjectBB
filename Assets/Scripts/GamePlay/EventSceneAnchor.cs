using UnityEngine;

namespace GamePlay
{
    [DisallowMultipleComponent]
    public sealed class EventSceneAnchor : MonoBehaviour
    {
        [SerializeField] private string _id;

        internal string Id => _id;
    }
}
