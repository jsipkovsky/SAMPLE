using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

//------------------------------------------
// STRUCTURES

// Attack structure
public struct AttackStruc
{
    public int attackValue;
    public string attackMark;
}

// Damage List structure
public struct DamageList
{
    public NewUnit target;
    public int damage;
    public string logMessage;
}

// Obstacles structure
//public struct Obstacle
//{
//    public string name;
//    public string trigger;
//    public int endTurn;
//    public bool isSolid; // check
//    public Cell cell1;
//}

// Structure for storing original Attributes
public struct AttrOrig
{
    public string UnitTag;
    public int PhysicalAttack;
    public int MagicalAttack;
    public int SpecialAttack;
    public int PhysicalDefence;
    public int MagicalDefence;
    public int SpecialDefence;
    public int ActionPoints;
    public int Damage;
}

//------------------------------------------
// Parent class for each Unit in the game

public abstract class NewUnit : MonoBehaviour
{
    //------------------------------------------
    // ATTRIBUTES

    // Unit Parameters
    public AttrOrig originalValues; // original values of Attributes

    public string UnitName;       // Unit Name
    public int PlayerNumber;      // Player Number (0 or 1)

    public int PhysicalAttack;    // Physical Attack
    public int MagicalAttack;     // Magical Attack  
    public int SpecialAttack;     // Special Attack  
    public int PhysicalDefence;   // Physical Defence
    public int MagicalDefence;    // Magical Defence
    public int SpecialDefence;    // Special Defence

    public int Health;            // Health Maximum
    public int HealthAct;         // Health Actual

    public int Damage;            // Damage

    public int RangeFrom;         // Minimal distance from where can attack
    public int RangeTo;           // Maximal distance to where can attack

    public int Iniciative;        // Iniciative (to determine sequence when Units play)
    public int ActionPoints;      // Action Poins (Maximum) 
    public int ActionsPointsAct;  // Action Poins (Actual) 
    public int MovementSpeed;     // Unit movement speed on Game grid

    public int AttackPrice;       // Price of Attack in Action Points 
    public int Resource;          // Local Resource (for Fractions with local resources)

    public int ActionMode;        // Action Mode - determines possible Cells and Targets
    public int ActionAtMoveEnd;   // Determine if action at end Turn is needed

    public bool IsImmune;         // True if Unit has immunity on some Attacks
    public bool isTarget;         // True if Unit is Target of current action
    public bool isMoving;         // True if Unit is currently moving
    public bool IsImmobile;       // True if movement is disabled for Unit
    // for now removed [public bool hasCombineAttack; // True if Unit is both melee and ranged] 
    public bool counterAction;    // True if attack (action) on Unit triggers counteraction
    public bool headingRight;     // True if Unit is heading right
    public bool IsPlaced;         // Mark Unit as placed within Unit placement part
    public bool PlayedInTurn;     // Check Unit has already played in current Turn // TEST

    // Attack List
    public List<AttackStruc> attacks = new List<AttackStruc>();
    public AttackStruc attack = new AttackStruc();

    // Skills
    public List<Skills> UnitSkills; // List of Unit Skills
    public List<string> skills;     // List of Unit Skills (for in-game instantiation)
    public string skillPicked;      // Skill marked as default (for Units with more choices)

    // Game Relations
    public Cell Cell;               // Cell where Unit currenly stand
    public Vector2 CoordsUnit;      // Current coordinates (Vector2 from 1,1) of Unit
    private Cell attackFrom;        // Cell from where melee attack is executed

    public List<Impact> impacts;    // List of all Impacts on current Unit 
    public Impacts unitImpacts;     // Class for processing Unit Impacts

    public List<Effects> EffectsOwned; // All Effects triggered by current Unit

    public List<NewUnit> unitAllowed;  // Allowed Targets (suppress standard determination)

    // UI
    public List<Arrow> Directions;      // 'Arrow' colliders triggering Melee Attack
    public Text damToDo;                // UI Text to show potential Damage value // TEST
    public Image damIcon;               // UI Image to show potential Damage icon // TEST

