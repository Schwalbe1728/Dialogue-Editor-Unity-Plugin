using UnityEditor;
using UnityEngine;

public partial class NodeEditor
{
    public bool DrawRandomizerConditionInterior(ConditionNode currentCondition)
    {
        int min;
        int max;
        int val;

        bool changed = false;

        min =
           EditorGUILayout.IntField("Min Value: ", currentCondition.randomizerCondition.MinValue);

        max =
           EditorGUILayout.IntField("Max Value: ", currentCondition.randomizerCondition.MaxValue);

        val =
           EditorGUILayout.IntField("Value Checked: ", currentCondition.randomizerCondition.ValueChecked);

        if(min < max)
        {
            changed =
                currentCondition.randomizerCondition.MinValue != min ||
                currentCondition.randomizerCondition.MaxValue != max;

            currentCondition.randomizerCondition.MinValue = min;
            currentCondition.randomizerCondition.MaxValue = max;            
        }

        changed |= val != currentCondition.randomizerCondition.ValueChecked;

        if(val <= currentCondition.randomizerCondition.MaxValue)
        {
            currentCondition.randomizerCondition.ValueChecked =
                (val >= currentCondition.randomizerCondition.MinValue) ?
                    val : currentCondition.randomizerCondition.MinValue;
        }
        else
        {
            currentCondition.randomizerCondition.ValueChecked = currentCondition.randomizerCondition.MaxValue;
        }

        return changed;
    }
}