using UnityEngine;
using UnityEngine.UI;

public class followAgent : MonoBehaviour
{
    public GameObject agent;
    Transform agentLoc;
    SimpleWalkerAgent reward;
    public Text score;
    Vector3 offset;
    // Start is called before the first frame update
    void Start()
    {
        agentLoc = agent.GetComponent<Transform>();
        reward = agent.GetComponent<SimpleWalkerAgent>();
        offset = new Vector3(1f, 3f, -10f);
        this.transform.position = agentLoc.position + offset;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position = new Vector3(1f, 3f, agentLoc.position.z + -10f);
        score.text = "Score: " + reward.currentReward.ToString("#.0");

    }
}
