using System.Collections.Generic;
using UnityEngine;

//--------------------------------------------------------------------
// IMPACTS PROCESSING

// Impacts Values (list of influenced Attributes)

public struct ImpactValues
{
    public string field;
    public int value;
}

// Impact structure

public struct Impact
{
    public string name;                      // Impact Name (should be same as Skill name)
    public NewUnit trigger;                  // Unit which triggered Impact (of related Cell Effect)
    public int endTurn;                      // Expires in Turn
    public int duration;                     // Expires in Turn determination (Cell Effect)
    public int exception;                    // Requires extra processing each Turn
    public bool OnEndProcess;                // Process Impact at the end of Unit Turn
    public bool isBuff;                      // Positive/negative Impact
    public bool OnExpireAction;              // Requires extra processing when expired
    public bool OnDeathAction;               // Requires extra processing when Target dies
    public bool isDamage;                    // Impact deals (DOT) damage indicator
    public List<ImpactValues> fieldsChanged; // Attributes influenced by Impact  
}

//----------------------------------
// Class for processing Impacts on currently playing Unit

public class Impacts : MonoBehaviour {

    //----------------------------------
    // ATTRIBUTES

    public NewUnit unit;             // Playing Unit
    private List<Impact> unitImpact; // Unit's current Impacts
    List<ImpactValues> values;       // Processed Impact values
    private int turn;                // Current Turn in Game

    //------------------------------------------
    // METHODS 

    // Main method to determine how each Impact will be processed (at start on each Unit Turn)

    public void ProcessImpacts()
    {
        unitImpact = unit.impacts;
        turn = unit.controller.Turn; // get current Turn

        for (int i = 0; i < unitImpact.Count; i++)
        {
            if (unitImpact[i].endTurn == turn && unitImpact[i].OnEndProcess != true) 
            {
                ProcessLastTurn(unitImpact[i]); // process Impact's which expires at start of current Turn
                i -= 1;
            }
            else
            {

                if (unitImpact[i].isDamage == true)
                {
                    ProcessDamage(unitImpact[i]); // process Impact's which deals DOT
                }

                if (unitImpact[i].exception != 0)
                {
                    ProcessExtra(unitImpact[i]); // process Impact's which requires extra processing
                }
            }
        }
    }

    // Method for removing Impact at end of Unit's Turn

    public void RemoveOnEndTurn()
    {
        // Get expired Impacts with 'endTurn' processing
        var toRemove = unit.impacts.FindAll(n => n.endTurn == turn && n.OnEndProcess == true);
        for (int i = 0; i < toRemove.Count; i++)
        {
            ProcessLastTurn(toRemove[i]);
        }
    }

    // Method for processing expired Impacts

    private void ProcessLastTurn(Impact expired)
    {
        values = expired.fieldsChanged;

        if (values != null)
        {
            foreach (var impact in values.FindAll(n => n.field != "HealthAct")) // process all except Health
            {
                var fldName = impact.field; // get field 

                int fieldValue = (int)unit.GetType().GetField(fldName).GetValue(unit);                         // get current value
                int orig = (int)unit.originalValues.GetType().GetField(fldName).GetValue(unit.originalValues); // get original value

                fieldValue = fieldValue - (orig * impact.value) / 100;

                unit.GetType().GetField(fldName).SetValue(unit, fieldValue); // set new value
            }
        }

        if (expired.OnExpireAction == true) // trigger 'onExpired' action
        {
            expired.trigger.TargetImpactExpired(unit);
        }

        unit.impacts.Remove(expired); // remove Impact from list
    }

    // Method for processing Impacts with DOT

    private void ProcessDamage(Impact damage)
    {
        var damageAmount = damage.fieldsChanged.FindLast(n => n.field == "HealthAct").value;
        int fieldValue = (int)unit.GetType().GetField("HealthAct").GetValue(unit); // get current value

        fieldValue = fieldValue - damageAmount;

        unit.GetType().GetField("HealthAct").SetValue(unit, fieldValue); // set new value
        unit.combatLog.LogSkill(damage.name + " caused  " + damageAmount + " damage to " + unit.UnitName + "."); // LOG damage test

        // Process counteraction
        if (unit.counterAction == true)
        {
            unit.Counteraction(unit);
        }

        // Check Unit is alive
        unit.IsAliveCheck(true);
    }

