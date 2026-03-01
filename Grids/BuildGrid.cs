using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;
public class BuildGrid : MonoBehaviour
{
    public Grid<Building> grid { private set; get; }
    public int width { get; private set; } = 15;
    public int height { get; private set; } = 15;
    public int cellSize { get; private set; } = 5;
    public Vector2 origin { get; private set; }

    private Building emptyBuilding;

    static public BuildGrid instance;
    public enum type
    {
        empty,
        citadel,
        turret,
        artillery,
        powerplant,
        terraformer,


        mainhive,
        harasserHive,
        buildupHive,
        reclaimerHive,





        barracks,
        decoy,
        barricade,

        ruin,

        harasserHiveTelegraphed,
        buildupHiveTelegraphed,
        reclaimerHiveTelegraphed,

        flamethrower,
        highground,

        turningHive,
        turningHiveTelegraphed,
    }

    public type buildMode = type.empty;

    [SerializeField] public Buildable[] buildings;


    private void Awake()
    {
        instance = this;
        InitGrid();
    }
    private void Start()
    {

        InitBuildings();

        //GameHandler.instance.onFightPhaseStart += CancelBuild;

    }
    private void Update()
    {


        //if (Input.GetKeyDown(KeyCode.Mouse1)) //test--------------------------------------------------------------
        //{
        //    Debug.Log("grid value " + grid.GetValue(mousePos));
        //    Vector2[] path = Pathfinding.GetStraightenedPathVectors(Pathfinding.GetPathNodes(mousePos, Vector2.zero));
        //    Pathfinding.DrawPathGizmos(path, Color.red);
        //}
        //Vector2 mousePos = Util.GetMouseWorldPosition();
        if (Input.GetMouseButtonDown(1))// || GameHandler.instance.playerBudget < Buildable.GetPrice(buildMode))
            CancelBuild();
    }
    private void LateUpdate()
    {
        Vector2 mousePos = Util.GetMouseWorldPosition();

        if (buildMode != type.empty && Input.GetKeyDown(KeyCode.Mouse0) && !EventSystem.current.IsPointerOverGameObject())
        {

            TryBuild(buildMode, mousePos);
            //CancelBuild(); //cancel here if want to do 1 building per click =================
        }
    }
    public Vector2 mainDiag { private set; get; }
    private void InitGrid()
    {
        //grid = Grid<Building>.CreateFrom(MapGenerator.instance.grid);
        grid = MapGenerator.CreateGridBase<Building>();// MapGenerator.CopyGrid<Building>();
        width = grid.width;
        height = grid.height;
        cellSize = (int)grid.cellSize;
        origin = grid.originPosition;
        mainDiag = new Vector2((float)width, (float)height) * cellSize;
        //grid = new Grid<Building>(width, height, cellSize, origin);

    }


    private void InitBuildings()
    {
        if (!Buildable.GetPrefab(type.empty).TryGetComponent<Building>(out emptyBuilding))
            Debug.LogError("emptyBuilding has no Building script");

        for (int i = 0; i < width; ++i)
            for (int j = 0; j < height; ++j)
                grid.SetValue(i, j, emptyBuilding);





        //if (!Instantiate(Buildable.GetPrefab(type.citadel), grid.GetCellCenter(x, y), Quaternion.identity).TryGetComponent<Building>(out Building tower))
        //    Debug.LogError("the prefab doesnt have building script");
        //grid.SetValue(x, y, tower);
    }

    public Action<type, int, int> onBuild;
    public Action onUseAbility;
    public Action onCancelBuild;
    public void CancelBuild()
    {

        buildMode = type.empty;
        onCancelBuild?.Invoke();

    }
    public Building TryBuild(type mode, Vector2 pos)
    {
        type currentMode = buildMode;
        buildMode = mode;
        int x, y;
        grid.GetClampedXY(pos, out x, out y);
        Building b = Build(x, y);
        buildMode = currentMode;
        return b;
    }
    public Building TryBuild(type mode, int x, int y)
    {
        type currentMode = buildMode;
        buildMode = mode;
        Building b = Build(x, y);
        buildMode = currentMode;
        return b;
    }

