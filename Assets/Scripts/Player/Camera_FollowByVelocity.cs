using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

//script to allow the camera to follow player ball, staying behind its movement direction
public class Camera_FollowByVelocity : MonoBehaviour
{

    //BOOLS TO CHANGE CAMERA FUNCTION
    public bool useVelocityBasedLerpSpeed;
    public bool useVelocityBasedFOV;
    //public bool cameraFollowsMoveDirection;

    private Vector3 staticCameraOffset;
    public float velocityBasedOffsetMultiplier;
    private float cameraHeight;
    private float minDistanceFromBall = 1;
    private float maxDistanceFromBall = 2;

    //CAMERA INTERPOLATION DAMPING
    //speed values for camera position/rotation interpolation
    private float xSpeed = 0.5f;
    private float ySpeed = 0.5f;
    private float zSpeed = 2.0f;
    private float rotationSpeed = 0.75f;

    //values by which to multiply the ball's velocity when calculating velocity-based speed values (if using velocity-based interpolation speed)
    private float velXSpeedMultiplier = 0.01f;
    private float velYSpeedMultiplier = 0.01f;
    private float velZSpeedMultiplier = 0.01f;
    //private float velBasedRotationSpeedMultiplier = 1.0f;

    //min/max velocity-based speed values
    private float velXSpeedMin = 0.25f;
    private float velYSpeedMin = 0.25f;
    private float velZSpeedMin = 0.25f;
    private float velXSpeedMax = 2.0f;
    private float velYSpeedMax = 1.0f;
    private float velZSpeedMax = 5.0f;

    //FOV
    //For velocity-based FOV change
    private float minFOV, maxFOV;
    private float velFOVMultiplier = 3.0f;

    //COLLISION AVOIDANCE
    private float minDistanceFromEnv = 3; //half the minimum distance the camera can be from the environment, for collision avoidance
    private float avoidSpeed = 10; //velocity at which camera tries to avoid collisions with environment (interpolated with other camera movement)
    private SphereCollider cameraCollider;
    private Collision[] cameraCollisions;

    public GameObject ball;
    private Rigidbody rb;

    private Vector3 lastVel = Vector3.zero; //used to maintain camera rotation when ball is stopped or moving very slowly
    private float sqrMinSpeed = 10; //(squared) minimum speed at which lastVel (so camera rotation) should be updated

    void Start()
    {
        rb = ball.GetComponent<Rigidbody>();
        if (rb == null) Debug.LogError("Camera follow target has no rigidbody!");
        cameraCollider = GetComponent<SphereCollider>();

        staticCameraOffset = this.transform.position - ball.transform.position; //initialises camera offset to its distance to associated ball in the editor
        cameraHeight = staticCameraOffset.y; //initialises camera height to height above ball in editor
        minFOV = GetComponent<Camera>().fieldOfView;
        maxFOV = minFOV + 20.0f;
    }

