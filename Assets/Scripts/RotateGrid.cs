using System;
using UnityEngine;
using Oculus.Interaction.Input;
using UnityEngine.InputSystem;

public class RotateGrid : MonoBehaviour
{
    
    [Header("Editor Debug")]
    [SerializeField] private bool editorDebug = true;
    [SerializeField] private Transform debugHand;      // drag an empty GameObject here
    [SerializeField] private Key debugPinchKey = Key.G;
    [SerializeField] private GameObject debugTarget;   // drag your GRID root here
    
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRHand rightHand;
    [SerializeField] private GameObject flower;
    [SerializeField] private GameObject flower_scale;
    [SerializeField] private GameObject flower_rotate;
    [SerializeField] private GameObject pivot;
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float scaleMultiplier = 0.5f;
    [SerializeField] private float rotationSpeed = 1.0f;

    private Vector3 lastHandPosition;
    private bool isMoving = false;
    private bool isScaling = false;
    private bool isRotating = false;
    private Transform activeHand;
    private float initialHandDistance;
    private Vector3 initialScale;
    private Quaternion initialHandRotation;
    private GameObject selectedFlower;
    void Start()
    {
        if (editorDebug && Application.isEditor)
        {
            selectedFlower = debugTarget;
            if (debugHand != null)
                activeHand = debugHand;
            initialHandRotation = activeHand != null ? activeHand.rotation : Quaternion.identity;
        }
    }

    void Update()
    {
        debugHand.transform.Rotate(0f, 20f * rotationSpeed * Time.deltaTime, 0f, Space.World);
        if (editorDebug && Application.isEditor)
        {
            var kb = Keyboard.current;
            
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            {
                Debug.Log("DEBUG PINCH pressed");
            }

            if (Keyboard.current != null && Keyboard.current.gKey.wasReleasedThisFrame)
            {
                Debug.Log("DEBUG PINCH released");
            }
        }
        if (IsPinchingOrDebug())
        {
            // Force rotation test: no moving/scaling in debug
            RotateFlower();
        }
        Ray headRay = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (selectedFlower == null && Physics.Raycast(headRay, out hit, Mathf.Infinity))
        {
            var resolved = ResolveBoundRoot(hit.collider.gameObject);
            if (resolved != null && IsPinching())
            {
                selectedFlower = resolved;
                StartInteraction();
            }
        }


        if (selectedFlower != null)
        {
            if (leftHand.IsTracked && rightHand.IsTracked && leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index) && rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
            {
                //ScaleFlowerWithHands();
                isScaling = true;
                isMoving = false;
                isRotating = false;
            }
            else if (IsPinching())
            {
                if (HasRotated())
                {
                    RotateFlower();
                    isRotating = true;
                    isMoving = false;
                    isScaling = false;
                }
                else
                {
                    //MoveFlowerWithHand();
                    isMoving = true;
                    isScaling = false;
                    isRotating = false;
                }
            }
            else if (!IsPinching() && (isMoving || isRotating))
            {
                StopInteraction();
            }
        }
    }

    bool IsPinching()
    {
        if (leftHand == null || rightHand == null)
        {
            Debug.LogError("OVRHand references are missing! Assign LeftHand and RightHand in Inspector.");
            return false;
        }

        bool isLeftPinching = leftHand.IsTracked && leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool isRightPinching = rightHand.IsTracked && rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        if (isLeftPinching) activeHand = leftHand.transform;
        else if (isRightPinching) activeHand = rightHand.transform;

        return isLeftPinching || isRightPinching;
    }

    void StartInteraction()
    {
        if (selectedFlower == null) return;

        lastHandPosition = activeHand.position;
        if (selectedFlower.GetComponent<Rigidbody>() != null)
        {
            selectedFlower.GetComponent<Rigidbody>().isKinematic = true;
        }

        initialHandRotation = activeHand.rotation;
    }

    void StopInteraction()
    {
        selectedFlower = null;
        isMoving = false;
        isScaling = false;
        isRotating = false;
    }

    void MoveFlowerWithHand()
    {
        if (selectedFlower == null) return;

        Vector3 handDelta = activeHand.position - lastHandPosition;
        selectedFlower.transform.position += handDelta * moveSpeed;
        lastHandPosition = activeHand.position;
    }

    void ScaleFlowerWithHands()
    {
        if (leftHand.IsTracked && rightHand.IsTracked)
        {
            float currentHandDistance = Vector3.Distance(leftHand.transform.position, rightHand.transform.position);
            if (!isScaling)
            {
                initialHandDistance = currentHandDistance;
                initialScale = flower_scale.transform.localScale;
                isScaling = true;
            }

            float scaleFactor = currentHandDistance / initialHandDistance;
            flower_scale.transform.localScale = initialScale * scaleFactor * scaleMultiplier;
        }
    }

    
    bool BelongsTo(GameObject hitObj, GameObject boundRoot)
    {
        if (!boundRoot) return false;
        return hitObj == boundRoot || hitObj.transform.IsChildOf(boundRoot.transform);
    }

    GameObject ResolveBoundRoot(GameObject hitObj)
    {
        if (BelongsTo(hitObj, flower)) return flower;
        if (BelongsTo(hitObj, flower_scale)) return flower_scale;
        if (BelongsTo(hitObj, flower_rotate)) return flower_rotate;
        return null;
    }

    void RotateFlower()
    {
        Debug.Log("Rotates flower");
        if (selectedFlower == null) return;

        Debug.Log("Selected flower exists");
        float prevYaw = initialHandRotation.eulerAngles.y;
        float currYaw = activeHand.rotation.eulerAngles.y;
        float yDelta = Mathf.DeltaAngle(prevYaw, currYaw);
        selectedFlower.transform.RotateAround(pivot.transform.position, Vector3.up, yDelta * rotationSpeed);
        //selectedFlower.transform.Rotate(0f, 0.2f * rotationSpeed, 0f, Space.World);
        initialHandRotation = activeHand.rotation;
    }

    bool IsPinchingOrDebug()
    {
        if (editorDebug && Application.isEditor)
        {
            if (debugHand != null) activeHand = debugHand;

            var kb = Keyboard.current;
            return kb != null && kb[debugPinchKey].isPressed;
        }

        return IsPinching();
    }



    bool HasRotated()
    {
        if (selectedFlower == null) return false;

        float rotationDifference = Quaternion.Angle(activeHand.rotation, initialHandRotation);
        return rotationDifference > 5f; 
    }

    GameObject GetFlowerObject(GameObject obj)
    {
        if (obj == flower || obj == flower_scale || obj == flower_rotate)
        {
            return obj;
        }
        return null;
    }
}
