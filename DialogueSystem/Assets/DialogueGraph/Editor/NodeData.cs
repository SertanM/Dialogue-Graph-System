using System;
using UnityEngine;
using DialogueEditor;

namespace DialogueEditor{
[Serializable]
public class NodeData
{
    public string Guid;
    public Vector2 Position;
    public string DialogueText;
    public string VariableName;
    public string VariableValue;
    public int type;
    
}
}