    private Building Build(int x, int y)
    {
        if (grid.GetValue(x, y) == null || !MapGenerator.instance.IsAreaPlayable(x, y)) //buildings are null outside of grid
            return null;

        Building newBuilding = emptyBuilding;
        Vector2 center = grid.GetCellCenter(x, y);
        CircleCollider2D collider;
        switch (buildMode)
        {
            case type.empty:
                break;
            case type.citadel:
            case type.turret:
            case type.artillery:


            case type.powerplant:
            case type.terraformer:

            case type.barracks:
            case type.barricade:
            case type.decoy:
            case type.flamethrower:

                int price = Buildable.GetPrice(buildMode);
                if (GetIfBusy(x, y) || GameHandler.instance.playerBudget < price || (buildMode == type.powerplant && !MapGenerator.instance.GetIfPowercell(x, y)))
                {
                    SoundManager.PlayFlatSound(SoundManager.Sound.buttonPressError);
                    if (buildMode == type.powerplant && !MapGenerator.instance.GetIfPowercell(x, y))
                        UI.instance.FlashMessageText("Mine can only be built on blue cells");
                    if (GameHandler.instance.playerBudget < price)
                    {
                        UI.instance.FlashCost();
                    }
                    return null;
                }
                GameHandler.instance.AddPlayerBudget(-price);

                if (!Instantiate(Buildable.GetPrefab(buildMode), center, Quaternion.identity).TryGetComponent<Building>(out newBuilding))
                    Debug.LogError("the prefab doesnt have building script");

                //kill units under building 
                float killDistance = 5;
                float stompDistance = killDistance * 2;
                if (buildMode != type.barricade && newBuilding.TryGetComponent<CircleCollider2D>(out collider))
                    foreach (Collider2D c in Physics2D.OverlapCircleAll(center, stompDistance, MaskProcessing.instance.unit))
                    {
                        Unit unit;
                        if (!c.TryGetComponent<Unit>(out unit)) continue;
                        float distance = Vector2.Distance(c.transform.position, center);

                        if (distance < killDistance)
                            unit.Kill();
                        else
                        {
                            Vector3 dir = c.transform.position - (Vector3)center;

                            float t = Mathf.Clamp01(1f - (dir.magnitude / (stompDistance)));

                            unit.mf.AddImpulse(dir.normalized * 16 * t);
                            unit.AddHealth(-Mathf.CeilToInt(unit.maxHealth * t));
                            unit.mf.SlowDown(0.5f, 1);
                        }

                    }


                if (buildMode != type.citadel)
                    SoundManager.PlayFlatSound(SoundManager.Sound.build);
                //if (buildMode != type.harasserHive && buildMode != type.mainhive && buildMode != type.buildupHive && buildMode != type.citadel && buildMode != type.ruin && buildMode != type.reclaimerHive && buildMode != type.harasserHiveTelegraphed && buildMode != type.buildupHiveTelegraphed && buildMode != type.reclaimerHiveTelegraphed)

                break;

            case type.mainhive:
            case type.ruin:
            case type.harasserHiveTelegraphed:
            case type.buildupHiveTelegraphed:
            case type.reclaimerHiveTelegraphed:
            case type.turningHiveTelegraphed:
            case type.reclaimerHive:
            case type.harasserHive:
            case type.buildupHive:
            case type.highground:
            case type.turningHive:
                if (GetIfBusy(x, y))
                {
                    Debug.Log("busy");
                    SoundManager.PlayFlatSound(SoundManager.Sound.buttonPressError);
                    return null;
                }

                if (!Instantiate(Buildable.GetPrefab(buildMode), center, Quaternion.identity).TryGetComponent<Building>(out newBuilding))
                    Debug.LogError("the prefab doesnt have building script");

                //kill units under building 
                if (newBuilding.TryGetComponent<CircleCollider2D>(out collider))
                    foreach (Collider2D c in Physics2D.OverlapCircleAll(center, collider.radius, MaskProcessing.instance.unit))
                        c.GetComponent<Entity>().Kill();


                break;
            default:
                break;
        }


        grid.SetValue(x, y, newBuilding);
        onBuild?.Invoke(buildMode, x, y);
        buildMode = type.empty;
        return newBuilding;
    }

    public void ForceBuild(type mode, int x, int y)
    {
        Vector2 center = grid.GetCellCenter(x, y);
        if (!Instantiate(Buildable.GetPrefab(mode), center, Quaternion.identity).TryGetComponent<Building>(out Building newBuilding))
            Debug.LogError("the prefab doesnt have building script");
    }
    public bool GetIfBusy(int x, int y)
    {
        return grid.GetValue(x, y) != emptyBuilding;
    }

    public bool GetIfBusy(Vector2 pos)
    {
        int x, y;
        grid.GetClampedXY(pos, out x, out y);
        return GetIfBusy(x, y);
    }
    public bool GetIfWalkable(int x, int y)
    {
        BuildGrid.type t = grid.GetValue(x, y).type;
        return t == type.empty || t == type.ruin || t == type.barricade;

    }
    public bool GetIfWalkable(Vector2 pos)
    {
        int x, y;
        grid.GetClampedXY(pos, out x, out y);
        return GetIfWalkable(x, y);
    }
}
[System.Serializable]
public class Buildable
{
    public BuildGrid.type buildType;
    public int price;
    public int reward;
    public Transform prefab;
    public Transform ghost;

    //[field: SerializeField] public bool builtOnRest { get; private set; }

    public static Transform GetPrefab(BuildGrid.type mode)
    {
        foreach (Buildable b in BuildGrid.instance.buildings)
            if (b.buildType == mode)
                return b.prefab;


        Debug.LogError("prefab of type " + mode + " not found");
        return null;
    }
    public static int GetPrice(BuildGrid.type mode)
    {
        foreach (Buildable b in BuildGrid.instance.buildings)
            if (b.buildType == mode)
                return b.price;

        //Debug.LogError("price of type " + mode + " not found");
        return 0;
    }
    public static int GetReward(BuildGrid.type mode)
    {
        foreach (Buildable b in BuildGrid.instance.buildings)
            if (b.buildType == mode)
                return b.reward;

        //Debug.LogError("price of type " + mode + " not found");
        return 0;
    }
    public static Transform GetGhost(BuildGrid.type mode)
    {
        foreach (Buildable b in BuildGrid.instance.buildings)
            if (b.buildType == mode)
                return b.ghost;

        Debug.LogError("ghost of type " + mode + " not found");
        return null;
    }

    // public static bool GetIfBuiltOnRest(BuildGrid.type mode)
    // {
    //     foreach (Buildable b in BuildGrid.instance.buildings)
    //         if (b.buildType == mode)
    //             return b.builtOnRest;

    //     Debug.LogError("builtOnRest of type " + mode + " not found");
    //     return false;
    // }
}