    // References to Main Game classes
    public ControllerGame controller;     // Game Controller
    public ControllerGameMP controllerMP; // Game Controller MP
    public CellGrid cellGrid;             // Game Grid
    public CellGridMP cellGridMP;         // Game Grid MP
    public CombatLog combatLog;           // Game Log

    // TO BE REMOVED
    public string skillMessage; 
    public bool multiPlayer;

    //------------------------------------------
    // EVENTS

    public event EventHandler UnitDown;    // triggered when Unit is clicked
    public event EventHandler UnitEnter;   // triggered when Unit is highlighted
    public event EventHandler UnitExit;    // triggered when Unit is de-highlighted

    //------------------------------------------
    // METHODS - MOUSE ACTIONS

    //------------------------------------------
    // OnMouseDown - Unit

    protected void OnMouseDown()
    {
        if (isTarget == true) // Must be target of currently selected action
        {
            UnitDown.Invoke(this, new EventArgs());
        }
    }

    //------------------------------------------
    // OnMousEnter - Unit

    protected void OnMouseEnter() 
    {
        UnitEnter.Invoke(this, new EventArgs()); // Get & show Unit data
    }

    //------------------------------------------
    // OnMouseExit - Unit

    protected void OnMouseExit()
    {
        UnitExit.Invoke(this, new EventArgs()); // Hide Unit data (switch to Cell/Team data)
    }

    //------------------------------------------
    // OnMousEnter - Attack Arrow

    public void ShowPathImg(string direction)
    {
        var dest = CoordsUnit;

        switch (direction)
        {
            case "Up":
                dest += new Vector2(0, 1);
                break;
            case "Right":
                dest += new Vector2(1, 0);
                break;
            case "Left":
                dest += new Vector2(-1, 0);
                break;
            case "Down":
                dest += new Vector2(0, -1);
                break;
        }

        // Mark path to Cell from where possible Attack will be executed
        attackFrom = cellGrid.Cells.FindLast(n => n.CellCoords == dest);
        cellGrid.MarkPathCurr(attackFrom);     
    }

    //------------------------------------------
    // OnMousExit - Attack Arrow

    public void RemovePath()
    {
        RemovePathSP();     
    }

    // Attack Arrow leave - singleplayer
    public void RemovePathSP()
    {
        cellGrid.CellsPossible = cellGrid.Cells.FindAll(n => n.IsHighlightable == true);
        foreach (var cell in cellGrid.CellsPossible)
        {
            cell.MarkAsReachable();
        }
    }

    //// Attack Arrow leave - multiplayer // to be handled in own MP class
    //public void RemovePathMP()
    //{
    //    cellGridMP.CellsPossible = cellGridMP.Cells.FindAll(n => n.IsHighlightable == true);
    //    foreach (var cell in cellGridMP.CellsPossible)
    //    {
    //        cell.MarkAsReachable();
    //    }
    //}

    //------------------------------------------
    // OnMouseDown - Attack Arrow

    public void MoveAndAttack()
    {  
        // Process movement to 'attackFrom' Cell & Attack
        if (cellGrid.Path.Count > 0)
        {
            controller.targetUnit = this;
            cellGrid.Move(attackFrom, cellGrid.Path, controller.PlayingUnit);

        } else
        {
            controller.ProcessAttack(controller.PlayingUnit, this); // only Attack
        }
       
        // Deactivate 'Attack Arrows'
        for (int i = 0; i < 4; i++)
        {
            Directions[i].gameObject.SetActive(false);
        }
    }

    //------------------------------------------
    // METHODS - GAME ACTIONS
    
    //------------------------------------------
    // Game Flow mechanics

    // Method for processing action at the end of each move

    public virtual void UpdateAtEndMove()
    {
        // never used base call
    }

    // Method for update Unit at start of each Turn (base)

    public virtual void UpdateAtStartTurn()
    {
        ActionsPointsAct = ActionPoints; // Restore Action Points
        unitImpacts.ProcessImpacts();    // Process Impacts
    }

    // Method for update Unit at end of each Turn (base)

