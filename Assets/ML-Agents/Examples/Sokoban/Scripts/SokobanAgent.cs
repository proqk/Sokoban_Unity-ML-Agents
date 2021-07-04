using System;
using UnityEngine;
using System.Linq;
using MLAgents;

public class SokobanAgent : Agent
{
    [Header("Specific to GridWorld")]
    private SokobanAcademy academy;
    public float timeBetweenDecisionsAtInference;
    private float timeSinceDecision;

    [Tooltip("Because we want an observation right before making a decision, we can force " + 
             "a camera to render before making a decision. Place the agentCam here if using " +
             "RenderTexture as observations.")]
    public Camera renderCamera;

    [Tooltip("Selecting will turn on action masking. Note that a model trained with action " +
             "masking turned on may not behave optimally when action masking is turned off.")]
    public bool maskActions = true;

    private const int NoAction = 0;  // do nothing!
    private const int Up = 1;
    private const int Down = 2;
    private const int Left = 3;
    private const int Right = 4;

    public override void InitializeAgent()
    {
        academy = FindObjectOfType(typeof(SokobanAcademy)) as SokobanAcademy;
    }

    public override void CollectObservations()
    {
        // There are no numeric observations to collect as this environment uses visual
        // observations.

        // Mask the necessary actions if selected by the user.
        if (maskActions)
        {
            SetMask();
        }
    }

    /// <summary>
    /// Applies the mask for the agents action to disallow unnecessary actions.
    /// </summary>
    private void SetMask()
    {
        // Prevents the agent from picking an action that would make it collide with a wall
        var positionX = (int) transform.position.x;
        var positionZ = (int) transform.position.z;
        var maxPosition = academy.gridSize - 1;

        if (positionX == 0)
        {
            SetActionMask(Left);
        }

        if (positionX == maxPosition)
        {
            SetActionMask(Right);
        }

        if (positionZ == 0)
        {
            SetActionMask(Down);
        }

        if (positionZ == maxPosition)
        {
            SetActionMask(Up);
        }
    }

    // to be implemented by the developer
    //에이전트가 초록색 +나 빨간색 x에 충돌했을 때 이를 처리한다
    //에이전트가 취할 행동을 브레인을 통해 받아오고, 에이전트의 행동에 따라 환경에 변화를 준다
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        AddReward(-0.01f);
        int action = Mathf.FloorToInt(vectorAction[0]);

        Vector3 targetPos = transform.position;
        switch (action)
        {
            case NoAction:
                // do nothing
                break;
            case Right:
                targetPos = transform.position + new Vector3(1f, 0, 0f);
                break;
            case Left:
                targetPos = transform.position + new Vector3(-1f, 0, 0f);
                break;
            case Up:
                targetPos = transform.position + new Vector3(0f, 0, 1f);
                break;
            case Down:
                targetPos = transform.position + new Vector3(0f, 0, -1f);
                break;
            default:
                throw new ArgumentException("Invalid action value");
        }

        //Collider[] blockTest = Physics.OverlapBox(targetPos, new Vector3(0.3f, 0.3f, 0.3f));
        //if (blockTest.Where(col => col.gameObject.CompareTag("wall")).ToArray().Length == 0)
        //{
        //    transform.position = targetPos;
        //}
        //if (blockTest.Where(col => col.gameObject.CompareTag("pit")).ToArray().Length == 1)
        //{
        //    Done();
        //    SetReward(1f);
        //}
        //if (blockTest.Where(col => col.gameObject.CompareTag("goal")).ToArray().Length == 1)
        //{
        //    Done();
        //    SetReward(-1f);
        //}

        //OverlapBox
        //매개변수 2개: 가상의 박스의 중심 & x,y,z 방향에 대한 박스의 크기 절반
        //targetPos: 브레인으로부터 전달받은 액션에 따라 에이전트가 이동할 위치
        //x,y,z방향의 크기가 모두 0.3f인 가상의 박스 전달
        //즉, 중심이 targetPos이고 길이가 0.6인 가상의 박스와 겹치는 콜라이더 모두 반환
        Collider[] blockTest = Physics.OverlapBox(targetPos, new Vector3(0.3f, 0.3f, 0.3f)); //targetPos에 위치한 콜라이더 전부 받아옴

