using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestDialogueSystem : MonoBehaviour {

    [SerializeField]
    private Dialogue dialogue;

    [SerializeField]
    private GameObject optionPrefab;

    [SerializeField]
    private GameObject optionsPanel;

    [SerializeField]
    private Text npcText;

	// Use this for initialization
	void Start ()
    {
        dialogue.StartDialogue();
        npcText.text = dialogue.CurrentNode.Text;

        PopulateOptionsPanel(dialogue.CurrentNode);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void PopulateOptionsPanel(DialogueNode node)
    {
        for(int i = optionsPanel.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(optionsPanel.transform.GetChild(i));
        }

        if (!node.ImmediateNode)
        {
            foreach (int optionIndex in node.OptionsAttached)
            {
                DialogueOption option = dialogue.GetOption(optionIndex);

                if (option.EntryConditionSet)
                {

                }

                if (option.CanDisplay)
                {
                    GameObject prefabInstance = Instantiate(optionPrefab, optionsPanel.transform);
                    Button optionButton = prefabInstance.GetComponent<Button>();

                    optionButton.GetComponentInChildren<Text>().text = option.OptionText;
                }
            }
        }
    }
}
