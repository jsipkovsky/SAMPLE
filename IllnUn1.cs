using System.Collections.Generic;
using UnityEngine;

// ----- ILLN-UN (CHILDREN OF CELIX) -----


public class IllnUn1 : ChildrenOfCelix
{
    //----------------------------------
    // ATTRIBUTES

    private NewUnit unitUsed; // Catch currenly processing Unit
    private Cell ZoneCell;    // Cell where Death Zone is applied
    private Cell ZoneDest;    // Cell where Unit is pushed by Death Zone
    private bool ZoneAttack;  // Check to log Attack or Death Zone Attack

    //----------------------------------
    // METHODS

    void Start()
    {
        EffectsOwned = new List<Effects>();
    }

    //----------------------------------
    // Attack processing

    public override void Attack(NewUnit attacker, NewUnit defender, bool doDamage)
    {
        int damage = 0;

        // Calculate final Damage
        if (defender.IsImmune != true)
        {
            float coeficient = CalculateAttack(attacker, defender);
            damage = Mathf.RoundToInt(coeficient * attacker.Damage);

        }
        else
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

        // Critical Attack (if Opponent played this turn)
        if (defender.PlayedInTurn == true)
        {
            Mathf.RoundToInt(damage * 1.5f); // TEST VALUE
        }

        if (doDamage == true)
        {
            DrawLine(attacker.transform.position, defender.transform.position, Color.red, 0.5f);

            // Substract Action Points
            attacker.ActionsPointsAct -= attacker.AttackPrice;

            // Substract Health (defender)
            defender.HealthAct -= damage;
            if (ZoneAttack == false)
            {
                combatLog.LogAttack(attacker.UnitName + " caused  " + damage + " damage to " + defender.UnitName + "."); // Log Attack
            } else
            {
                combatLog.LogSkill("Death Zone attack caused  " + damage + " damage to " + defender.UnitName + "."); // Log Death Zone Attack
            }

            // Counteraction (defender)
            if (defender.counterAction == true)
            {
                defender.Counteraction(defender);
            }

            // Is alive check (defender)
            defender.IsAliveCheck(false);

        } else {

            // Show Damage done if Unit entered
            defender.ShowDamage(damage, CursorSetter.melee_spr);
        }
    }

    // Method for processing 'Death Zone' Skill

    public override void UpdateAtEndMove()
    {
        if (ZoneCell != null) // remove Effect from previously occupied Cell
        {
            var Zone = ZoneCell.CellEffects.FindLast(n => n.trigger == this);
            ZoneCell.CellEffects.Remove(Zone);
            EffectsOwned.Remove(Zone);

            ZoneCell.IsMoveStopper = false; 
        }

        if (headingRight == true) // Get new Cell to be marked as 'Zone Cell'
        {
            ZoneCell = cellGrid.Cells.FindLast(n => n.CellCoords == CoordsUnit + new Vector2(1, 0));
        }
        else
        {
            ZoneCell = cellGrid.Cells.FindLast(n => n.CellCoords == CoordsUnit + new Vector2(-1, 0));
        }

        //----------------------------------
        // Prepare Death Zone Effect (probably moved to Start)

        var playerNumber1 = 0;
        if (PlayerNumber == 0)
        {
            playerNumber1 = 1;
        }

        Effects deathZoneCell = new Effects()
        {
            name = "Death Zone",
            trigger = this,
            endTurn = 999,
            targetPlayer = playerNumber1,
            directExecute = true,
            cell = ZoneCell,
            isPersistent = false,
        };

        // Apply on Zone Cell
        ZoneCell.CellEffects.Add(deathZoneCell);
        ZoneCell.IsMoveStopper = true; 
    }

    //----------------------------------
    // Skills

    // Method for executing Death Zone Skill

    public override void DirectEffectApply(NewUnit unit)
    {
        // Execute Death Zone Attack
        ZoneAttack = true;
        Attack(this, unit, true);
        ZoneAttack = false;

        // Shift Target and stop it's move
        var xcoord = 0;
        if (headingRight == true)
        {
            xcoord = 1;
        }
        else
        {
            xcoord = -1;
        }

        // Try to find empty Cell where Unit can be pushed
        ZoneDest = cellGrid.Cells.Find(n =>n.CellCoords.x == CoordsUnit.x +(xcoord * 2) && 
                                           n.CellCoords.y == CoordsUnit.y && n.IsTaken == false);
        if (ZoneDest != null)
        {
            unit.transform.position += new Vector3(xcoord, 0, 0);
            unit.CoordsUnit += new Vector2(xcoord, 0);
        }
        else // if not found, use previous Cell on Path (should always be not taken)
        {
            var cellPos = cellGrid.CellPrev.transform.position; 
            unit.transform.position = new Vector3(cellPos.x, cellPos.y, unit.transform.position.z);
            unit.CoordsUnit = new Vector2(cellGrid.CellPrev.CellCoords.x, cellGrid.CellPrev.CellCoords.y);
        }

        // Shift Unit and end movement
        unit.Cell = cellGrid.Cells.FindLast(n => n.CellCoords == unit.CoordsUnit);
        unit.Cell.unitCurr = unit;
        cellGrid.SubMovesDone = true;
    }
}
