using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Services.Elements
{
    [CreateAssetMenu(fileName = "GameConfiguration", menuName = "Game/Game Configuration")]
    public sealed class GameConfiguration : ScriptableObject
    {
        [SerializeField] private List<ElementConfiguration> _configurations = new();
        public IReadOnlyList<ElementConfiguration> Configurations => _configurations;
    }
}