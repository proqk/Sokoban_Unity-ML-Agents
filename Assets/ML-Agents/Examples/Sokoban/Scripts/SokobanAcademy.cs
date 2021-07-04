using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MLAgents;


public class SokobanAcademy : Academy
{
    [HideInInspector]
    public List<GameObject> actorObjs;
    [HideInInspector]
    public int[] players;

    public GameObject trueAgent;

    public int gridSize;
    private int numBoxes; //박스의 수

    public GameObject camObject;
    Camera cam;
    Camera agentCam;

    public GameObject agentPref;
    public GameObject goalPref;
    public GameObject boxPref; //박스 오브젝트
    public GameObject pitPref;
    GameObject[] objects;

    GameObject plane;
    GameObject sN;
    GameObject sS;
    GameObject sE;
    GameObject sW;

    public override void InitializeAcademy() //아카데미 초기화
    {
        gridSize = (int)resetParameters["gridSize"];
        cam = camObject.GetComponent<Camera>();

        objects = new GameObject[4] {agentPref, goalPref, pitPref, boxPref}; //유니티의 게임 오브젝트들을 리스트처럼 저장하고 있다가 호출 시 인덱스에 해당하는 오브젝트 불러온다

        agentCam = GameObject.Find("agentCam").GetComponent<Camera>();

        actorObjs = new List<GameObject>();

        plane = GameObject.Find("Plane");
        sN = GameObject.Find("sN");
        sS = GameObject.Find("sS");
        sW = GameObject.Find("sW");
        sE = GameObject.Find("sE");
    }

    public void SetEnvironment() //게임 리셋 (게임 진행 전 모든 게임 오브젝트의 초기 상태 지정)
    {
        gridSize = (int)resetParameters["gridSize"]; //게임을 리셋할 때 바닥 타일의 범위는 리셋 파라미터의 gridSize 값에 따라 동적으로 변경

        cam.transform.position = new Vector3(-((int)resetParameters["gridSize"] - 1) / 2f, 
                                             (int)resetParameters["gridSize"] * 1.25f, 
                                             -((int)resetParameters["gridSize"] - 1) / 2f);
        cam.orthographicSize = ((int)resetParameters["gridSize"] + 5f) / 2f;

        List<int> playersList = new List<int>();
        numBoxes = (int)resetParameters["numBoxes"]; //리셋 파라미터에서 설정한 numBoxes값을 가져온다
        for (int i = 0; i < numBoxes; i++) //설정된 numBoxes(박수) 수만큼 3추가
        {
            playersList.Add(3); //자리이동 금지!! 1, 2보다 먼저 Add되어야 함
        }

        for (int i = 0; i < (int)resetParameters["numObstacles"]; i++) //리셋 파라미터에 설정된 numObstacles(빨간색 x) 수만큼 2 추가
        {
            playersList.Add(2);
        }
        
        for (int i = 0; i < (int)resetParameters["numGoals"]; i++) //numGoals(초록색 +) 수만큼 1추가
        {
            playersList.Add(1);
        }

        players = playersList.ToArray();

        plane.transform.localScale = new Vector3(gridSize / 10.0f, 1f, gridSize / 10.0f);
        plane.transform.position = new Vector3((gridSize - 1) / 2f, -0.5f, (gridSize - 1) / 2f);
        sN.transform.localScale = new Vector3(1, 1, gridSize + 2);
        sS.transform.localScale = new Vector3(1, 1, gridSize + 2);
        sN.transform.position = new Vector3((gridSize - 1) / 2f, 0.0f, gridSize);
        sS.transform.position = new Vector3((gridSize - 1) / 2f, 0.0f, -1);
        sE.transform.localScale = new Vector3(1, 1, gridSize + 2);
        sW.transform.localScale = new Vector3(1, 1, gridSize + 2);
        sE.transform.position = new Vector3(gridSize, 0.0f, (gridSize - 1) / 2f);
        sW.transform.position = new Vector3(-1, 0.0f, (gridSize - 1) / 2f);

        agentCam.orthographicSize = (gridSize) / 2f;
        agentCam.transform.position = new Vector3((gridSize - 1) / 2f, gridSize + 1f, (gridSize - 1) / 2f);

    }

    public override void AcademyReset() //게임 초기화 (게임에 있는 모든 오브젝트의 초기 위치 결정, 배치)
    {
        foreach (GameObject actor in actorObjs)
        {
            DestroyImmediate(actor);
        }
        SetEnvironment();

        actorObjs.Clear();

        //에이전트, 초록색 +, 빨간색 x의 초기 위치를 결정한다
        //좌표가 아니라 게임판을 0부터 번호 매겨서 그 번호에 오브젝트를 배치하는 식으로 한다
        HashSet<int> numbers = new HashSet<int>(); //중복X 리스트 HashSet - 오브젝트 위치 저장. 게임 초기화 시 소코반을 구성하는 오브젝트 위치를 자동으로 겹치지 않게 한다

        while(numbers.Count < numBoxes) //박스가 벽 면에 닿게 시작하면 아예 시작도 못 하는 경우의 수 발생 -> 박스만 먼저 벽에 닿지 않는 위치에서 생성 (먼저 Add했으므로 가능)
        {
            int x = Random.Range(0, gridSize - 2) + 1; //1~gridSize-2 값 중 하나
            int y = Random.Range(0, gridSize - 2) + 1;
            numbers.Add(x + gridSize * y); //게임판 좌표가 0,1,2,3..이렇게 넘어가므로 중간쯔음 지점은 이 식으로 계산한다
        }

        while (numbers.Count < players.Length + 1) //numbers의 길이가 players길이+1 될 때까지
        {
            numbers.Add(Random.Range(0, gridSize * gridSize)); //0부터 gridSize*gridSize - 1 사이에 있는 임의의 값 반환
        }
        int[] numbersA = Enumerable.ToArray(numbers);

        for (int i = 0; i < players.Length; i++)
        {
            int x = (numbersA[i]) / gridSize;
            int y = (numbersA[i]) % gridSize;
            GameObject actorObj = Instantiate(objects[players[i]]);
            actorObj.transform.position = new Vector3(x, -0.25f, y);
            actorObjs.Add(actorObj);
        }

        int x_a = (numbersA[players.Length]) / gridSize;
        int y_a = (numbersA[players.Length]) % gridSize;
        trueAgent.transform.position = new Vector3(x_a, -0.25f, y_a);

    }

    //박스가 초록색 +나 빨간색 x와 충돌했을 때 충돌한 오브젝트 두 가지를 모두 게임에서 삭제하고 남은 박스의 개수를 반환한다
    //소코반 게임에서 박스와 골의 개수는 같다=하나 골인할 때마다 같이 삭제
    public int RemoveBoxGoal(GameObject Box, GameObject Goal)
    {
        DestroyImmediate(Box); //게임에서 Box 바로 제거
        DestroyImmediate(Goal);
        actorObjs.Remove(Box); //게임을 구성하는 모든 오브젝트를 저장하는 리스트인 actorObjs에서 Box 제거
        actorObjs.Remove(Goal);

        int nBox = 0;
        foreach(GameObject obj in actorObjs) //for~in
        {
            if (obj.CompareTag("box")) nBox++; //box의 수를 센다
        }

        return nBox;
    }

    public override void AcademyStep()
    {

    }
}
