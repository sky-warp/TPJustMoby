using System;
using System.Collections.Generic;

namespace _Project.Scripts.Services.Tower
{
    [Serializable]
    public class TowerState
    {
        public List<TowerElementState> Elements = new();
    }
}