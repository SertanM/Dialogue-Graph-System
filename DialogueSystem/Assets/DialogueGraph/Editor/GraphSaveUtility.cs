using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueEditor;

namespace DialogueEditor{
public class GraphSaveUtility
{
    private DialogueGraphView _targetGraphView;
    private DialogueContainer _containerCache;

    private List<Edge> Edges => _targetGraphView.edges.ToList();
    private List<MyNode> Nodes => _targetGraphView.nodes.ToList().Cast<MyNode>().ToList();
    public static GraphSaveUtility GetInstance(DialogueGraphView targetGraphView){
        return new GraphSaveUtility{
            _targetGraphView = targetGraphView
        };
    }

    public void SaveGraph(string fileName){
        var dialogueContainer = ScriptableObject.CreateInstance<DialogueContainer>();
        if(!SaveNodes(dialogueContainer)) return;
        SaveExposedProperties(dialogueContainer);

        if(!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");

        AssetDatabase.CreateAsset(dialogueContainer, $"Assets/Resources/{fileName}.asset");
        AssetDatabase.SaveAssets();
    }

    

    private bool SaveNodes(DialogueContainer dialogueContainer){
        if(!Edges.Any()) return false;


        var connectedPorts = Edges.Where(x => x.input.node!=null).ToArray();
        for(var i =0; i < connectedPorts.Length; i++){
            var outputNode = connectedPorts[i].output.node as MyNode;
            var inputNode = connectedPorts[i].input.node as MyNode;

            dialogueContainer.NodeLinks.Add(new NodeLinkData{
                BaseNodeGuid = outputNode.GUID,
                PortName = connectedPorts[i].output.portName,
                TargetNodeGuid = inputNode.GUID
            });//Çalışıyor
        }

        foreach(var dialogueNode in Nodes.Where(node=>!node.EntryPoint)){
            dialogueContainer.DialogueNodes.Add(new NodeData{
                Guid = dialogueNode.GUID,
                type = dialogueNode.NodesType,
                DialogueText = dialogueNode.DialogueText,
                
                VariableName = dialogueNode.variableName,
                VariableValue = dialogueNode.newValue,
                Position = dialogueNode.GetPosition().position
            });//Çalışıyor
            
        }

        return true;
    }

    private void SaveExposedProperties(DialogueContainer dialogueContainer)
    {
        dialogueContainer.exposedProperties.AddRange(_targetGraphView.ExposedProperties);
    }

    public void LoadGraph(string fileName){
        _containerCache = Resources.Load<DialogueContainer>(fileName);
        if(_containerCache==null){
            EditorUtility.DisplayDialog("File not found!", "Target dialogue graph is does not exist!", "Ok");
            return;
        }

        ClearGraph();
        CreateNodes();
        ConnectNodes();
        CreateExposedProperties();
    }

    private void CreateExposedProperties()
    {
        _targetGraphView.ClearBlackBoardAndExposedProperties();

        foreach(var exposedProperty in _containerCache.exposedProperties){
            _targetGraphView.AddPropertyToBlackBoard(exposedProperty);
        }
    }

    private void ConnectNodes() //HATALI
    {
        for(var i = 0; i < Nodes.Count; i++){
            var connections = _containerCache.NodeLinks.Where(x=>x.BaseNodeGuid == Nodes[i].GUID).ToList();
            for(var j = 0; j < connections.Count; j++){
                var targetNodeGuid = connections[j].TargetNodeGuid;

                var targetNode = Nodes.First(x=>x.GUID==targetNodeGuid);
                LinkNodes(Nodes[i].outputContainer[j].Q<Port>(), (Port) targetNode.inputContainer[0]);
                targetNode.SetPosition(new Rect(
                    _containerCache.DialogueNodes.First(x=>x.Guid==targetNodeGuid).Position,
                    _targetGraphView.defaultNodeSize
                ));
                
            }
        }
    }

    private void LinkNodes(Port output, Port input)
    {
        var tempEdge = new Edge{
            output = output,
            input = input
        };
        tempEdge?.input.Connect(tempEdge);
        tempEdge?.output.Connect(tempEdge);
        _targetGraphView.Add(tempEdge);
    }

    private void CreateNodes()
    {
        foreach(var nodeData in _containerCache.DialogueNodes){
            if(nodeData.type == 0){
                var tempNode = _targetGraphView.CreateDialogueNode(nodeData.DialogueText, Vector2.zero);
                tempNode.GUID = nodeData.Guid;
                _targetGraphView.AddElement(tempNode);
                var nodePorts = _containerCache.NodeLinks.Where(x=>x.BaseNodeGuid==nodeData.Guid).ToList();
                nodePorts.ForEach(x => _targetGraphView.AddChoice(tempNode,x.PortName));
                
            } 
            else if(nodeData.type == 1){
                var tempNode = _targetGraphView.CreateAssignmentNode("Assignment Node", Vector2.zero);
                tempNode.GUID = nodeData.Guid;
                tempNode.variableName = nodeData.VariableName;
                tempNode.newValue = nodeData.VariableValue;
                _targetGraphView.AddElement(tempNode);
                var nodePort = _containerCache.NodeLinks.Where(x=>x.BaseNodeGuid==nodeData.Guid).ToList();
                nodePort.ForEach(x => _targetGraphView.AddOutput(tempNode));
                _targetGraphView.RemoveLastOutput(tempNode);
            }
            else {
                Debug.LogError("What the helL?");
                
            }

            
        }
    }

    private void ClearGraph()
    {
        Nodes.Find(x => x.EntryPoint).GUID = _containerCache.NodeLinks[0].BaseNodeGuid;

        foreach(var node in Nodes){
            if(node.EntryPoint) continue; 
            Edges.Where(x => x.input.node==node).ToList().ForEach(edge => _targetGraphView.RemoveElement(edge));

            _targetGraphView.RemoveElement(node);
        }
    }
}
}