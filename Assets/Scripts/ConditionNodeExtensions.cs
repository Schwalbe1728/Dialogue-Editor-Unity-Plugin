using UnityEngine;

public partial class ConditionNode
{
    public ConditionTypes ConditionType;

    public RandomizerCondition randomizerCondition;

    public override bool ConditionTest()
    {
        return
            randomizerCondition != null && randomizerCondition.ConditionTest();
    }

    public RandomizerCondition SetRandomizerCondition(int min, int max, int valChecked)
    {
        if(randomizerCondition == null)
        {
            randomizerCondition = new RandomizerCondition();
        }

        randomizerCondition.MinValue = min;
        randomizerCondition.MaxValue = max;
        randomizerCondition.ValueChecked = valChecked;

        return randomizerCondition;
    }
}

[System.Serializable]
public enum ConditionTypes
{
    RandomizerCondition
}

[System.Serializable]
public class RandomizerCondition : ConditionNodeBase
{
    public int MinValue;
    public int MaxValue;
    public int ValueChecked;

    public override bool ConditionTest()
    {
        int val = Random.Range(MinValue, MaxValue + 1);
        Debug.Log("Randomizer Condition: " + val);

        return val > ValueChecked;
    }
}