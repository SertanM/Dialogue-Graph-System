using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using UnityEditor.Experimental.GraphView;
using System.Linq;
using DialogueEditor;

namespace DialogueEditor{
public class DialogueGraph : EditorWindow
{
    private DialogueGraphView _graphView;
    private string _fileName = "New Narrative";

    [MenuItem("Graph/Dialogue Graph")]
    public static void OpenDialogueGraphWindow(){
        var window = GetWindow<DialogueGraph>();
        window.titleContent = new GUIContent("Dialogue Graph");
        
    }

    private void OnEnable(){
        ConstructGraphView();
        GenerateToolBar();
        //GenerateMinimap();
        GenerateBlackboard();
    }

    private void GenerateBlackboard()
    {
        var blackboard = new Blackboard(_graphView);
        blackboard.Add(new BlackboardSection{
            title = "Exposed Properties"
        });

        blackboard.addItemRequested = _blackboard => {
            _graphView.AddPropertyToBlackBoard(new ExposedProperty());
        };
        blackboard.editTextRequested = (blackboard1, element, newValue) => {
            var oldPropertyName = ((BlackboardField)element).text;
            if(_graphView.ExposedProperties.Any(x=>x.PropertyName==newValue)){
                EditorUtility.DisplayDialog("Error", "This property name already exist, please chose another one", "Ok");
                return;
            }

            var propertyIndex = _graphView.ExposedProperties.FindIndex(x => x.PropertyName==oldPropertyName);
            _graphView.ExposedProperties[propertyIndex].PropertyName = newValue;
            ((BlackboardField)element).text = newValue;
        };

        blackboard.SetPosition(new Rect(10,30,200,300));

        _graphView.Add(blackboard);
        _graphView.Blackboard = blackboard;
    }

    // private void GenerateMinimap()
    // {
    //     var minimap = new MiniMap{anchored=true};
    //     minimap.SetPosition(new Rect(10,30,200,140));
    //     _graphView.Add(minimap);
    // }

    private void OnDisable(){
        rootVisualElement.Remove(_graphView);
    }

    private void GenerateToolBar(){
        var toolBar = new Toolbar();

        var fileNameTextField = new TextField("File Name:");
        fileNameTextField.SetValueWithoutNotify(_fileName);
        fileNameTextField.MarkDirtyRepaint();
        fileNameTextField.RegisterValueChangedCallback(evt => _fileName = evt.newValue);
        toolBar.Add(fileNameTextField);
        
        


        toolBar.Add(new Button(() => RequestDataOperation(true)){text = "Save Data"});
        toolBar.Add(new Button(() => RequestDataOperation(false)){text = "Load Data"});

        rootVisualElement.Add(toolBar);
    }

    private void RequestDataOperation(bool save)
    {
        if(string.IsNullOrEmpty(_fileName)){
            EditorUtility.DisplayDialog("File name is empty!","Please enter a valid file name", "Ok");
            return;
        }

        var saveUtility = GraphSaveUtility.GetInstance(_graphView);
        if(save) saveUtility.SaveGraph(_fileName);
        else saveUtility.LoadGraph(_fileName);
        
    }

    private void ConstructGraphView(){
        _graphView = new DialogueGraphView(this){
            name = "Dialogue Graph"
        };

        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }
}
}