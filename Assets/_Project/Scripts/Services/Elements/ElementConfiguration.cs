using UnityEngine;

namespace _Project.Scripts.Services.Elements
{
    [CreateAssetMenu(fileName = "ElementConfiguration", menuName = "Game/Element Configuration")]
    public sealed class ElementConfiguration : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private ElementColor _color;

        public string Id => _id;
        public Sprite Sprite => _sprite;
        public ElementColor ColorId => _color;
    }
}