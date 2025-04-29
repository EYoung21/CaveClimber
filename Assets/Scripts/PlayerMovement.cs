using UnityEngine;
using System;
using System.Collections;
using Random = UnityEngine.Random;  // This is the correct way to reference Random

// Ice pick/pickaxe logic has been removed from this script
public class PlayerMovement : MonoBehaviour
{
    // private bool isMoving = false;
    public float speed;
    private Rigidbody2D rb2d;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private float inputX = 0;
    private float inputY = 0;

    private SpriteRenderer playerSpriteRenderer;

    public Sprite spriteDown;

    public Sprite[] framesRight;
    public Sprite[] framesUp; //climbing animation?
    public Sprite[] framesDown;

    public Sprite[] framesIdle; //defaulted right

    public Sprite[] deathAnimations;

    float frameTimerUp, frameTimerLeft, frameTimerRight, frameTimerDown, frameTimerIdle, frameTimerDeath;

    int frameIndexUp, frameIndexLeft, frameIndexRight, frameIndexDown, frameIndexIdle, frameIndexDeath;

    // float frameTimerLeft, frameTimerRight;
    float framesPerSecond = 10;

    private string lastDirection;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        playerSpriteRenderer = GetComponent<SpriteRenderer>();

        frameTimerUp = frameTimerDown = frameTimerLeft = frameTimerRight = frameTimerIdle = frameTimerDeath = (1f / framesPerSecond);
        frameIndexUp = frameIndexLeft = frameIndexRight = frameIndexDown = frameIndexIdle = frameIndexDeath = 0;

        lastDirection = "right"; //defaulted right
    }

    // Update is called once per frame
    void Update()
    {

        // KeyCode up = KeyCode.UpArrow;
        KeyCode right = KeyCode.RightArrow;
        KeyCode left = KeyCode.LeftArrow;
        // KeyCode down = KeyCode.DownArrow;

        bool isMoving = false;

        inputY = 0;
        inputX = 0;
        playerSpriteRenderer.flipX = false;

        if (Input.GetKey(left)) {
            isMoving = true;
            frameTimerLeft -= Time.deltaTime;
            if (frameTimerLeft <= 0) {
                frameIndexLeft++;
                if (frameIndexLeft >= framesRight.Length)
                {
                    frameIndexLeft = 0;
                }
                frameTimerLeft = (1f / framesPerSecond);
                playerSpriteRenderer.sprite = framesRight[frameIndexLeft];
            }
            playerSpriteRenderer.flipX = true;
            lastDirection = "left";
            inputX = -1;
        }

        else if (Input.GetKey(right)) {
            isMoving = true;
            frameTimerRight -= Time.deltaTime;
            if (frameTimerRight <= 0) {
                frameIndexRight++;
                if (frameIndexRight >= framesRight.Length) {
                    frameIndexRight = 0;
                }
                frameTimerRight = (1f / framesPerSecond);
                playerSpriteRenderer.sprite = framesRight[frameIndexRight];
            }
            inputX = 1;
            lastDirection = "right";
        }

        //idle animation
        if (!isMoving) {
            if (rb2d.linearVelocity.y < 0) {
                frameTimerDown -= Time.deltaTime;
                if (frameTimerDown <= 0) {
                    frameIndexDown++;
                    if (frameIndexDown >= framesDown.Length) {
                        frameIndexDown = 0;
                    }
                    frameTimerDown = (1f / framesPerSecond);
                    playerSpriteRenderer.sprite = framesDown[frameIndexDown];
                }
                if (lastDirection == "left") {
                    playerSpriteRenderer.flipX = true;
                }
            }
            else if (rb2d.linearVelocity.y > 0) {
                frameTimerUp -= Time.deltaTime;
                if (frameTimerUp <= 0) {
                    frameIndexUp++;
                    if (frameIndexUp >= framesUp.Length) {
                        frameIndexUp = 0;
                    }
                    frameTimerUp = (1f / framesPerSecond);
                    playerSpriteRenderer.sprite = framesUp[frameIndexUp];
                }
                if (lastDirection == "left") {
                    playerSpriteRenderer.flipX = true;
                }
            }
            else if (rb2d.linearVelocity.y == 0) {
                frameTimerIdle -= Time.deltaTime;
                if (frameTimerIdle <= 0) {
                    frameIndexIdle++;
                    if (frameIndexIdle >= framesIdle.Length) {
                        frameIndexIdle = 0;
                    }
                    frameTimerUp = (1f / framesPerSecond);
                    playerSpriteRenderer.sprite = framesIdle[frameIndexIdle];
                }
                if (lastDirection == "left") {
                    playerSpriteRenderer.flipX = true;
                }
            }
        }

        rb2d.linearVelocity = new Vector2(inputX, inputY) * speed;
    }


    public IEnumerator deathAnimation()
    {
        frameTimerDeath -= Time.deltaTime;
        if (frameTimerDeath <= 0)
        {
            frameIndexDeath++;
            if (frameIndexDeath >= deathAnimations.Length)
            {
                frameIndexDeath = 0;
            }
            frameTimerDeath = (1f / framesPerSecond);
            playerSpriteRenderer.sprite = deathAnimations[frameIndexDeath];
        }
        yield return new WaitForSeconds(1f);
    }

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(1f);
    }
    // private void animationLoop(Sprite[] animationArray)
    // {
    //     //animationTimer -= Time.deltaTime;
    //     //if (animationTimer < 0)
    //     //{
    //     //    animationTimer = 1f / animationFPS;
    //     //    currentFrame++;
    //     //    if (currentFrame >= animationArray.Length)
    //     //    {
    //     //        currentFrame = 0;

    //     //    }
    //     //    spriteRenderer.sprite = animationArray[currentFrame];
    //     //}

    // }

    // public IEnumerator deathAnimation()
    // {
    //     animationLoop(deathAnimations);
    //     yield return new WaitForSeconds(1f);
    // }
}