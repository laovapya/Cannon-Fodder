using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class AIdirector : MonoBehaviour
{

    // [field: SerializeField] public int desiredBlockerCount { get; private set; } = 11;
    // [field: SerializeField] public int desiredDefenderCount { get; private set; } = 10;
    // [field: SerializeField] public int desiredReclaimerCount { get; private set; } = 4;
    // [field: SerializeField] public int desiredTurningCount { get; private set; } = 4;
    [field: SerializeField] public int harasserHiveCount { get; private set; }
    [field: SerializeField] public int blockerHiveCount { get; private set; }
    [field: SerializeField] public int defenderHiveCount { get; private set; }
    [field: SerializeField] public int reclaimerHiveCount { get; private set; }
    [field: SerializeField] public int turningHiveCount { get; private set; }
    public static AIdirector instance;
    GameState state;
    Graph worldGraph;
    enum strategy
    {
        attack,
        block,
        defend,
        reclaim
    }
    private void Awake()
    {
        instance = this;


    }


    void Start()
    {
        state = new GameState();
        worldGraph = MapGenerator.instance.worldGraph;

        desiredRatios = new Dictionary<strategies, float>
    {
        { strategies.attack, attackRatio },
        { strategies.block, blockRatio },
        { strategies.defend, defendRatio },
        { strategies.reclaim, reclaimRatio },
        { strategies.turn, turnRatio}
    };
    }
    [SerializeField] private float boostAttack = 5f;
    [SerializeField] private float boostBlock = 5f;
    [SerializeField] private float boostDefend = 5f;
    [SerializeField] private float boostReclaim = 5f;
    [SerializeField] private float boostTurn = 5f;
    [SerializeField] private float boostNotAttack = 5f; // multiplier applied to all non-attack actions

    private enum strategies
    {
        attack,
        block,
        defend,
        reclaim,
        turn
    }
    [Header("Base strategy distribution")]
    [SerializeField] private float attackRatio = 0.3077f;
    [SerializeField] private float blockRatio = 0.2308f;
    [SerializeField] private float defendRatio = 0.2692f;
    [SerializeField] private float reclaimRatio = 0.1154f;
    [SerializeField] private float turnRatio = 0.0769f;

    private Dictionary<strategies, float> desiredRatios;

    public void SpendBudget(float budget, bool isTelegraphing = true)
    {
        state.TakeSnapshot();

        int totalBudget = Mathf.FloorToInt(budget);
        if (totalBudget <= 0)
            return;

        float hiveBudgetSum = harasserHiveCount + blockerHiveCount + defenderHiveCount + reclaimerHiveCount + turningHiveCount + budget;
        float[] currentRatios = { harasserHiveCount / hiveBudgetSum, blockerHiveCount / hiveBudgetSum, defenderHiveCount / hiveBudgetSum, reclaimerHiveCount / hiveBudgetSum, turningHiveCount / hiveBudgetSum };
        float[] resultRatios = { desiredRatios[strategies.attack], desiredRatios[strategies.block], desiredRatios[strategies.defend], desiredRatios[strategies.reclaim], desiredRatios[strategies.turn] };
        float freeSpace = budget / hiveBudgetSum;
        float[] baseRatios = FreeSpaceDistributor.DistributeFreeSpace(currentRatios, freeSpace, resultRatios);
        var ratios = new Dictionary<System.Action<bool>, float>
    {
        { Attack, baseRatios[0] },
        { BlockFrontline, baseRatios[1] },
        { Defend, baseRatios[2] },
        { Reclaim,baseRatios[3] },
        { Turn, baseRatios[4] }
    };
        // if (!isTelegraphing) //remove, add conditions on wave number for some buildings. 
        //     ratios[Attack] = 3f;

        //==========================================================
        //float panicNotAttack, panicBlock, panicDefend, panicReclaim, panicTurn, panicAttack;
        //ComputeRawUtilities(out panicAttack, out panicNotAttack, out panicBlock, out panicDefend, out panicReclaim, out panicTurn);


        // var panics = new Dictionary<System.Action<bool>, float>
        //     {
        //         { Attack, panicNotAttack },
        //         { BlockFrontline, panicBlock },
        //         { Defend, panicDefend },
        //         { Reclaim, panicReclaim },
        //         { Turn, panicTurn }
        //     };

        // var actionBoosts = new Dictionary<System.Action<bool>, float>
        // {
        //     { Attack, 0 },
        //     { BlockFrontline, boostBlock },
        //     { Defend, boostDefend },
        //     { Reclaim, boostReclaim },
        //     { Turn, boostTurn }
        // };

        // foreach (var key in ratios.Keys.ToList())
        // {
        //     if (key == Attack)
        //     {
        //         ratios[key] *= 1f + boostAttack * (panicAttack);
        //     }
        //     else
        //     {
        //         ratios[key] *= (1f + actionBoosts[key] * panics[key]) * (1f + boostNotAttack * panicNotAttack);
        //     }
        // }
        //====================================================================
        foreach (float ratio in ratios.Values)
        {
            Debug.Log("panicked ratios: " + ratio);
        }
        Debug.Log("===================================");
        float ratioSum = ratios.Values.Sum();
        if (ratioSum <= 0f)
            return;

        var allocations = new Dictionary<System.Action<bool>, int>();
        int remaining = totalBudget;


        foreach (var kv in ratios)
        {
            float exact = totalBudget * (kv.Value / ratioSum);
            int count = Mathf.RoundToInt(exact);
            allocations[kv.Key] = count;
            remaining -= count;
        }


        if (remaining > 0)
        {
            allocations[Attack] += remaining;
            remaining = 0;
        }


        foreach (var kv in allocations)
        {
            for (int i = 0; i < kv.Value; i++)
            {
                kv.Key(isTelegraphing);
            }
        }
    }

    [Header("Strategy Weights")]
    [SerializeField] private float defendMainhiveW = 2;
    [SerializeField] private float defendWeightFewSoldiers = 0.3f;
    [SerializeField] private float defendWeightNeedTerritory = 0.5f;
    [SerializeField] private float defendWeightHiveRemoteness = 0.2f;

    [SerializeField] private float defendW = 2;
    [SerializeField] private float defendWeightMainHiveDanger = 0.4f;
    [SerializeField] private float defendWeightMainHiveDefense = 0.4f;
    [SerializeField] private float defendWeightNeedContestMainhive = 0.2f;

    [SerializeField] private float attackW = 2;
    [SerializeField] private float attackBase = 0.3f;
    [SerializeField] private float attackWeightPerimeterBlocked = 0.4f;
    [SerializeField] private float attackWeightPlayerUndefended = 0.35f;
    [SerializeField] private float attackWeightFewSoldiers = 0.25f;

    [SerializeField] private float blockW = 2;
    [SerializeField] private float blockWeightPlayerDefended = 0.6f;
    [SerializeField] private float blockWeightPerimeterBlocked = 0.4f;

    [SerializeField] private float reclaimW = 2;
    [SerializeField] private float reclaimWeightNeedContestMainhive = 0.5f;
    [SerializeField] private float reclaimWeightNeedSurround = 0.3f;
    [SerializeField] private float reclaimWeightNeedTerritory = 0.2f;

    [SerializeField] private float turnW = 2;
    [SerializeField] private float turnWeightManySoldiers = 1f;

    private void ComputeRawUtilities(
        out float rawAttack,
    out float rawNotAttack,
    out float rawBlock,
    out float rawDefend,
    out float rawReclaim,
    out float rawTurn)
    {
        float needTerritory = 1f - state.enemyTerritoryPercent;
        float needSurround = 1f - state.surroundCoefficient;

        float manySoldiers = state.soldierFulfilment;
        float fewSoldiers = 1f - manySoldiers;

        float playerDefended = state.playerDefenseCoefficient;
        float playerUndefended = 1f - playerDefended;

        float needContestMainhive =
            1f - Mathf.Clamp01(state.mainHiveExpansion / state.citadelMainhiveDistance);
        float mainHiveDefense = state.mainhiveDefense / GameState.maxMainhiveDefense;
        float mainHiveDanger = state.mainhiveDanger / state.maxCellDanger;

        int blockerHivePerimeterMargin = 3;
        float perimeterFree = blockerHiveCount <= 0
            ? 1
            : Mathf.Clamp01((float)state.frontlinePerimeter / (blockerHiveCount * blockerHivePerimeterMargin));
        //Debug.Log("perimeter blocked " + perimeterFree);

        rawAttack =
            Mathf.Clamp01(attackBase +
                attackWeightPlayerUndefended * playerUndefended -
                attackWeightPerimeterBlocked * perimeterFree +
                attackWeightFewSoldiers * fewSoldiers);

        rawNotAttack = Mathf.Pow(1 - rawAttack, 3);

        rawBlock = Mathf.Clamp01(
            blockWeightPlayerDefended * playerDefended +
            blockWeightPerimeterBlocked * perimeterFree
        );

        rawBlock *= rawBlock;

        float defendAnywhere =
            defendWeightFewSoldiers * fewSoldiers +
            defendWeightNeedTerritory * needTerritory +
            defendWeightHiveRemoteness * state.hiveRemoteness;

        defendAnywhere = Mathf.Max(0f, defendAnywhere);
        defendAnywhere *= defendAnywhere;

        float defendMainHive =
            defendWeightMainHiveDanger * mainHiveDanger -
            defendWeightMainHiveDefense * mainHiveDefense +
            defendWeightNeedContestMainhive * needContestMainhive;

        defendMainHive = Mathf.Sqrt(Mathf.Clamp01(defendMainHive));

        rawDefend = Mathf.Max(defendAnywhere, defendMainHive);

        rawReclaim =
            reclaimWeightNeedContestMainhive * needContestMainhive +
            reclaimWeightNeedSurround * needSurround +
            reclaimWeightNeedTerritory * needTerritory;

        rawTurn = Mathf.Sqrt(Mathf.Clamp01(state.soldierFulfilment));
    }





    private Building PickAndBuild(
        Func<int, int, float?> utilityFunc,
        float temperature,
        BuildGrid.type buildType, bool onAnyTerritory = false)
    {
        var candidates = new List<(Vector2Int pos, float utility)>();

        worldGraph.ForEachPlayableCell((int x, int y) =>
        {
            if ((TerritoryGrid.instance.grid.GetValue(x, y) != TerritoryGrid.type.enemy && !onAnyTerritory)
      || BuildGrid.instance.GetIfBusy(x, y))
                return;

            float? u = utilityFunc(x, y);
            if (u == null) return;

            candidates.Add((new Vector2Int(x, y), u.Value));
        });

        if (candidates.Count == 0) return null;

        Vector2Int chosen = WeightedPick(candidates, temperature);
        // if (buildType == BuildGrid.type.harasserHive || buildType == BuildGrid.type.harasserHiveTelegraphed)
        //   Debug.Log("AI chose to build harasser hive at " + chosen + " with utility " + candidates.Find(c => c.pos == chosen).utility + " with frontline distance " + state.frontlineDistances[chosen.x, chosen.y]);
        var pos = BuildGrid.instance.grid.GetWorldPosition(chosen.x, chosen.y);
        Building b = BuildGrid.instance.TryBuild(buildType, pos);
        state.UpdateOnBuild();
        return b;
    }
    [Header("Attack")]
    [SerializeField] private float attack_temperature = 0.01f;
    [SerializeField] private float attack_wDanger = 1f;
    [SerializeField] private float attack_wBuildingPriority = 0.3f;
    //[SerializeField] private float attack_wHealth = 0.1f;
    //[SerializeField] private float attack_wPathLength = 2f;
    [SerializeField] private float attack_wFrontlineDistance = 2f;
    [SerializeField] private float attack_wPresence = 1.5f;

    private void Attack(bool isTelegraphing)
    {
        BuildGrid.type type = isTelegraphing ? BuildGrid.type.harasserHiveTelegraphed : BuildGrid.type.harasserHive;
        harasserHiveCount++;
        Building b = PickAndBuild((x, y) =>
        {

            int tx = state.cellTarget[x, y].x;
            int ty = state.cellTarget[x, y].y;
            if (tx == -1 || ty == -1) return 0;

            Building cellTarget = BuildGrid.instance.grid.GetValue(tx, ty);
            if (cellTarget == null) return 0;


            float normPathDanger = Mathf.Clamp01(state.pathDanger[x, y] / 300);

            if (state.pathZombiePresence[x, y] > 8)
                normPathDanger /= 10;

            float normBuilding = GetBuildingPriority(cellTarget) / citadelPriority;
            //float normHealth = 1f - cellTarget.health / cellTarget.maxHealth;
            float normHivePresence = Mathf.Clamp01(state.hivePresence[x, y] / state.maxHivePresence);

            //float normPathLength = state.pathLength[x, y] / state.citadelMainhiveDistance;
            //Debug.Log("Frontline distance: " + state.frontlineDistances[x, y] + " / " + state.citadelMainhiveDistance);
            float normFrontline = CurvedMid(state.frontlineDistances[x, y] /
                                            state.citadelMainhiveDistance, 5);

            float utility =
                -attack_wDanger * normPathDanger +
                attack_wBuildingPriority * normBuilding +
                //attack_wHealth * normHealth +
                //attack_wPathLength * normPathLength +
                attack_wFrontlineDistance * normFrontline -
                attack_wPresence * normHivePresence;

            return utility;

        }, attack_temperature, type);
        if (b is TelegraphingBuilding t)
        {
            t.onFinishedBuildingDie += () => harasserHiveCount--;
        }
        else
            b.onDie += (int x) => harasserHiveCount--;
    }
    [Header("Block")]
    [SerializeField] private float block_temperature = 0.01f;
    [SerializeField] private float block_wDanger = 1f;
    [SerializeField] private float block_wFrontline = 2f;
    [SerializeField] private float block_wPresence = 0.7f;
    private void BlockFrontline(bool isTelegraphing)
    {
        BuildGrid.type type = isTelegraphing ? BuildGrid.type.buildupHiveTelegraphed : BuildGrid.type.buildupHive;
        blockerHiveCount++;
        Building b = PickAndBuild((x, y) =>
        {
            float normDanger = state.cellDanger[x, y] / state.maxCellDanger;
            float normFrontlineDistance = Mathf.Min(state.frontlineDistances[x, y],
                                            state.neutralFrontlineDistances[x, y])
                                            / state.citadelMainhiveDistance;

            float normPresence = Mathf.Clamp01(state.hivePresence[x, y] / state.maxHivePresence);
            normPresence = CurvedMid(normPresence, 6);

            float utility =
                -block_wDanger * normDanger
                - block_wFrontline * normFrontlineDistance
                - block_wPresence * normPresence;

            return utility;

        }, block_temperature, type);
        if (b is TelegraphingBuilding t)
        {
            t.onFinishedBuildingDie += () => blockerHiveCount--;
        }
        else
            b.onDie += (int x) => blockerHiveCount--;
    }
    [Header("Defend")]
    [SerializeField] private float defend_temperature = 0.01f;

    [SerializeField] private float defend_wMain = 1f;
    //[SerializeField] private float defend_wReclaimer = 1f;
    [SerializeField] private float defend_wResource = 1f;
    [SerializeField] private float defend_wLowPresence = 1f;

    private void Defend(bool isTelegraphing)
    {
        BuildGrid.type type = isTelegraphing ? BuildGrid.type.buildupHiveTelegraphed : BuildGrid.type.buildupHive;
        defenderHiveCount++;
        Building b = PickAndBuild((x, y) =>
        {
            //float nearReclaimer = state.isNearReclaimer[x, y] ? 1 : 0;
            float defendRuins = state.isNearResource[x, y] ? 1 : 0;



            float distToMain = Vector2.Distance(MapGenerator.instance.mainHivePos, BuildGrid.instance.grid.GetCellCenter(x, y));
            //float distanceToMainHive = Mathf.Clamp01(1f - distToMain / state.citadelMainhiveDistance);
            float mainHiveDefenseFulfilment = 1 - Mathf.Clamp01(state.mainhiveDefense / GameState.maxMainhiveDefense);
            float defendMain = Mathf.Sqrt((1 - Mathf.Clamp01(distToMain / state.citadelMainhiveDistance))) * mainHiveDefenseFulfilment; //(distToMain < GameState.mainHiveDefenseRadius * MapGenerator.instance.cellSize ? 1 : 0)




            float lowPresence = 1f - Mathf.Clamp01(state.hivePresence[x, y] / state.maxHivePresence);

            float utility =
                defend_wMain * defendMain +
                //defend_wReclaimer * nearReclaimer +
                defend_wResource * defendRuins +
                defend_wLowPresence * lowPresence;


            return utility;

        }, defend_temperature, type);
        if (b is TelegraphingBuilding t)
        {
            t.onFinishedBuildingDie += () => defenderHiveCount--;
        }
        else
            b.onDie += (int x) => defenderHiveCount--;
    }
    [Header("Reclaim")]
    [SerializeField] private float reclaim_temperature = 0.01f;

    [SerializeField] private float reclaim_wMain = 2f;
    [SerializeField] private float reclaim_wPresence = .5f;
    //[SerializeField] private float reclaim_wPathlength = 1f;
    [SerializeField] private float reclaim_wPathDanger = 1;
    [SerializeField] private float reclaim_wSafety = .3f;
    [SerializeField] private float reclaim_wResourceCitadelDistance = .2f;
    [SerializeField] private float reclaim_wEnclosure = 0.1f;

    private void Reclaim(bool isTelegraphing)
    {
        reclaimerHiveCount++;
        BuildGrid.type type = isTelegraphing ? BuildGrid.type.reclaimerHiveTelegraphed : BuildGrid.type.reclaimerHive;
        Building b = PickAndBuild((x, y) =>
        {
            if (Mathf.Min(state.neutralFrontlineDistances[x, y], state.frontlineDistances[x, y]) > 20f || state.isNearReclaimer[x, y])
                return 0f;

            float distToMain = Vector2.Distance(
    MapGenerator.instance.mainHivePos,
    BuildGrid.instance.grid.GetCellCenter(x, y));
            float distanceToMainHive = Mathf.Clamp01(1f - distToMain / state.citadelMainhiveDistance);

            float highPresence = Mathf.Clamp01(state.hivePresence[x, y] / state.maxHivePresence);
            float cellDanger = state.cellDanger[x, y] / state.maxCellDanger;


            float nearbyDangerNorm = state.nearbyDanger[x, y] / GameState.maxNearbyDanger;
            float resourceCitadelDistance = 1f - Mathf.Clamp01(state.ResourceCitadelCostMap[x, y] / state.citadelMainhiveDistance);
            float enclosure = Mathf.Clamp01(1f - 2f * Mathf.Abs(state.enclosureMap[x, y] - 0.4f));

            float utility =
                reclaim_wMain * distanceToMainHive +
                reclaim_wPresence * highPresence +
                reclaim_wPathDanger * nearbyDangerNorm +
                reclaim_wSafety * cellDanger +
                reclaim_wResourceCitadelDistance * resourceCitadelDistance +
                reclaim_wEnclosure * enclosure;

            return utility;

        }, reclaim_temperature, type, true);
        if (b is TelegraphingBuilding t)
        {
            t.onFinishedBuildingDie += () => reclaimerHiveCount--;
        }
        else
            b.onDie += (int x) => reclaimerHiveCount--;
    }

    [Header("Turn")]
    [SerializeField] private float turn_temperature = 0.05f;
    [SerializeField] private float turn_wSafety = 8;
    [SerializeField] private float turn_wEnclosure = 6;
    [SerializeField] private float turn_wFrontlineDistance = 2;

    private void Turn(bool isTelegraphing)
    {
        turningHiveCount++;
        BuildGrid.type type = isTelegraphing ? BuildGrid.type.turningHiveTelegraphed : BuildGrid.type.turningHive;
        Building b = PickAndBuild((x, y) =>
        {
            float cellSafety = 1f - state.cellDanger[x, y] / state.maxCellDanger;
            float enclosure = Mathf.Clamp01(1f - 2f * Mathf.Abs(state.enclosureMap[x, y] - 0.4f));
            float normFrontlineDistance = Mathf.Min(state.frontlineDistances[x, y],
                                            state.neutralFrontlineDistances[x, y])
                                            / state.citadelMainhiveDistance;
            float utility = turn_wEnclosure * enclosure + turn_wSafety * cellSafety - turn_wFrontlineDistance * normFrontlineDistance;

            return utility;

        }, turn_temperature, type);
        if (b is TelegraphingBuilding t)
        {
            t.onFinishedBuildingDie += () => turningHiveCount--;
        }
        else
            b.onDie += (int x) => turningHiveCount--;
    }
    float CurvedMid(float v, float sharpness = 4f)
    {
        v = Mathf.Clamp01(v);
        return Mathf.Max(0f, 1f - sharpness * (v - 0.5f) * (v - 0.5f));
    }


    private static float SoftmaxWeight(float utility, float temperature)
    {
        return Mathf.Exp(utility / temperature);
    }

    private static Vector2Int WeightedPick(List<(Vector2Int pos, float utility)> items, float temperature)
    {//high temperature => more randomness
        float total = 0f;
        float[] weights = new float[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            weights[i] = SoftmaxWeight(items[i].utility, temperature);
            total += weights[i];
        }

        float r = UnityEngine.Random.value * total;
        float sum = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            sum += weights[i];
            if (sum >= r)
                return items[i].pos;
        }
        return items[items.Count - 1].pos;
    }
    private float citadelPriority = 100;
    float GetBuildingPriority(Building building)
    {
        if (building == null) return 0;
        BuildGrid.type type = building.type;
        switch (type)
        {
            default: return 0;

            case BuildGrid.type.citadel:
                return citadelPriority;
            case BuildGrid.type.turret:
                return 20;
            case BuildGrid.type.artillery:
                return 30;
            case BuildGrid.type.powerplant:
                return 50;
            case BuildGrid.type.terraformer:
                return 40;
        }
    }
}

