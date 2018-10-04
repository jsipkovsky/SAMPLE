using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ----- ARIANNA (CHILDREN OF CELIX) ----


public class Arianna1 : ChildrenOfCelix
{
    //----------------------------------
    // ATTRIBUTES

    private int Modifier;
    List<RaycastHit2D> targets;
    readonly int layerMask = 1 << 0; // mask for Cells (used reverted)

    //----------------------------------
    // METHODS

    //----------------------------------
    // Attack processing

    public override void Attack(NewUnit attacker, NewUnit defender, bool doDamage)
    {
        int damage = 0;

        // Calculate basic Damage
        if (defender.IsImmune != true)
        {
            float coeficient = CalculateAttack(attacker, defender);
            damage = Mathf.RoundToInt(coeficient * attacker.Damage);
        }
        else
        {
            damage = defender.CalculateReduced(attacker, defender, false);
        }

        if (doDamage == true)
        {
            // DrawLine(attacker.transform.position, defender.transform.position, Color.red, 0.5f);

            // Substract Action Points
            attacker.ActionsPointsAct -= attacker.AttackPrice;

            // Substract Health (defender)
            defender.HealthAct -= damage;
            combatLog.LogAttack(attacker.UnitName + " caused  " + damage + " damage to " + defender.UnitName + "."); // Log Attack

            // Counteraction (defender)
            if (defender.counterAction == true)
            {
                defender.Counteraction(defender);
            }

            // Is alive check (defender)
            defender.IsAliveCheck(false);

        } else
        {
            // Show Damage done if Unit entered
            defender.ShowDamage(damage, CursorSetter.ranged_spr);
        }

        //----------------------------------
        // Divine Arrows

        // Attacker & Defender mutual position
        var playerPos = attacker.transform.localPosition;
        var enemyPos = defender.transform.localPosition;

        // Distance between Units [vector] 
        Vector2 dist_vectors = playerPos - enemyPos;

        // Get all objects in 'shoot' Ray except Cells
        targets = Physics2D.RaycastAll(playerPos, -(dist_vectors), 100.0F, ~layerMask).OrderBy(n => n.distance).ToList();

        if (targets.Count > 1)
        {
            targets.RemoveAt(0); // Attack already processed

            Modifier = 2;            // basic modifier (each target, always divide by 2)
            var position = enemyPos; // position for animation (defender position is default)

            foreach (var target in targets)
            {
                // Check to hit is NewUnit or not (this or Layer mask update)
                if (target.transform.GetComponent<NewUnit>() != null)
                {
                    // Get Target (both friendly and enemy)
                    var targetUnit = target.transform.GetComponent<NewUnit>();
                    PenetrateAttack(attacker, targetUnit, Modifier, doDamage);

                    // Raise modifier
                    Modifier += 1;
                    position = target.transform.position;
                }
                else
                {
                    position = target.transform.position;
                    break; // solid Effect, which is not Unit, stop the shot on it
                }
            }

            Modifier = 0; // default modifier

            if (doDamage == true)
            {
                DrawLine(attacker.transform.position, position, Color.red, 0.5f); // (FOR TESTING)
            }
        }
    }

    // Attack Target on way of shot with decreased Attack

    private void PenetrateAttack(NewUnit attacker, NewUnit target, int modifier, bool doDamage)
    {
        int damage = 0;

        // Calculate basic Damage
        if (target.IsImmune != true)
        {
            float coeficient = CalculateAttack(attacker, target);
            damage = Mathf.RoundToInt(coeficient * attacker.Damage);
        }
        else
        {
            damage = target.CalculateReduced(attacker, target, false);
        }

        damage = damage / modifier; // update Damage

        if (doDamage == true)
        {
            // Substract Health (defender)
            target.HealthAct -= damage;
            combatLog.LogSkill("Divine arrows caused  " + damage + " damage to " + target.UnitName + "."); // Log Divine Arrows

            // Counteraction (defender)
            if (target.counterAction == true)
            {
                Counteraction(target);
            }

            // Is alive check (defender)
            target.IsAliveCheck(false);

        } else
        {
            // Show Damage done if Unit entered
            target.ShowDamage(damage, CursorSetter.ranged_spr);
        }
    }

    //----------------------------------
    // Skills

    // Method for execute 'There is no escape' Skill

    public override void ExecuteSkill(NewUnit receiver, Cell targetCell = null)
    {
        // Calculate basic Damage, ignoring immunity
        float coeficient = CalculateAttack(this, receiver);
        int damage = Mathf.RoundToInt(coeficient * Damage);

        // Substract Action Points (Arianna)
        ActionsPointsAct -= UnitSkills[0].skillPrice;

        // Substract Health (defender)
        receiver.HealthAct -= damage;
        combatLog.LogSkill(" There is no escape caused  " + damage + " damage to " + receiver.UnitName + "."); // Log 'No Escape'

        // Counteraction (defender)
        if (receiver.counterAction == true)
        {
            Counteraction(receiver);
        }

        // Is alive check (defender)
        receiver.IsAliveCheck(false);

        // Fraction Progress
        //CelixProgress(UnitSkills[0].resourcePrice, PlayerNumber);

        ActionMode = 0; // restore Action Mode
        UnitSkills[0].IsActivated = false; 
        UnitSkills[0].isClickable = true;  
    }

    // Method for activate/deactivate 'There is no escape' Skill

    public override void ActivateSkill1()
    {
        if (UnitSkills[0].IsActivated == false)
        {
            UnitSkills[0].IsActivated = true;

            ActionMode = 1; // SKILL-OFFENSIVE
            controller.SkillRange = UnitSkills[0].SkillRange;

        } else
        {
            UnitSkills[0].IsActivated = false;

            ActionMode = 0; // STANDARD
            controller.SkillRange = 0;
        }
    }
}
