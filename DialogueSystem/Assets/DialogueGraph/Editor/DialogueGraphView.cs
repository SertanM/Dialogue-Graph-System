using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using Codice.CM.Client.Differences;
using System.Linq;
using DialogueEditor;

namespace DialogueEditor{

public class DialogueGraphView : GraphView 
{
    public readonly Vector2 defaultNodeSize = new Vector2(100, 150);


    public Blackboard Blackboard;
    public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();
    private NodeSearchWindow _searchWindow;

    public DialogueGraphView(EditorWindow window){
        styleSheets.Add(Resources.Load<StyleSheet>("DialogueBackground"));
        this.SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        AddElement(GenerateEntryPointNode());
        AddSearchWindow(window);
    }

    private void AddSearchWindow(EditorWindow window)
    {
        _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        _searchWindow.Init(window,this);
        nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition),_searchWindow);
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter){
        var compatiblePorts = new List<Port>();

        ports.ForEach(port => {
            if(startPort!=port&&startPort.node!=port.node) compatiblePorts.Add(port);
        });

        return compatiblePorts;
    }

    private Port GeneratePort(MyNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single){
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float)); 
    }

    // private Port GeneratePortToAssignmentNode(AssignmentNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single){
    //     return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float)); 
    // }

    private DialogueNode GenerateEntryPointNode(){
        var node = new DialogueNode{
            title = "START",
            GUID = Guid.NewGuid().ToString(),
            DialogueText = "EntryPoint",
            EntryPoint = true
        };

        var port = GeneratePort(node, Direction.Output);
        port.portName = "Output";
        node.outputContainer.Add(port);

        node.capabilities &= ~Capabilities.Movable;
        node.capabilities &= ~Capabilities.Deletable;

        node.RefreshExpandedState();
        node.RefreshPorts();

        node.SetPosition(new Rect(100,200,100,150));
        
        return node;
    }

    public void CreateNode(string nodeName, Vector2 pos){
        AddElement(CreateDialogueNode(nodeName, pos));
    }

    public void CreateAssignment(string nodeName, Vector2 pos){
        AddElement(CreateAssignmentNode(nodeName, pos));
    }

    public AssignmentNode CreateAssignmentNode(string nodeName, Vector2 pos, bool willAddOutput = true){
        var AssignmentNode = new AssignmentNode{
            title = nodeName,
            variableName = "Variable",
            newValue = "Value",
            GUID = Guid.NewGuid().ToString()
        };

        var inputPort = GeneratePort(AssignmentNode, Direction.Input,Port.Capacity.Multi);
        inputPort.portName = "Input";
        AssignmentNode.inputContainer.Add(inputPort);

        if(willAddOutput) AddOutput(AssignmentNode);

        var nameTextField = new TextField(string.Empty);
        nameTextField.RegisterValueChangedCallback(evt => {
            AssignmentNode.variableName = evt.newValue;
        });
        nameTextField.SetValueWithoutNotify("VARIABLE");
        nameTextField.style.height = 30;
        AssignmentNode.mainContainer.Add(nameTextField);

        var newValueTextField = new TextField(string.Empty);
        newValueTextField.RegisterValueChangedCallback(evt => {
            AssignmentNode.newValue = evt.newValue;
        });
        newValueTextField.SetValueWithoutNotify("Value");
        newValueTextField.style.height = 30;
        AssignmentNode.mainContainer.Add(newValueTextField);

        

        AssignmentNode.RefreshExpandedState();
        AssignmentNode.RefreshPorts();
        AssignmentNode.SetPosition(new Rect(pos, defaultNodeSize));

        return AssignmentNode;
    }
    

    public DialogueNode CreateDialogueNode(string nodeName, Vector2 pos)
    {
        var DialogueNode = new DialogueNode{
            title = nodeName,
            DialogueText = nodeName,
            GUID = Guid.NewGuid().ToString(),
        };

        var inputPort = GeneratePort(DialogueNode, Direction.Input,Port.Capacity.Multi);
        inputPort.portName = "Input";
        DialogueNode.inputContainer.Add(inputPort);

        DialogueNode.styleSheets.Add(Resources.Load<StyleSheet>("Node"));

        var addChoiceButton = new Button(() => {
            AddChoice(DialogueNode);
        });
        addChoiceButton.text = "Add";
        addChoiceButton.AddToClassList("orange-button");
        addChoiceButton.styleSheets.Add(Resources.Load<StyleSheet>("Button"));
        DialogueNode.titleContainer.Add(addChoiceButton);
        
        var deleteChoiceButton = new Button(() => {
            DeleteChoice(DialogueNode);
        });
        deleteChoiceButton.text = "Delete";
        deleteChoiceButton.AddToClassList("orange-button");
        deleteChoiceButton.styleSheets.Add(Resources.Load<StyleSheet>("Button"));
        DialogueNode.titleContainer.Add(deleteChoiceButton);

        var textField = new TextField(string.Empty);
        textField.RegisterValueChangedCallback(evt => {
            DialogueNode.DialogueText = evt.newValue;
            //DialogueNode.title = evt.newValue;
        });
        textField.SetValueWithoutNotify(DialogueNode.title);
        DialogueNode.mainContainer.Add(textField);

        DialogueNode.RefreshExpandedState();
        DialogueNode.RefreshPorts();
        DialogueNode.SetPosition(new Rect(pos, defaultNodeSize));
        return DialogueNode;
    }

    public void AddOutput(AssignmentNode AssignmentNode){
        var outputPort = GeneratePort(AssignmentNode, Direction.Output);
        outputPort.portName = "Output";
        AssignmentNode.outputContainer.Add(outputPort);
    }

    public void RemoveLastOutput(AssignmentNode assignmentNode){
        assignmentNode.outputContainer.RemoveAt(1);
        assignmentNode.RefreshPorts();
    }

    public void AddChoice(DialogueNode dialogueNode, string overridenPortName = "")
    {
        var generatedPort = GeneratePort(dialogueNode, Direction.Output);

        var oldLabel = generatedPort.contentContainer.Q<Label>("type");
        generatedPort.contentContainer.Remove(oldLabel);

        var outputPortCount = dialogueNode.outputContainer.Query("connector").ToList().Count;
        generatedPort.portName = $"Choice {outputPortCount}";

        var choicePortName = string.IsNullOrEmpty(overridenPortName) ? $"Choice {outputPortCount}" : overridenPortName;

        var textField = new TextField{
            name = string.Empty,
            value = choicePortName
        };

        textField.style.height = 20f;
        textField.RegisterValueChangedCallback(evt=>generatedPort.portName=evt.newValue);

        generatedPort.contentContainer.Add(new Label("   "));
        generatedPort.contentContainer.Add(textField);

        generatedPort.portName = choicePortName;
        dialogueNode.outputContainer.Add(generatedPort);
        dialogueNode.RefreshExpandedState();
        dialogueNode.RefreshPorts();
        

    }

    private void DeleteChoice(DialogueNode dialogueNode){
        var count = dialogueNode.outputContainer.Query("connector").ToList().Count;
        if(count != 0){
            dialogueNode.outputContainer.RemoveAt(count - 1);
        }
        dialogueNode.RefreshExpandedState();
        dialogueNode.RefreshPorts();
    }

    public void ClearBlackBoardAndExposedProperties(){
        ExposedProperties.Clear();
        Blackboard.Clear();
    }

    public void AddPropertyToBlackBoard(ExposedProperty exposedProperty)
    {
        var localPropertyName = exposedProperty.PropertyName;
        var localPropertyValue = exposedProperty.PropertyValue;
        int t = -1;
        while(ExposedProperties.Any(x => x.PropertyName == localPropertyName)){
            t++;
            if(t!=0) localPropertyName = localPropertyName.Substring(0, localPropertyName.Length-(3+(t.ToString().Length)));
            localPropertyName = $"{localPropertyName} ({t})";
        }

        var property = new ExposedProperty();
        property.PropertyName = localPropertyName;
        property.PropertyValue = localPropertyValue;
        ExposedProperties.Add(property);

        var container = new VisualElement();
        var blackboardField = new BlackboardField{ text = property.PropertyName, typeText="string"};
        container.Add(blackboardField);

        var propertyValueTextField = new TextField("Value: "){
            value = localPropertyValue
        };
        propertyValueTextField.RegisterValueChangedCallback(evt => {
            var changingPropertyIndex = ExposedProperties.FindIndex(x => x.PropertyName==property.PropertyName);
            ExposedProperties[changingPropertyIndex].PropertyValue = evt.newValue;
        });
        var blackBoardValueRow = new BlackboardRow(blackboardField,propertyValueTextField);
        container.Add(blackBoardValueRow);

        Blackboard.Add(container);
    }
}
}