public class FreeSpaceDistributor
{
    public static float[] DistributeFreeSpace(float[] currentRatios, float freeSpace, float[] desiredRatios, int maxIter = 1000, float tol = 1e-6f, float step = 0.1f)
    {
        if (currentRatios.Length != 5 || desiredRatios.Length != 5)
            throw new ArgumentException("Arrays must have length 5.");

        float[] x = new float[5]; // initial distribution: start with equal split
        for (int i = 0; i < 5; i++) x[i] = freeSpace / 5f;

        for (int iter = 0; iter < maxIter; iter++)
        {
            // compute gradient of loss w.r.t x_i: grad_i = 2 * ((r_i + x_i) - d_i)
            float[] grad = new float[5];
            for (int i = 0; i < 5; i++)
                grad[i] = 2f * ((currentRatios[i] + x[i]) - desiredRatios[i]);

            // take a gradient step
            for (int i = 0; i < 5; i++)
                x[i] -= step * grad[i];

            // project to non-negative
            for (int i = 0; i < 5; i++)
                if (x[i] < 0f) x[i] = 0f;

            // project to sum constraint: scale proportionally
            float sumX = 0f;
            for (int i = 0; i < 5; i++) sumX += x[i];
            if (sumX > 0f)
            {
                float scale = freeSpace / sumX;
                for (int i = 0; i < 5; i++)
                    x[i] *= scale;
            }
            else
            {
                // all zero case: distribute equally
                for (int i = 0; i < 5; i++) x[i] = freeSpace / 5f;
            }

            // check convergence
            float maxChange = 0f;
            for (int i = 0; i < 5; i++)
                maxChange = Math.Max(maxChange, Math.Abs(step * grad[i]));
            if (maxChange < tol) break;
        }
        // scale to sum to 1
        for (int i = 0; i < 5; i++)
            x[i] /= freeSpace;
        return x;
    }
}
