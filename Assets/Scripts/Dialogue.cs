using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue")]
public class Dialogue : ScriptableObject
{
    public string Name { get { return this.name; } }
	
}

[System.Serializable]
public class DialogueOption
{

}
