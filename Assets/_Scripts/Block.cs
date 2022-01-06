using UnityEngine;
using System.Collections;

public class Block : MonoBehaviour
{
    public float movementSpeed;
    public float rotationSpeed;
    public Platform[] platforms;
    [HideInInspector]
    public string meshName;
    [HideInInspector]
    public Color color;

    private Vector3 rotationDirection;
    Vector3 moveTarget;
    Vector3 moveLocalTarget;
    bool moveToPalette;
    bool held;
    Camera mainCamera;
    Plane plane;

    bool foundPlatform;
    bool moveToPlatform;
    public bool reachedPlatform;
    bool deleting;
    Vector3 deleteDirection;
    Platform platformFound;

    Vector3 platformDirection;

    private void Awake()
    {
        held = false;
        moveToPalette = false;
        moveToPlatform = false;
        deleting = false;
        reachedPlatform = false;
        transform.rotation = Random.rotation;
        rotationDirection = Random.rotation.eulerAngles;
    }

    private void Start()
    {
        rotationSpeed *= Random.Range(1f, 1.3f);
        mainCamera = Camera.main;
        plane = new Plane(Vector3.forward, transform.position + -mainCamera.transform.forward * 20);
        Vector3[] deleteDirections = { transform.forward, transform.right, -transform.forward, -transform.right };
        deleteDirection = deleteDirections[Random.Range(0, deleteDirections.Length)];
    }

    // Update is called once per frame
    void Update()
    {
        if (!moveToPlatform && !reachedPlatform && !deleting)
        {
            transform.Rotate(rotationDirection * rotationSpeed * Time.deltaTime);
        }
        if (moveToPalette)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, moveLocalTarget, movementSpeed * Time.deltaTime);
        }
        else if (held || moveToPlatform)
        {
            transform.position = Vector3.Lerp(transform.position, moveTarget, movementSpeed * Time.deltaTime * 5);
            if (moveToPlatform && Vector3.Distance(transform.position, moveTarget) < 0.01f)
            {
                moveToPlatform = false;
                reachedPlatform = true;
            }
        }
        if (deleting)
        {
            transform.Translate(deleteDirection * movementSpeed * Time.deltaTime * 10);
        }

        if (Input.GetKeyDown(KeyCode.Space) && reachedPlatform)
        {
            transform.eulerAngles = Vector3.zero;
        }
    }

    public void SetLocalTarget(Vector3 target)
    {
        moveToPalette = true;
        moveLocalTarget = target;
        //transform.localPosition = target;
    }

    private void OnMouseDrag()
    {
        if (!moveToPlatform)
        {
            moveToPalette = false;
            held = true;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                Vector3 pointalongplane = ray.origin + (ray.direction * distance);
                moveTarget = pointalongplane;
            }

            RaycastHit rayHit;
            if (Physics.Raycast(transform.position, ray.direction, out rayHit))
            {
                Platform p = rayHit.transform.gameObject.GetComponent<Platform>();
                if (p != null && p.ready && !p.used)
                {
                    foundPlatform = true;
                    platformFound = rayHit.transform.GetComponent<Platform>();
                    if (!platformFound.forceUp)
                    {
                        platformDirection = rayHit.normal;
                    }
                    else
                    {
                        platformDirection = Vector3.up;
                    }
                    BlockSpawner.instance.projectionPlane.SetActive(true);
                    BlockSpawner.instance.projectionPlane.transform.position = platformFound.transform.position;// + platformDirection.normalized;// * 0.778f;
                    BlockSpawner.instance.projectionPlane.transform.up = platformDirection;
                }
                else
                {
                    BlockSpawner.instance.projectionPlane.SetActive(false);
                    foundPlatform = false;
                    if (platformFound != null)
                    {
                        platformFound = null;
                    }
                }
            }
            else
            {
                BlockSpawner.instance.projectionPlane.SetActive(false);
                foundPlatform = false;
                if (platformFound != null)
                {
                    platformFound = null;
                }
            }
        }
    }

    private void OnMouseUp()
    {
        BlockSpawner.instance.projectionPlane.SetActive(false);
        held = false;
        if (!foundPlatform)
        {
            moveToPalette = true;
            BlockSpawner.instance.RepositionBlocks();
        }
        else
        {
            platformFound.used = true;
            platformFound.ready = false;
            //Destroy(platformFound);
            Destroy(GetComponent<BoxCollider>());

            BlockSpawner.instance.RemoveBlock(this);
            moveToPlatform = true;
            if (!platformFound.CompareTag("GridPlatform"))
            {
                moveTarget = platformFound.GetComponentInParent<Block>().transform.position + platformDirection.normalized * 1.1f;// 0.778f; 
            } else
            {
                moveTarget = platformFound.transform.position + platformDirection.normalized * 0.5f;
            }
            transform.eulerAngles = Vector3.zero;
            foreach (Platform p in platforms)
            {
                p.gameObject.SetActive(true);
                p.ready = true;
            }
        }
    }

    public void Delete()
    {
        StartCoroutine(DelayedDelete());
        Destroy(gameObject, 2f);
    }

    IEnumerator DelayedDelete()
    {
        yield return new WaitForSeconds(Random.Range(0f, 0.5f));
        moveToPalette = false;
        moveToPlatform = false;
        held = false;
        reachedPlatform = false;
        deleting = true;
    }

}
