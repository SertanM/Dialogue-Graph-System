using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueEditor{
    [Serializable]
    public class DialogueContainer : ScriptableObject
    {
        public List<NodeLinkData> NodeLinks = new List<NodeLinkData>();
        public List<NodeData> DialogueNodes = new List<NodeData>();
        public List<ExposedProperty> exposedProperties= new List<ExposedProperty>();

    }

}