        //blockTest 중 태그가 wall인 오브젝트만 뽑아서 배열로 만들고 그 배열의 길이를 받아서 if문 들어감
        if (blockTest.Where(col => col.gameObject.CompareTag("wall")).ToArray().Length == 0) //경우의 수 1. 에이전트가 가려는 곳이 벽이 아니면 (에이전트가 벽과 충돌?)
        {
            if ((blockTest.Where(col => col.gameObject.CompareTag("pit")).ToArray().Length == 1) ||
                    (blockTest.Where(col => col.gameObject.CompareTag("goal")).ToArray().Length == 1))
            { //골(초록색+)이나 핏(빨간색x)에 충돌했으면, 게임 종료 및 보상 -1 획득 (박스가 들어가야 하므로 에이전트가 들어가면 무조건 -1)
                Done();
                SetReward(-1f);
            }
            else if (blockTest.Where(col => col.gameObject.CompareTag("box")).ToArray().Length == 1) //경우의 수 2. 박스를 밀었다 (에이전트가 박스와 충돌?)
            {  // 가능한 경우의 수 (1. 박스가 x에 충돌? 2.박스가 벽이나 다른 박스에 충돌? 3.박스가 골+에 충돌? 4. 박스를 nextBoxPos로 그냥 밀기)
                GameObject box = blockTest[0].gameObject;
                Vector3 nextBoxPos = 2 * targetPos - transform.position;
                //에이전트가 1 이동하면 박스는 에이전트 기준으로 2 이동해야 한다 (밀었으니까)
                /*에이전트의 위치 targetPos = transform.position + 이동거리
                 * 박스 위치 nextBoxPos = transform.position + (2*이동거리) = (2*transform.position) - transform.position + (2* 이동거리) 
                 * = 2 * (transform.position+이동거리)-transform.position = 2* targetPos - transform.position
                */

                Collider[] boxBlockTest = Physics.OverlapBox(nextBoxPos, new Vector3(0.3f, 0.3f, 0.3f)); //박스 콜라이더 지름 0.6
                if (boxBlockTest.Where(col => col.gameObject.CompareTag("pit")).ToArray().Length == 1)
                { //박스가 구멍에 빠짐
                    Done();
                    SetReward(-1f);
                    Debug.Log("박스구멍");
                }
                else if ((boxBlockTest.Where(col => col.gameObject.CompareTag("box")).ToArray().Length == 1) ||
                        (boxBlockTest.Where(col => col.gameObject.CompareTag("wall")).ToArray().Length == 1))
                { //박스를 벽이나 다른 박스 쪽으로 밀음
                    SetReward(-0.1f); //게임 끝나지 않고 에이전트와 박스의 위치 그대로, 학습 효율성을 위해 -0.1 보상 획득
                    Debug.Log("박스벽/박스");
                }
                else if (boxBlockTest.Where(col => col.gameObject.CompareTag("goal")).ToArray().Length == 1)
                { //박스를 골에 넣음
                    GameObject goal = boxBlockTest[0].gameObject;
                    transform.position = targetPos; //에이전트 이동
                    if (academy.RemoveBoxGoal(box, goal) == 0) Done(); //남은 박스가 없으면 게임 종료
                    SetReward(1f);
                    Debug.Log("박스골");
                }
                else
                { //그냥 박스를 미는 경우
                    box.transform.position = nextBoxPos; //박스 이동
                    transform.position = targetPos; //에이전트 이동
                    SetReward(0.1f); //박스를 많이 밀도록 유도하기 위해서 0.1 보상
                    Debug.Log("박스이동");
                }
            }
            else //경우의 수 3. 그냥 이동
            {
                transform.position = targetPos;
            }
        }

    }

    // to be implemented by the developer
    public override void AgentReset()
    {
        academy.AcademyReset();
    }

    public void FixedUpdate()
    {
        WaitTimeInference();
    }

    private void WaitTimeInference()
    {
        if(renderCamera != null)
        {
            renderCamera.Render();
        }

        if (!academy.GetIsInference())
        {
            RequestDecision();
        }
        else
        {
            if (timeSinceDecision >= timeBetweenDecisionsAtInference)
            {
                timeSinceDecision = 0f;
                RequestDecision();
            }
            else
            {
                timeSinceDecision += Time.fixedDeltaTime;
            }
        }
    }
}
