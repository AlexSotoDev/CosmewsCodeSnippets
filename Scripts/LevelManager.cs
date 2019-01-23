using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : Singleton<LevelManager>
{
    [SerializeField]
    GameObject throwGameComponents;
    [SerializeField]
    GameObject shadowAndOcclusionPlanes;
    [SerializeField]
    GameObject wormPrefab;

    [SerializeField]
    GameObject portalPrefab;

    [SerializeField]
    List<GameObject> _levelPrefabs = new List<GameObject>();

    int _currentLevelIndex = 0;
    //GameObject feedbackManager;

    GameObject _activeLevel = null;

    private GameObject activeWorm = null;
    private GameObject activePortal = null;
    [SerializeField]
    public Vector3 wormSpawnInitialYOffset;
    [SerializeField]
    [Tooltip("Should match y of first row in order to match up")]
    public Vector3 wormSpawnFirstGrowthYOffset;

    [SerializeField]
    public Vector3 portalSpawnPosOffset = Vector3.zero;
    [SerializeField]
    public Vector3 arGridLocalOffset;

    public GameObject ActiveWorm
    {
        get
        {
            return activeWorm;
        }

        set
        {
            activeWorm = value;
        }
    }

    public GameObject ActivePortal
    {
        get
        {
            return activePortal;
        }

        set
        {
            activePortal = value;
        }
    }

    void Start()
    {
        InputManager.Instance.TouchBeganEvent += OnTouchBegan;
        GameManager.OnGameStateChanged += OnGameStateChanged;
        GrowGoalManager.OnAllGoalsReached += OnLevelComplete;
        throwGameComponents.SetActive(false);
        activeWorm = null;
        _currentLevelIndex = 0;
        //feedbackManager = GameObject.FindGameObjectWithTag("Feedback");

    }

    void OnTouchBegan(Touch touch)
    {
        if (GameManager.Instance.State == GameState.PREP_THROWING_GAME && _levelPrefabs.Count > 0)
        {
            OpenLevel(_currentLevelIndex);
            if (_currentLevelIndex == _levelPrefabs.Count)
            {
                _currentLevelIndex = 0;
            }
        }
        else if (GameManager.Instance.State == GameState.POST_THROWING_GAME)
        {
            GameManager.Instance.ChangeGameState(GameState.PREP_THROWING_GAME);
        }
    }

    public void OpenLevel(int levelIndex)
    {
        // Should have a component on the root game object of a level that gives more info about the state but for now it's just a GAMEOBJECt
        Transform levelParent = WormsPlaneManager.Instance.GameRootObject.transform;
        _activeLevel = Instantiate(_levelPrefabs[levelIndex], levelParent) as GameObject;

        // Should determine the Game State to switch to based on the level object selected
        GameManager.Instance.ChangeGameState(GameState.THROWING_GAME);
    }

    public void CloseLevel()
    {
        if (_activeLevel != null)
        {
            //Delete the suns created by the level then destroy the level game object (which have different parents for the sake of tracking consistency with tracking (suns should move with tracked plans vs no parent Unity space where they constantly will slip around according perpetual scanning/re-orientation.
            _activeLevel.GetComponent<GrowGoalManager>().DeleteSuns();
            Destroy(_activeLevel);
            //GameManager.Instance.ChangeGameState(GameState.PREP_THROWING_GAME);
        }
    }
    public void EnableThrowComponents()
    {
        throwGameComponents.SetActive(true);
    }
    public void DisableThrowComponents()
    {
        throwGameComponents.SetActive(false);
    }
    void OnGameStateChanged(GameState newState, GameState prevState)
    {
        if (newState == GameState.PREP_THROWING_GAME)
        {
            throwGameComponents.transform.position = WormsPlaneManager.Instance.GameRootObject.transform.position;
            shadowAndOcclusionPlanes.SetActive(true);
        }
        if (newState == GameState.THROWING_GAME)
        {
            EnableThrowComponents();
            SpawnPortal();
            SpawnWorm();
            SubscribeWormToGridCells();
        }
        if (newState == GameState.POST_THROWING_GAME)
        {
            CloseLevel();
            UnsubscribeWormToGridCells();
            DeSpawnWorm();
            DeSpawnPortal();
            DisableThrowComponents();
        }
    }

    private void UnsubscribeWormToGridCells()
    {
        activeWorm?.GetComponentInChildren<HeadFollow>().UnsubscribeFromGridCells(throwGameComponents.GetComponentInChildren<ARGrid>(true));
        //Debug.Log("Unsubscribed from grid cells");
    }

    private void SubscribeWormToGridCells()
    {
        activeWorm?.GetComponentInChildren<HeadFollow>().SubscribeToGridCells(throwGameComponents.GetComponentInChildren<ARGrid>(true));
        //Debug.Log("Subscribed to grid cells");
    }

    public void SpawnWorm()
    {
        if (activeWorm == null)
        {
            activeWorm = Instantiate(wormPrefab, WormsPlaneManager.Instance.GameRootObject.transform.position + wormSpawnInitialYOffset, Quaternion.identity) as GameObject;
            HeadFollow wormHead = activeWorm.GetComponentInChildren<HeadFollow>();
            Vector3 targetPostition = new Vector3(Camera.main.transform.position.x, wormHead.transform.position.y,Camera.main.transform.position.z);
            wormHead.GrowToPoint(WormsPlaneManager.Instance.GameRootObject.transform.position + wormSpawnFirstGrowthYOffset);
            //Debug.Log("Worm spawned at: " + activeWorm.transform.position);
            //Debug.Log("Worm spawned with rot: " + activeWorm.transform.rotation);
            //Debug.Log("Game ROOT Obj pos: " + WormsPlaneManager.Instance.GameRootObject.transform.position);
        }
    }

    private void OnLevelComplete()
    {
        //Debug.Log("Level completed");
        SwitchToNextLevel();
    }
    public void SwitchToPreviousLevel()
    {
        GameManager.Instance.ChangeGameState(GameState.POST_THROWING_GAME);
        if (_currentLevelIndex == 0)
        {
            _currentLevelIndex = 0;
        }
        else
        {
            _currentLevelIndex--;
        }        //Debug.Log("Current Level: " + _currentLevelIndex);
    }
    public void SwitchToNextLevel()
    {
        GameManager.Instance.ChangeGameState(GameState.POST_THROWING_GAME);
        if (_currentLevelIndex > _levelPrefabs.Count - 1)
        {
            _currentLevelIndex = _levelPrefabs.Count - 1;
        }
        else
        {
            _currentLevelIndex++;
        }
        //Debug.Log("Current Level: " + _currentLevelIndex);
    }

    void DeSpawnWorm()
    {
        if (activeWorm)
        {
            activeWorm.SetActive(false);
            Destroy(activeWorm.gameObject);
            //Debug.Log("Destroyed the currently active worm");
            activeWorm = null;
        }
    }
    void SpawnPortal()
    {
        if (activePortal == null)
        {
            //feedbackManager.GetComponent<Feedback>().feedbackCall("portalSound");
			CosmewsAudioManager.Main.PlayNewSound("s_PortalOpen", false, false);
            activePortal = Instantiate(portalPrefab, WormsPlaneManager.Instance.GameRootObject.transform.position + portalSpawnPosOffset, Quaternion.identity) as GameObject;
            //activePortal.transform.SetParent(WormsPlaneManager.Instance.GameRootObject.transform, true);
            //Debug.Log("Portal spawned at: " + activePortal.transform.position);
            //Debug.Log("Game ROOT Obj pos: " + WormsPlaneManager.Instance.GameRootObject.transform.position);
        }
    }

    void DeSpawnPortal()
    {
        if (activePortal)
        {
            activePortal.SetActive(false);
            Destroy(activePortal.gameObject);
            Debug.Log("Destroyed the currently active portal");
            activePortal = null;
        }
    }
    void RotateThrowObjectsToFaceCamera()
    {
        Vector3 targetPostition = new Vector3(Camera.main.transform.position.x, throwGameComponents.transform.position.y, Camera.main.transform.position.z);
        throwGameComponents.transform.LookAt(targetPostition);
    }

    void RotateWormToFaceCamera()
    {
            Vector3 targetPostition = new Vector3(Camera.main.transform.position.x, activeWorm.transform.position.y, Camera.main.transform.position.z);
            activeWorm.transform.LookAt(targetPostition);
    }
}
