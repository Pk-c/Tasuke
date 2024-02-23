#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace TasukeChan
{
    public class TkData : ScriptableObject
    {
        public List<TCategoryNode> cnodes;
        public List<TObjectNode> onodes;
    }
}
#endif
