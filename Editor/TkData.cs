using System.Collections.Generic;
using UnityEngine;

namespace TasukeChan
{

    [CreateAssetMenu(fileName = "tkBoard", menuName = "TasukeBoard/Create")]
    public class TkData : ScriptableObject
    {
        public List<CategoryNode> cnodes;
        public List<ObjectNode> onodes;
    }
}
