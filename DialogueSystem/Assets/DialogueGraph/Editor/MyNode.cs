using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using DialogueEditor;
namespace DialogueEditor{
public abstract class MyNode : Node
{
    public virtual int NodesType {get => 2;}
    public string GUID;
    public bool EntryPoint = false;
    public string DialogueText;

    //---------------------------
    public string variableName;
    public string newValue;
}
}