    // Method for processing Impacts with extra processing

    private void ProcessExtra(Impact extra)
    {
        switch (extra.exception)
        {
            case 1: // Issue a Challenge

                unit.IsImmobile = true;              // Unit can't move
                unit.unitAllowed.Add(extra.trigger); // Unit can attack only Impact trigger
                break;

            case 2: // God's wrap

                unit.IsImmobile = true; // Unit can't move
                break;

            case 3: // Eternal Glory aura

                unit.ActionsPointsAct += 1; // Raise AP (+1) for this Turn
                break;

            case 4: // Mind Break

                unit.PlayerNumber = extra.trigger.PlayerNumber; // switch Team (TBD)
                break;
        }
    }

    // Method for adding new Impacts for Unit

    public void ApplyNew(Impact newImpact)
    {
        values = newImpact.fieldsChanged; // get Attributes to be changed
        if (values != null)
        {
            foreach (var impact in values.FindAll(n => n.field != "HealthAct"))
            {
                var fldName = impact.field; // get field 

                int fieldValue = (int)unit.GetType().GetField(fldName).GetValue(unit);                         // get current value
                int orig = (int)unit.originalValues.GetType().GetField(fldName).GetValue(unit.originalValues); // get original value

                fieldValue = fieldValue + (orig * impact.value) / 100;

                unit.GetType().GetField(fldName).SetValue(unit, fieldValue); // set new value
            }
        }

        // If duration exists (Cell Effect Impacts), it has priority over EndTurn   
        if (newImpact.duration != 0)
        {
            newImpact.endTurn = turn + newImpact.duration;
        }

        unit.impacts.Add(newImpact);
    }

    // Method for modifying Impacts for Units

    public void Modify(Impact modImpact) // check if 'modify' only can be applied
    {
        // Remove out-dated Impact and restore it's values
        var index = unit.impacts.FindIndex(n => n.name == modImpact.name);                  // get Index (for later insert)
        var oldValues = unit.impacts.FindLast(n => n.name == modImpact.name).fieldsChanged; // read out-dated Impact

        foreach (var impact in oldValues.FindAll(n => n.field != "HealthAct"))
        {
            var fldName = impact.field; // get field 

            int fieldValue = (int)unit.GetType().GetField(fldName).GetValue(unit);                         // get current value
            int orig = (int)unit.originalValues.GetType().GetField(fldName).GetValue(unit.originalValues); // get original value

            fieldValue = fieldValue - (orig * impact.value) / 100;

            unit.GetType().GetField(fldName).SetValue(unit, fieldValue); // set new value
        }

        unit.impacts.RemoveAll(n => n.name == modImpact.name); // remove from Unit Impact list

        values = modImpact.fieldsChanged;

        // Add updated Impact and restore it's values

        foreach (var impact in values.FindAll(n => n.field != "HealthAct"))
        {
            var fldName = impact.field; // get field 

            int fieldValue = (int)unit.GetType().GetField(fldName).GetValue(unit);                         // get current value
            int orig = (int)unit.originalValues.GetType().GetField(fldName).GetValue(unit.originalValues); // get original value

            fieldValue = fieldValue + (orig * impact.value) / 100;

            unit.GetType().GetField(fldName).SetValue(unit, fieldValue); // set new value
        }

        unit.impacts.Insert(index,modImpact); // Unit Impact list
    }

    // Method for removing Impact 

    public void RemoveImpact(Impact toRemove)
    {
        values = toRemove.fieldsChanged;
        if (values != null)
        {
            foreach (var impact in values.FindAll(n => n.field != "HealthAct"))
            {
                var fldName = impact.field; // get field 

                int fieldValue = (int)unit.GetType().GetField(fldName).GetValue(unit);                         // get current value
                int orig = (int)unit.originalValues.GetType().GetField(fldName).GetValue(unit.originalValues); // get original value

                fieldValue = fieldValue - (orig * impact.value) / 100;

                unit.GetType().GetField(fldName).SetValue(unit, fieldValue); // set new value
            }
        }

        unit.impacts.Remove(toRemove);
    }

    // Remove Impact by name

    public void RemoveImpactByName(string toRemove)
    {
        unit.impacts.RemoveAll(n => n.name == toRemove);
    }
}