    public virtual void UpdateAtEndTurn()
    {
        PlayedInTurn = true;           // Mark Unit with 'played in Turn' indicator
        unitImpacts.RemoveOnEndTurn(); // Remove expired Impacts with 'OnEndTurn' processing
        ActionMode = 0;                // Reset mode to standard

        // Deactivate Skills (only persistent Skills remains activated)
        foreach (var skill in UnitSkills)
        {
            if (skill.IsPassive == false && skill.IsPersistent == false)
            {
                skill.IsActivated = false;
            }
        }

        unitAllowed.Clear(); // Clear Target restrictions   // TEST here or Controller!
        IsImmobile = false;  // Clear movement restrictions // TEST here or Controller!

        // Clear Attack Directions and reset cursor
        foreach (var unit in controller.UnitsAll)
        {
            foreach (var button in unit.Directions)
            {
                button.gameObject.tag = "NotAccess";
            }
        }

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // ---------------------------------------------------------
    // Game Flow mechanics - Death processing

    // Quick check Unit is still alive

    public void IsAliveCheck(bool onTurn)
    {
        if (HealthAct <= 0)
        {
            UpdateAfterDeath(onTurn);
        }
    }

    // Method for update Game when Unit dies

    public virtual void UpdateAfterDeath(bool OnTurn)
    {
        // Handle Impacts
        foreach (var impact in impacts.FindAll(n => n.OnDeathAction == true))
        {
            impact.trigger.TargetDied(this);
        }

        // Handle Effects 
        if (EffectsOwned != null)
        {
            var nonPersistent = EffectsOwned.FindAll(n => n.isPersistent == false);
            for (int i = 0; i < nonPersistent.Count; i++)
            {
                nonPersistent[i].cell.CellEffects.Remove(nonPersistent[i]);

                var unit = nonPersistent[i].cell.unitCurr;
                if (unit != null)
                {
                    unit.cellGrid.ApplyCellEffects(unit, 0); // remove Effect
                }
            }
        }

        // Handle Game Flow
        combatLog.LogDeath(UnitName + " is dead."); // Log Death
        Cell.IsTaken = false;                       // Cell not Taken any more
        controller.UnitsAll.Remove(this);           // Remove Unit form list of all Units
        if (PlayerNumber == 0)                      // Remove Unit form list of current Team
        {
            controller.UnitsPlayer0.Remove(this);
        } else
        {
            controller.UnitsPlayer1.Remove(this);
        }

        // Destroy entire game object (or just send transform away?)
        Destroy(this.gameObject);

        if (OnTurn == true) // If Unit was on Turn, shift to next
        {
            controller.ShiftToNext();
        }
    }

    // Method for processing special action when Target of Impact died

    public virtual void TargetDied(NewUnit unit)
    {
        // to be done
    }

    //------------------------------------------
    // Attack processing

    // Basic method for Attack processing (FALSE: simulate & show, TRUE: real damage & log)

    public virtual void Attack(NewUnit attacker, NewUnit defender, bool doDamage)
    {
        int damage = 0;

        // Calculate basic Damage
        if (defender.IsImmune != true)
        {
            float coeficient = CalculateAttack(attacker, defender);
            damage = Mathf.RoundToInt(coeficient * attacker.Damage);

        } else
        {
            damage = defender.CalculateReduced(attacker, defender, false);
        }

        // Check if Attack is rear Attack
        if (attacker.RangeTo == 1)
        {
            if (RearAttack(attacker, defender) == true)
            {
                Mathf.RoundToInt(damage * 1.3f); // TEST VALUE
            }
        }

        if (doDamage == true) 
        {
            // Substract Health (defender)
            defender.HealthAct -= damage;
            combatLog.LogAttack(attacker.UnitName + " caused  " + damage + " damage to " + defender.UnitName + "."); // Log Attack

            // Substract Action Points (attacker)
            attacker.ActionsPointsAct -= attacker.AttackPrice;

            // Counteraction (defender) 
            if (defender.counterAction == true)
            {
                defender.Counteraction(defender);
            }

            // Is alive check (defender)
            defender.IsAliveCheck(false);
        }
        else
        {
            // Show Damage done if Unit entered
            defender.ShowDamage(damage, CursorSetter.melee_spr);
        }
    }

    // Method for calculation of coeficient for final Damage (standard Attack)

    public float CalculateAttack(NewUnit attacker, NewUnit defender)
    {
        float koef = 0; // initial coeficient

        attack.attackMark = "PhysicalDefence";
        attack.attackValue = attacker.PhysicalAttack;
        attacks.Add(attack);
        attack.attackMark = "MagicalDefence";
        attack.attackValue = attacker.MagicalAttack;
        attacks.Add(attack);
        attack.attackMark = "SpecialDefence";
        attack.attackValue = attacker.SpecialAttack;
        attacks.Add(attack);

        attacks = attacks.OrderByDescending(n => n.attackValue).ToList();

        if (attacks[0].attackValue > 0) // Primary Attack type
        {
            int defenceValue = (int)defender.GetType().GetField(attacks[0].attackMark).GetValue(defender);
            koef = koef + (attacks[0].attackValue - defenceValue);
        }

        if (attacks[1].attackValue > 0) // Secondary Attack type
        {
            int defenceValue = (int)defender.GetType().GetField(attacks[1].attackMark).GetValue(defender);
            koef = koef + ((attacks[1].attackValue - defenceValue) / 2);
        }

        koef = 1 + (koef / 100);

        attacks.Clear();
        return koef;
    }

    //------------------------------------------
    // Attack - processing exceptions

    // Method for damage calculation if Target is immune (always run as override)

    public virtual int CalculateReduced(NewUnit attacker, NewUnit defender, bool handle)
    {
        var reduced = 0; // not used!
        return reduced;
    }

    // Method to check if Attack is rear Attack

    public bool RearAttack(NewUnit attacker, NewUnit defender)
    {
        var rear = false;

        if (defender.headingRight) // check if defender on [1,0] Coords
        {
            if (defender.CoordsUnit.x - attacker.CoordsUnit.x == -1 && defender.CoordsUnit.y - attacker.CoordsUnit.y == 0)
            {
                rear = true;
            }
        }
        else // check if defender on [-1,0] Coords
        {
            if (defender.CoordsUnit.x - attacker.CoordsUnit.x == 1 && defender.CoordsUnit.y - attacker.CoordsUnit.y == 0)
            {
                rear = true;
            }
        }
        return rear;
    }

    // Method for processing counteraction (always run as override)

    public virtual void Counteraction(NewUnit trigger)
    {
        throw new NotImplementedException("CHECK WHY"); // TEST
    }

    // Method for showing Damage on highlighted Unit

    public void ShowDamage(int damage, Sprite image)
    {
        // Show Damage
        damToDo.gameObject.SetActive(true);
        damToDo.text = damage.ToString();

        // Show Damge Type Icon
        damIcon.gameObject.SetActive(true);
        damIcon.sprite = image;

        controller.damageList.Add(this);

    }

    //------------------------------------------
    // Skills processing (all overrides)

    public virtual void ActivateSkill1() {} // Activate Skill 1, if exists
    public virtual void ActivateSkill2() {} // Activate Skill 2, if exists
    public virtual void ActivateSkill3() {} // Activate Skill 3, if exists

    // Skill execution handled separately for each Unit

    public virtual void ExecuteSkill(NewUnit receiver, Cell targetCell = null) {}

    // Skill selection handled separately for each Unit, update same for all Units

    public virtual void ExecuteSelection(string selection) 
    {
        // Call updates
        cellGrid.GetPathsAll(this);                                // Refresh paths
        controller.GetPossibleTargets(this);                       // Refresh targets
        controller.UpdateSkillBar(this, 0, "", this.skillMessage); // Update Skill Bar
    }

    // Method for processing special action when Target Unit Impact expired

    public virtual void TargetImpactExpired(NewUnit unit)
    {
        // not used here
    }

    // Method for direct applying of Effect (always run as override)

    public virtual void DirectEffectApply(NewUnit unit) { }

    //------------------------------------------

    // Method for 'draw' Attack (USED FOR TESTS)

    public void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0.1f)
    {
        GameObject myLine = new GameObject();
        myLine.transform.position = start;
        myLine.AddComponent<LineRenderer>();
        LineRenderer line = myLine.GetComponent<LineRenderer>();
        //line.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
        line.startColor = color;
        line.endColor = color;
        line.startWidth = 0.2f;
        line.endWidth = 0.2f;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        GameObject.Destroy(myLine, duration);
    }
}

