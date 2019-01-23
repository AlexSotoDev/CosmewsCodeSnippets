using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Throw : MonoBehaviour
{
    Transform throwCameraTransform = null;

    [Header("Throw settings")]
    [Tooltip("The game object you're throwing. Should have a rigidbody component")]
    [SerializeField]
    GameObject throwableObject;

    //[SerializeField]
    //GameObject FeedbackManager;

	[SerializeField]
    [Tooltip("Invisible mochi operating with increased forces on rigidbody to fake throwable physics determinism for predictive cat catching")]
	GameObject fakeObject;

    [SerializeField]
    private float throwableHeldForwardOffset = 0.5f;

    Rigidbody throwableObjectRigidBody;
	Rigidbody fakeObjectRigidBody;

    private Vector2 deltaTouchVector;
    private Vector2 deltaTouchVectorClamped;
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;

    private float minTouchDeltaX;
    private float maxTouchDeltaX;
    private float minTouchDeltaY;
    private float maxTouchDeltaY;

    [Header("Ammo")]
    [SerializeField]
    private int startAmmo = int.MaxValue;
    private int currentAmmo;
    private int totalMochiThrown = 0;
    [SerializeField]
    private bool hasUnlimitedAmmo = true;

    /*
    [Header("Debug Touch Point Image")]
    [Tooltip("Shows UI touch start touch and endpoints using UI Sprite")]
    [SerializeField]
    private Image startImage;
    [SerializeField]
    private Image endImage;
    */

    private Vector3 forwardHeldVector;

    [Header("Throw Scaling Forces")]

    [SerializeField]
    public float XForceScalar = 10f;
    [SerializeField]
    public float YForceScalar = 15f;
    [SerializeField]
    public float ZForceScalar = 10f;

    Vector2 currentSwipeVector;
    Vector3 currentThrowVector;

    private float XForceScalarInit;
    private float YForceScalarInit;
    private float ZForceScalarInit;

    private float minThrowXInit;
    private float maxThrowXInit;

    private float minThrowYInit;
    private float maxThrowYInit;

    private float minThrowZInit;
    private float maxThrowZInit;

    public delegate void UpdateSwipeVector(Vector2 swipe);
    public event UpdateSwipeVector swipeEvent;

    public delegate void UpdateThrowVector(Vector3 throwVector);
    public event UpdateThrowVector throwEvent;

	bool isBeingHeld;

    [Header("Mechanical Rework")]
    //[SerializeField]
    float swipeMinX;
    //[SerializeField]
    float swipeMaxX;
    //[SerializeField]
    float swipeMinY;
    //[SerializeField]
    float swipeMaxY;

    [Space]

    [SerializeField]
    float reworkMinXThrowForce;
    [SerializeField]
    float reworkMaxXThrowForce;
    [SerializeField]
    float reworkMinYThrowForce;
    [SerializeField]
    float reworkMaxYThrowForce;
    [SerializeField]
    float reworkMinZThrowForce;
    [SerializeField]
    float reworkMaxZThrowForce;

    Vector2 clampedSwipeVector;
    Vector2 roundedSwipeVector;

    [Header("Screen Grid")]
    [SerializeField]
    public int horizontalSubdivisions = 10;
    [SerializeField]
    public int verticalSubdivisions = 10;

    [Header("Mochi Scale (uniform scale)")]
    [SerializeField]
    float heldThrowMochiStartScale;
    [SerializeField]
    float releaseThrowMochiEndScale;
    [SerializeField]
    float timeToScale;
    [SerializeField]
    AnimationCurve throwScaleCurve;

    Vector2 rawSwipe;

    [Header("User clamping (OLD, Deprecated by rework)")]
    [SerializeField]
    public float minThrowX = Mathf.NegativeInfinity;
    [SerializeField]
    public float maxThrowX = Mathf.Infinity;
    [Space]
    [SerializeField]
    public float minThrowY = -Mathf.Infinity;
    [SerializeField]
    public float maxThrowY = Mathf.Infinity;
    [Space]
    [SerializeField]
    public float minThrowZ = 0f;
    [SerializeField]
    public float maxThrowZ = Mathf.Infinity;

    [Header("UI Debug Info")]
    [SerializeField]
    Text swipeVectorStr;
    [SerializeField]
    Text throwVectorStr;
    [SerializeField]
    Text rawSwipeVectorStr;
    [SerializeField]
    Text roundedSwipeVectorStr;

    [SerializeField]
    Text swipeVectorMagnitudeStr;
    [SerializeField]
    Text throwVectorMagnitudeStr;
    [SerializeField]
    Text rawSwipeMagnitudeStr;

    private void Awake()
    {
        throwCameraTransform = Camera.main.transform;
    }

    // Use this for initialization
    void Start ()
    {
        InitializeMochi();
        InitializeMochiStats();

        SubscribeToTouchInputEvents();

        forwardHeldVector = new Vector3(0f, 0f, throwableHeldForwardOffset);

        //Replaced InitializeSwipeRange() with throw rework
        SetDeviceMinMaxDeltaTouch(Screen.currentResolution.width, Screen.currentResolution.height);

        currentSwipeVector = Vector2.zero;

        //Default values
        SetStartVals();
        //InitializeSwipeRange();
    }

    private void SetDeviceMinMaxDeltaTouch(int deviceWidth, int deviceHeight)
    {
        minTouchDeltaX = -deviceWidth * 0.5f;
        maxTouchDeltaX = deviceWidth * 0.5f;

        //Min should be clamped to 0 for our case since we never want to throw mochi with down force (only positive input)
        minTouchDeltaY = 0f;
        maxTouchDeltaY = deviceHeight;
    }
    private void SubscribeToTouchInputEvents()
    {
        InputManager.Instance.TouchBeganEvent += OnTouchBegin;
        InputManager.Instance.TouchIdleEvent += OnTouchIdle;
        InputManager.Instance.TouchEndedEvent += OnTouchEnd;
        InputManager.Instance.TouchCanceledEvent += OnTouchEnd;

    }
    
    private void UnsubscribeToTouchInputEvents()
    {
        InputManager.Instance.TouchBeganEvent -= OnTouchBegin;
        InputManager.Instance.TouchIdleEvent -= OnTouchIdle;
        InputManager.Instance.TouchEndedEvent -= OnTouchEnd;
        InputManager.Instance.TouchCanceledEvent -= OnTouchEnd;
    }

	Vector2 TranslateClampedSwipe(Vector2 swipeVector)
    {
        swipeVector.x /= horizontalSubdivisions;
        swipeVector.y /= verticalSubdivisions;

        float x = (float)System.Math.Round(swipeVector.x, 1);
        float y = (float)System.Math.Round(swipeVector.y, 1);

        return Vector2.zero;
    }

    private void InitializeSwipeRange()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        swipeMinX = -screenWidth;
        swipeMaxX = screenWidth;

        swipeMinY = -screenHeight;
        swipeMaxY = screenHeight;
    }

    private void InitializeMochiStats()
    {
        if(hasUnlimitedAmmo)
        {
            currentAmmo = int.MaxValue;
        }
        else
        {
            currentAmmo = startAmmo;
        }

        totalMochiThrown = 0;
    }

    private void InitializeMochi()
    {
        //Legacy instantiate code
        //Replaced with Eddie's object pooling system
        //throwableObject = Instantiate(throwableObject, this.transform.position, Quaternion.identity);
        //fakeObject = Instantiate (fakeObject, this.transform.position, Quaternion.identity);

		//throwableObjectRigidBody = throwableObject.GetComponent<Rigidbody>();
		//fakeObjectRigidBody = fakeObject.GetComponent<Rigidbody>();

        //Doesn't need to be initialized anymore. Called on TouchBegin()

        //throwableObject = MochiPoolManager.Instance.SpawnRealMochi(this.transform.position, Quaternion.identity);
        //fakeObject = MochiPoolManager.Instance.SpawnRealMochi(this.transform.position, Quaternion.identity);

		if (NetworkManager.singleton != null && NetworkManager.singleton.isNetworkActive)
        {
            NetworkServer.Spawn(throwableObject);
            NetworkServer.Spawn(fakeObject);
            //Debug.Log("Spawned Mochi on Network");
        }
    }

    private void SetStartVals()
    {
        XForceScalarInit = XForceScalar;
        YForceScalarInit = YForceScalar;
        ZForceScalarInit = ZForceScalar;

        minThrowXInit = minThrowX;
        maxThrowXInit = maxThrowX;

        minThrowYInit = minThrowY;
        maxThrowYInit = maxThrowY;

        minThrowZInit = minThrowZ;
        maxThrowZInit = maxThrowZ;
    }

    public void ResetValsToStartVals()
    {
        XForceScalar = XForceScalarInit;
        YForceScalar = YForceScalarInit;
        ZForceScalar = ZForceScalarInit;

        minThrowX = minThrowXInit;
        maxThrowX = maxThrowXInit;

        minThrowY = minThrowYInit;
        maxThrowY = maxThrowYInit;

    }

    private void OnDestroy()
    {
        UnsubscribeToTouchInputEvents();
    }

    private void SetMochiHoldingState(bool isHeld)
    {
        throwableObject.GetComponent<PooledMochi>().isMochiHeld = isHeld;
        fakeObject.GetComponent<PooledMochi>().isMochiHeld = isHeld;

        isBeingHeld = isHeld;
    }


    private void OnTouchBegin(Touch currentTouch)
    {
        
        if (currentAmmo > 0 || hasUnlimitedAmmo)
        {
            SetMochiHoldingState(true);

            startTouchPosition = currentTouch.position;

            //For Pooling
            throwableObject = MochiPoolManager.Instance.SpawnRealMochi(this.transform.position, Quaternion.identity, heldThrowMochiStartScale);
            fakeObject = MochiPoolManager.Instance.SpawnFakeMochi(this.transform.position, Quaternion.identity, heldThrowMochiStartScale);

            throwableObjectRigidBody = throwableObject.GetComponent<Rigidbody>();
            fakeObjectRigidBody = fakeObject.GetComponent<Rigidbody>();

            throwableObject.transform.position = throwCameraTransform.position + throwCameraTransform.TransformDirection(forwardHeldVector);

            throwableObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

            throwableObject.transform.rotation = throwCameraTransform.rotation;

            fakeObject.transform.position = throwCameraTransform.position + throwCameraTransform.TransformDirection(forwardHeldVector);

            fakeObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;

            fakeObject.transform.rotation = throwCameraTransform.rotation;

            throwableObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
            fakeObject.GetComponent<Rigidbody>().velocity = Vector3.zero;

            //startImage.rectTransform.anchoredPosition = startTouchPosition;


            //FeedbackManager.GetComponent<Feedback>().feedbackCall("press");
			CosmewsAudioManager.Main.PlayNewSound("s_Press", false, false);

			//AudioManager.Instance.Play("ThrowTouch");

			iOSHapticFeedback.Instance.Trigger(iOSHapticFeedback.iOSFeedbackType.ImpactLight);

			//currentWorm.GetComponentInChildren<FacePoseManager>().TransitionToExpression("HungryAnimation");
            
        }
    }

    private void OnTouchIdle(Touch currentTouch)
    {
        if (currentAmmo > 0 || hasUnlimitedAmmo)
        {
			isBeingHeld = true;

            Vector3 vec = Camera.main.ScreenToWorldPoint(new Vector3(currentTouch.position.x, currentTouch.position.y, forwardHeldVector.z));
            throwableObject.transform.position = vec;

            throwableObject.transform.rotation = throwCameraTransform.rotation;

            fakeObject.transform.position = vec;

            fakeObject.transform.rotation = throwCameraTransform.rotation;
            //endImage.rectTransform.anchoredPosition = currentTouch.position;
        }

    }
    
    private void OnTouchEnd(Touch currentTouch)
    {
        if (currentAmmo > 0 || hasUnlimitedAmmo)
        {
            SetMochiHoldingState(false);

            endTouchPosition = currentTouch.position;

            deltaTouchVector = endTouchPosition - startTouchPosition;

            //endImage.rectTransform.anchoredPosition = endTouchPosition;

            //ThrowMochi(throwableObject, currentTouch.deltaPosition);

            //ThrowFakeMochi(fakeObject, currentTouch.deltaPosition);

            ThrowMochiRework(throwableObject, currentTouch.deltaPosition);

			//ThrowFakeMochiRework(fakeObject, currentTouch.deltaPosition);

            StartCoroutine(ScaleMochiOverTime(throwableObject, releaseThrowMochiEndScale, timeToScale));
            StartCoroutine(ScaleMochiOverTime(fakeObject, releaseThrowMochiEndScale, timeToScale));

            iOSHapticFeedback.Instance.Trigger(iOSHapticFeedback.iOSFeedbackType.SelectionChange);

            //UpdateDebugUIVals();
            //FeedbackManager.GetComponent<Feedback>().feedbackCall("release");
			CosmewsAudioManager.Main.PlayNewSound("s_Release", false, false);


        }

    }

	private void ThrowMochi(GameObject throwable, Vector2 swipeVector)
	{
        throwableObjectRigidBody.constraints = RigidbodyConstraints.None;

        currentSwipeVector = swipeVector;

        float xForce = swipeVector.x;
        float yForce = swipeVector.y;
        float zForce = swipeVector.y;

        //xForce = Mathf.Clamp(xForce, minTouchDeltaX, maxTouchDeltaX);
        //yForce = Mathf.Clamp(yForce, minTouchDeltaY, maxTouchDeltaY);
        //zForce = Mathf.Clamp(zForce, minTouchDeltaY, maxTouchDeltaY);


        float finalXForce = xForce;
        finalXForce *= (XForceScalar / 100f);

        float finalYForce = yForce;
        finalYForce *= (YForceScalar / 100f);

        float finalZForce = zForce;
        finalZForce *= (ZForceScalar / 100f);

        Vector3 dir = new Vector3(finalXForce, finalYForce, finalZForce);

        dir.x = Mathf.Clamp(finalXForce, minThrowX, maxThrowX);
        dir.y = Mathf.Clamp(finalYForce, minThrowY, maxThrowY);
        dir.z = Mathf.Clamp(finalZForce, minThrowZ, maxThrowZ);

        currentThrowVector = dir;
        //Debug.Log("Swipe Vector: " + swipeVector);
        //Debug.Log("Swipe magnitude: " + swipeVector.magnitude);
        //Debug.Log("Throw vector: " + currentThrowVector);
        //Debug.Log("Throw magnitude: " + currentThrowVector.magnitude);

        throwableObjectRigidBody.AddRelativeForce(dir, ForceMode.Impulse);

        BroadcastThrow(currentThrowVector);

        currentAmmo--;
        totalMochiThrown++;
		//Debug.Log (dir);

    }

	private void ThrowFakeMochi(GameObject throwable, Vector2 swipeVector)
	{
		fakeObjectRigidBody.constraints = RigidbodyConstraints.None;

		currentSwipeVector = swipeVector;

		float xForce = swipeVector.x;
		float yForce = swipeVector.y;
		float zForce = swipeVector.y;

		//xForce = Mathf.Clamp(xForce, minTouchDeltaX, maxTouchDeltaX);
		//yForce = Mathf.Clamp(yForce, minTouchDeltaY, maxTouchDeltaY);
		//zForce = Mathf.Clamp(zForce, minTouchDeltaY, maxTouchDeltaY);


		float finalXForce = xForce;
		finalXForce *= (XForceScalar / 100f);

		float finalYForce = yForce;
		finalYForce *= (YForceScalar / 100f);

		float finalZForce = zForce;
		finalZForce *= (ZForceScalar / 100f);

		Vector3 dir = new Vector3(finalXForce, finalYForce, finalZForce);

		dir.x = Mathf.Clamp(finalXForce, minThrowX, maxThrowX);
		dir.y = Mathf.Clamp(finalYForce, minThrowY, maxThrowY);
		dir.z = Mathf.Clamp(finalZForce, minThrowZ, maxThrowZ);

		currentThrowVector = dir;

		//Debug.Log("Fake throw vector: " + dir * 2.0f);

		fakeObjectRigidBody.AddRelativeForce(dir * 2.0f, ForceMode.Impulse);
		//Debug.Log (dir * 2);

	}

    private void UpdateDebugUIVals()
    {
        swipeVectorStr.text = currentSwipeVector.ToString();
        throwVectorStr.text = currentThrowVector.ToString();

        swipeVectorMagnitudeStr.text = currentSwipeVector.magnitude.ToString();
        throwVectorMagnitudeStr.text = currentThrowVector.magnitude.ToString();

        swipeVectorMagnitudeStr.text = currentSwipeVector.magnitude.ToString();
        throwVectorMagnitudeStr.text = currentThrowVector.magnitude.ToString();

        rawSwipeVectorStr.text = rawSwipe.ToString();
        rawSwipeMagnitudeStr.text = rawSwipe.magnitude.ToString();

        roundedSwipeVectorStr.text = roundedSwipeVector.ToString();

    }

    private void BroadcastThrow(Vector3 throwVector)
    {
        if (throwEvent != null)
        {
            throwEvent(throwVector);
        }

    }

    private void BroadcastSwipe(Vector2 swipeVector)
    {
        if (throwEvent != null)
        {
            swipeEvent(swipeVector);
        }

    }

    public GameObject GetHeldMochi()
	{
		return throwableObject;
	}

    public bool GetIsBeingHeld()
	{
		return isBeingHeld;
	}

    private float nfmod(float a, float b)
    {
        return a - b *  Mathf.Floor(a / b);
    }

    private float NormalizeFloat(float val, float min, float max, float minRange = -1.0f, float maxRange = 1.0f)
    {
        return ( ( (maxRange-minRange) * ( (val - min) / (max - min) ) )  + maxRange );
    }
    

    private void ThrowMochiRework(GameObject throwableObject, Vector2 swipeInput)
    {
        rawSwipe = swipeInput;

        Rigidbody throwableBody = throwableObject.GetComponent<Rigidbody>();

        throwableBody.constraints = RigidbodyConstraints.None;

        float firstHorizontalSubdivision = ((float)(Screen.width) / horizontalSubdivisions);
        float firstVerticalSubdivision = ((float)(Screen.width) / verticalSubdivisions);

        float remainderX = swipeInput.x % firstHorizontalSubdivision;
        float remainderY = swipeInput.y % firstVerticalSubdivision;

        //Debug.Log("Remainder X: " + remainderX);
        //Debug.Log("Remainder Y: " + remainderY);

        float roundedX = swipeInput.x;
        float roundedY = swipeInput.y;

        if (remainderX >= (float)(horizontalSubdivisions) / 2)
        {
            roundedX += (firstHorizontalSubdivision - remainderX);
        }
        else
        {
            roundedX -= remainderX;
        }

        if (remainderY >= (float)(verticalSubdivisions) / 2)
        {
            roundedY += (firstVerticalSubdivision - remainderY);
        }
        else
        {
            roundedY -= remainderY;
        }

        Vector2 roundedVector = new Vector2(roundedX, roundedY);

        roundedSwipeVector = roundedVector;

        //Debug.Log("Rounded Swipe Vector: " + roundedVector);

        //First clamp for [-1,1] range
        float clampedX = roundedVector.x / swipeMaxX;
        float clampedY = roundedVector.y / swipeMaxY;

        //clampedY = Mathf.Clamp01(clampedY);

        Vector2 roundedScaledVector = new Vector2(clampedX, clampedY);

        //Now move scale to [-0.5f, 0.5f] for positive and negative input. Move range from [0,1] to [-0.5, 0.5]
        //clampedX -= 0.5f;
        //clampedY -= 0.5f;
        //Clamped Y can stay because we never want negative y input (mochi should always go forward (z/forward dir) and either upward arc or fall straight down (gravity will bring down without downward force applied)

        clampedSwipeVector = roundedScaledVector;

        //Debug.Log("Rounded SCALED swipe: " + clampedSwipeVector);

        currentSwipeVector = clampedSwipeVector;

        //Debug.Log("Clamped swipe vector: " + clampedSwipeVector);

        Vector3 throwForce = new Vector3(clampedSwipeVector.x, clampedSwipeVector.y, clampedSwipeVector.y);

        throwForce.x *= XForceScalar;
        throwForce.y *= YForceScalar;
        throwForce.z *= ZForceScalar;

        throwForce.x = Mathf.Clamp(throwForce.x, reworkMinXThrowForce, reworkMaxXThrowForce);
        throwForce.y = Mathf.Clamp(throwForce.y, reworkMinYThrowForce, reworkMaxYThrowForce);
        throwForce.z = Mathf.Clamp(throwForce.z, reworkMinZThrowForce, reworkMaxZThrowForce);

        currentThrowVector = throwForce;

        throwableBody.AddRelativeForce(currentThrowVector, ForceMode.Impulse);

        BroadcastThrow(throwForce);

        currentAmmo--;
        totalMochiThrown++;
    }

	private void ThrowFakeMochiRework(GameObject throwableObject, Vector2 swipeInput)
	{
		rawSwipe = swipeInput;

		Rigidbody throwableBody = throwableObject.GetComponent<Rigidbody>();

		throwableBody.constraints = RigidbodyConstraints.None;

		float firstHorizontalSubdivision = ((float)(Screen.width) / horizontalSubdivisions);
		float firstVerticalSubdivision = ((float)(Screen.width) / verticalSubdivisions);

		float remainderX = swipeInput.x % firstHorizontalSubdivision;
		float remainderY = swipeInput.y % firstVerticalSubdivision;

		//Debug.Log("Remainder X: " + remainderX);
		//Debug.Log("Remainder Y: " + remainderY);

		float roundedX = swipeInput.x;
		float roundedY = swipeInput.y;

		if (remainderX >= (float)(horizontalSubdivisions) / 2)
		{
			roundedX += (firstHorizontalSubdivision - remainderX);
		}
		else
		{
			roundedX -= remainderX;
		}

		if (remainderY >= (float)(verticalSubdivisions) / 2)
		{
			roundedY += (firstVerticalSubdivision - remainderY);
		}
		else
		{
			roundedY -= remainderY;
		}

		Vector2 roundedVector = new Vector2(roundedX, roundedY);

		roundedSwipeVector = roundedVector;

		//Debug.Log("Rounded Swipe Vector: " + roundedVector);


		//First clamp for [-1,1] range
		float clampedX = roundedVector.x / swipeMaxX;
		float clampedY = roundedVector.y / swipeMaxY;

		//clampedY = Mathf.Clamp01(clampedY);

		Vector2 roundedScaledVector = new Vector2(clampedX, clampedY);

		//Now move scale to [-0.5f, 0.5f] for positive and negative input. Move range from [0,1] to [-0.5, 0.5]
		//clampedX -= 0.5f;
		//clampedY -= 0.5f;
		//Clamped Y can stay because we never want negative y input (mochi should always go forward (z/forward dir) and either upward arc or fall straight down (gravity will bring down without downward force applied)

		clampedSwipeVector = roundedScaledVector;

		//Debug.Log("Rounded SCALED swipe: " + clampedSwipeVector);

		currentSwipeVector = clampedSwipeVector;

		//Debug.Log("Clamped swipe vector: " + clampedSwipeVector);

		Vector3 throwForce = new Vector3(clampedSwipeVector.x, clampedSwipeVector.y, clampedSwipeVector.y);

		throwForce.x *= XForceScalar;
		throwForce.y *= YForceScalar;
		throwForce.z *= ZForceScalar;

		throwForce.x = Mathf.Clamp(throwForce.x, reworkMinXThrowForce, reworkMaxXThrowForce);
		throwForce.y = Mathf.Clamp(throwForce.y, reworkMinYThrowForce, reworkMaxYThrowForce);
		throwForce.z = Mathf.Clamp(throwForce.z, reworkMinZThrowForce, reworkMaxZThrowForce);

		currentThrowVector = throwForce;

        Vector3 fakeThrowForce = currentThrowVector * 4.0f;

		throwableBody.AddRelativeForce(fakeThrowForce, ForceMode.Impulse);

        BroadcastThrow(fakeThrowForce);

	}

    private IEnumerator ScaleMochiOverTime(GameObject mochi, float endScale = 1.0f, float duration = 1.0f)
    {

        float startTime = Time.time;
        Vector3 startScale = mochi.transform.localScale;
        Vector3 endScaleVector = new Vector3(endScale, endScale, endScale);

        while (Time.time < startTime + duration)
        {
            float progress = (Time.time - startTime) / duration;
            //mochi.transform.localScale = Vector3.Lerp(startScale, endScaleVector , (Time.time - startTime) / duration);
            mochi.transform.localScale = Vector3.Lerp(startScale, endScaleVector, throwScaleCurve.Evaluate( progress ));
            //Debug.Log((Time.time - startTime) / duration);
            yield return null;
        }
        mochi.transform.localScale = endScaleVector;
    }
    
}