    void LateUpdate()
    {
        Vector3 vel = Vector3.zero;
        if(rb.velocity.sqrMagnitude < sqrMinSpeed)
        {
            vel = lastVel;
        }
        else
        {
            vel = rb.velocity;
            lastVel = rb.velocity; //only update lastVel if current speed > min speed
        }

        Vector3 ballPosition = ball.transform.position;

        if (useVelocityBasedLerpSpeed)
        {
            zSpeed = Mathf.Min(vel.sqrMagnitude * velZSpeedMultiplier, velZSpeedMax);
            xSpeed = Mathf.Min(vel.sqrMagnitude * velXSpeedMultiplier, velXSpeedMax);
        }

        /* CAMERA POSITIONING */
        //get camera offset and lock it to max/min camera distances if necessary
        Vector3 cameraOffset = vel * velocityBasedOffsetMultiplier;
        float offsetSqrMagnitude = cameraOffset.sqrMagnitude;

        if (offsetSqrMagnitude < minDistanceFromBall)
        {
            cameraOffset = cameraOffset.normalized * minDistanceFromBall;
        }
        else if (offsetSqrMagnitude > maxDistanceFromBall)
        {
            cameraOffset = cameraOffset.normalized * maxDistanceFromBall;
        }

        Debug.DrawLine(ball.transform.position, ball.transform.position - cameraOffset, Color.blue);

        Vector3 newCameraPosition = Vector3.zero;
        Vector3 avoidVector = AvoidCollisions();

        //LERP CAMERA MOVEMENT
        newCameraPosition.x = Mathf.Lerp(transform.position.x, ball.transform.position.x - cameraOffset.x + avoidVector.x, xSpeed * Time.deltaTime);
        //newCameraPosition.y = Mathf.Lerp(transform.position.y, (ball.transform.position.y - cameraOffset.y) + cameraHeight + avoidVector.y, positionLateralSpeed * Time.deltaTime); //Y with offset
        newCameraPosition.y = Mathf.Lerp(transform.position.y, ball.transform.position.y + cameraHeight + avoidVector.y, ySpeed * Time.deltaTime); //Y with only set camera height
        newCameraPosition.z = Mathf.Lerp(transform.position.z, ball.transform.position.z - cameraOffset.z + avoidVector.z, zSpeed * Time.deltaTime);
        

        //SMOOTHDAMP CAMERA MOVEMENT
        /*
        float cameraVelX = GetComponent<Camera>().velocity.x;
        float cameraVelY = GetComponent<Camera>().velocity.y;
        float cameraVelZ = GetComponent<Camera>().velocity.z;

        newCameraPosition.x = Mathf.SmoothDamp(transform.position.x, ball.transform.position.x - cameraOffset.x + avoidVector.x, ref cameraVelX, 0.25f);
        //newCameraPosition.y = Mathf.Lerp(transform.position.y, (ball.transform.position.y - cameraOffset.y) + cameraHeight + avoidVector.y, positionLateralSpeed * Time.deltaTime); //Y with offset
        newCameraPosition.y = Mathf.SmoothDamp(transform.position.y, ball.transform.position.y + cameraHeight + avoidVector.y, ref cameraVelY, 1.0f); //Y with only set camera height
        newCameraPosition.z = Mathf.SmoothDamp(transform.position.z, ball.transform.position.z - cameraOffset.z + avoidVector.z, ref cameraVelZ, 1.0f);
        */
        transform.position = newCameraPosition;

        /* CAMERA ROTATION */
        //if ball is onscreen, rotate camera to follow ball velocity; if ball is offscreen, rotate camera to follow ball. This way, the 
        //ball is not forced to the middle of the screen, but the camera quickly reorients itself on the ball if it goes offscreen.
        //The overall effect, depending on the speed settings, gives the camera a sense of weight and momentum, like a helicopter following a car.
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(GetComponent<Camera>());

        /*
        if (GeometryUtility.TestPlanesAABB(planes, ball.GetComponent<SphereCollider>().bounds)) //this checks if the ball's collider bounds are within the camera's frustum planes
        {
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(ballVelocity), rotationSpeed * Time.deltaTime);
        }
        else //if ball is offscreen, rotate camera to follow ball
        {
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(ball.transform.position - transform.position), rotationSpeed * Time.deltaTime);
        }
        */

        this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(vel), rotationSpeed * Time.deltaTime);

        /* CAMERA FOV */
        if (useVelocityBasedFOV)
        {
            float newFOV = Mathf.Clamp(vel.magnitude * velFOVMultiplier, minFOV, maxFOV);
            GetComponent<Camera>().fieldOfView = Mathf.Lerp(GetComponent<Camera>().fieldOfView, newFOV, Time.deltaTime);

        }
    }

    //function to stop the camera from colliding with the environment. Casts lines in six directions and returns a Vector3
    //containing the directions in which to move to avoid collisions
    Vector3 AvoidCollisions()
    {
        Vector3 avoidVector = Vector3.zero;

        if (Physics.Linecast(transform.position, transform.position + transform.TransformDirection(Vector3.forward * minDistanceFromEnv)))
        {
            avoidVector.z -= avoidSpeed;
        }
        if (Physics.Linecast(transform.position, transform.position + transform.TransformDirection(Vector3.back * minDistanceFromEnv)))
        {
            avoidVector.z += avoidSpeed;
        }
        if (Physics.Linecast(transform.position, transform.position + transform.TransformDirection(Vector3.left * minDistanceFromEnv)))
        {
            avoidVector.x += avoidSpeed;
        }
        if (Physics.Linecast(transform.position, transform.position + transform.TransformDirection(Vector3.right * minDistanceFromEnv)))
        {
            avoidVector.x -= avoidSpeed;
        }
        if (Physics.Linecast(transform.position, transform.position + transform.TransformDirection(Vector3.up * minDistanceFromEnv)))
        {
            avoidVector.y -= avoidSpeed;
        }
        if (Physics.Linecast(transform.position, transform.position + transform.TransformDirection(Vector3.down * minDistanceFromEnv)))
        {
            avoidVector.y += avoidSpeed;
        }

        return avoidVector;
    }

}
