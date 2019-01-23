using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrowGoalManager : MonoBehaviour
{
    [SerializeField]
    public GameObject[] goals;

    [SerializeField]
    public GameObject goalPrefab;

    [SerializeField]
    public Vector3Int[] gridGoals;

    GameObject[] gridGoalObjects;

    GameObject activeSun;

    int activeSunIndex;

    GameObject activeGoalNode;
    int activeGoalIndex = 0;

    public delegate void AllGoalsReached();
    public static event AllGoalsReached OnAllGoalsReached;

    public delegate void FinalGoalReached();     public static event FinalGoalReached OnFinalGoalReached;

    ARGrid arGridReference;

    [SerializeField]
    Material goalStartMat;
    [SerializeField]
    Material goalEndMat;
    [SerializeField]
    float levelCompleteDelay = 2.5f;

	GameObject directionalLight;

    // Use this for initialization
    void Start()
    {

        gridGoalObjects = new GameObject[gridGoals.Length];

        //Debug.Log("Grid goals length: " + gridGoals.Length);
        //Debug.Log("Grid goal objects length: " + gridGoalObjects.Length);

        GameManager.OnGameStateChanged += OnGameStateChanged;
        arGridReference = FindObjectOfType<ARGrid>();

        if(arGridReference != null)
        {
            //Debug.Log("Found grid");
            InitializeGridGoalNodes();
        }
        else
        {
            //Debug.Log("Could not find grid");
        }
		directionalLight = GameObject.FindWithTag("Light");
    }

    public void DeleteSuns()
    {
        for (int i = 0; i < gridGoals.Length; i++)
        {
            Vector3Int currentCell = gridGoals[i];
            arGridReference.grid[currentCell.x, currentCell.y, currentCell.z].GetComponent<GridCell>().goalNode = null;
            Destroy(gridGoalObjects[i].gameObject);
        }
    }

    public ARGrid SetGridRef(ARGrid arGridRef)
    {
        arGridReference = arGridRef;
        return arGridReference;
    }

	private void OnEnable()
	{
		//SetLightDirection();
	}


    void SetLightDirection()
	{
		if(directionalLight)
		{
            Vector3 sunPos = goals[goals.Length - 1].transform.position;
            Vector3 lookRot = (LevelManager.Instance.ActivePortal.transform.position - sunPos);
            directionalLight.transform.position = sunPos;
            directionalLight.transform.rotation = Quaternion.LookRotation(lookRot, Vector3.up);
        }
	}

    private void InitializeGridGoalNodes()
    {
        UIManager.Instance.FadeScreenOut();
        //Set the planets where we would have suns to inactive
        for (int i = 0; i < gridGoals.Length; i++)
        {
            //arGridReference.Grid[gridGoals[i].x, gridGoals[i].y, gridGoals[i].z].gameObject.SetActive(false);
            GameObject currentSun = Instantiate(goalPrefab);
            //Debug.Log("Spawned sun");
            //Debug.Log(gridGoals[i]);
            //Debug.Log(arGridReference.grid[gridGoals[i].x, gridGoals[i].y, gridGoals[i].z].transform.position);
            currentSun.transform.position = arGridReference.grid[gridGoals[i].x, gridGoals[i].y, gridGoals[i].z].transform.position;
            currentSun.transform.SetParent(arGridReference.grid[gridGoals[i].x, gridGoals[i].y, gridGoals[i].z].transform);

            arGridReference.grid[gridGoals[i].x, gridGoals[i].y, gridGoals[i].z].GetComponent<GridCell>().goalNode = currentSun;

            //Debug.Log("Moved sun to corresponding ar grid position");
            currentSun.SetActive(false);
            gridGoalObjects[i] = currentSun;
        }
        //Debug.Log("Done with initialization. Will activate Sun 0");
        ActivateSun(0);
        //Now spawn the first sun

    }

    void InitializeGoalNodes()
    {
        for (int i = 0; i < goals.Length; i++)
        {
            goals[i].SetActive(false);
            goals[i].GetComponent<MeshRenderer>().material = goalStartMat;
        }

        ActivateNode(0);
    }


    void ActivateNode(int i)
    {
        goals[i].SetActive(true);
        SubscribeToNode(i);
        activeGoalNode = goals[i];
        activeGoalIndex = i;
    }

	void ActivateNode(GameObject node)     {
        node.SetActive(true);     }

    void ActivateSun(int i)
    {
        gridGoalObjects[i].SetActive(true);
        SubscribeToSun(i);
        activeSun = gridGoalObjects[i];
        activeSunIndex = i;

    }

	void ActivateSun(GameObject sun)
	{
		sun.SetActive(true);
    }

    void DeactivateSun(int i)
    {
        gridGoalObjects[i].SetActive(false);
        UnsubscribeFromSun(i);
    }


    void DeactivateNode(int i)
    {
        goals[i].SetActive(false);
        UnsubscribeFromNode(i);
    }

    void DeactivateNode(GameObject node)
    {
        node.SetActive(false);
        UnsubscribeFromNode(node);
    }

    void DeactivateSun(GameObject sun)
    {
        sun.SetActive(false);
        UnsubscribeFromSun(sun);
    }


    void SubscribeToNode(int i)
    {
        goals[i].GetComponent<NewGrowGoal>().goalReachedEvent += CurrentGoalHasBeenReached;
    }

    void SubscribeToSun(int i)
    {
        gridGoalObjects[i].GetComponent<NewGrowGoal>().goalReachedEvent += CurrentSunHasBeenReached;
    }

    void UnsubscribeFromNode(int i)
    {
        goals[i].GetComponent<NewGrowGoal>().goalReachedEvent -= CurrentGoalHasBeenReached;
    }

    void UnsubscribeFromSun(int i)
    {
        gridGoalObjects[i].GetComponent<NewGrowGoal>().goalReachedEvent -= CurrentSunHasBeenReached;
    }

    void UnsubscribeFromNode(GameObject node)
    {
        node.GetComponent<NewGrowGoal>().goalReachedEvent -= CurrentGoalHasBeenReached;
    }

    void UnsubscribeFromSun(GameObject sun)
    {
        sun.GetComponent<NewGrowGoal>().goalReachedEvent -= CurrentSunHasBeenReached;
    }

    void CurrentGoalHasBeenReached()
    {
        //Deactivate currentNode
        DeactivateNode(activeGoalNode);

        //Spawn Next Goal
        if (activeGoalIndex + 1 < goals.Length)
        {
            ActivateNode(activeGoalIndex + 1);
            //CosmewsAudioManager.Main.PlayNewSound("s_MunchSound", false, false);
        }
        //Win/end state
        else
        {
			ActivateNode(activeGoalNode);
            OnFinalGoalReached?.Invoke();
			ParticleEffectManager.Instance.SpawnParticle(ParticleEffects.SunExplosion, goals[goals.Length - 1].transform.position, Quaternion.identity, 1.0f);
			UIManager.Instance.FadeScreenIn();
            ActivateAllNodesPostGame();
            StartCoroutine(DelayedEndLevel(levelCompleteDelay));
        }
    }

    void CurrentSunHasBeenReached()
    {
        DeactivateSun(activeSun);
        if (activeSunIndex + 1 < gridGoals.Length)
        {
            CosmewsAudioManager.Main.PlayNewSound("s_EatingStarA", false, false);
            ActivateSun(activeSunIndex + 1);
        }
        else
        {
            CosmewsAudioManager.Main.PlayNewSound("s_BigStar", false, false);
			OnFinalGoalReached?.Invoke();
			ParticleEffectManager.Instance.SpawnParticle(ParticleEffects.SunExplosion, activeSun.transform.position, Quaternion.identity, 1.0f);
			UIManager.Instance.FadeScreenIn();
            StartCoroutine(DelayedEndLevel(levelCompleteDelay));
        }

    }

    void ActivateAllNodesPostGame()
    {
        for (int i = 0; i < goals.Length; i++)
        {
            goals[i].SetActive(true);
            goals[i].GetComponent<MeshRenderer>().material = goalEndMat;
        }
    }

    void ActivateAllSunsPostGame()
    {
        for (int i = 0; i < goals.Length; i++)
        {
            gridGoalObjects[i].SetActive(true);
        }
    }

    void ChangeAllGoalNodeMaterial(Material material)
    {
        for (int i = 0; i < goals.Length; i++)
        {
            goals[i].GetComponent<MeshRenderer>().material = material;
        }
    }

    IEnumerator DelayedEndLevel(float delay)
    {
        yield return new WaitForSeconds(delay);
        OnAllGoalsReached?.Invoke();
    }

    void DisableAllNodes()
    {
        for (int i = 0; i < goals.Length; i++)
        {
            goals[i].SetActive(false);
        }
    }

    void OnGameStateChanged(GameState newState, GameState prevState)
    {  
        if (newState == GameState.POST_THROWING_GAME)
        {
            GameManager.OnGameStateChanged -= OnGameStateChanged;
            Destroy(this.gameObject);
            //Debug.Log("Destroyed grow goal manager");
        }

    }

}
