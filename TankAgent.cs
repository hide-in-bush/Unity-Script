using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;



public class TankAgent : Agent
{
    [Header("import data")]
    public bool invert;
    public GameObject opponent;
    public float speed = 5;
    public float angularSpeed = 10;
    public GameObject shellPrefab;
    public float shellSpeed = 10;
    public AudioClip shotAudio;


    private Rigidbody agentRb;
    private Rigidbody oppoagentRb;
    private TankHealth tankHealth;
    private TankHealth otankHealth;
    private int previoushp = 100;
    private int previousohp = 100;
    private float invertMult;
    private Transform firePosition;
    private bool shellIsReady=true;
    private float cdStartTime=-1f;



    public override void InitializeAgent()
    {
        //invert factor
        invertMult = invert ? -1f : 1f;
        //get rigitbody
        agentRb = GetComponent<Rigidbody>();
        oppoagentRb = opponent.GetComponent<Rigidbody>();
        //get hp
        tankHealth = GetComponent<TankHealth>();
        otankHealth = opponent.GetComponent<TankHealth>();
        //get fire position
        firePosition = transform.Find("FirePosition");
    }



    public override void AgentReset()
    {
        if (tankHealth.hp <= 0|| otankHealth.hp <= 0)
        {
            //destory shell
            //while(GameObject.Find("Shell(Clone)") != null)
           // { 
           //     Destroy(GameObject.Find("Shell(Clone)"));
           // }


            //set start position
            transform.localPosition = new Vector3(invertMult *10f, 0f, invertMult * 10f);
            transform.localRotation = Quaternion.Euler(new Vector3(0, 90 + invertMult * 90, 0));
            agentRb.velocity = new Vector3(0f, 0f, 0f);
            agentRb.angularVelocity = new Vector3(0f, 0f, 0f);

            opponent.transform.localPosition = new Vector3(invertMult *(-10f), 0f, invertMult * (-10f));
            opponent.transform.localRotation = Quaternion.Euler(new Vector3(0, 90 - invertMult * 90, 0));
            oppoagentRb.velocity = new Vector3(0f, 0f, 0f);
            oppoagentRb.angularVelocity = new Vector3(0f, 0f, 0f);



            //set start hp
            tankHealth.hp = 100;
            tankHealth.hpSlider.value = 1f;
            otankHealth.hpSlider.value = 1f;
            previoushp = 100;
            previousohp = 100;
        }
    }


    public override void CollectObservations()
    {
        //self info
        AddVectorObs(tankHealth.hp / 100f);
        AddVectorObs(invertMult * transform.localPosition.x / 20f);
        AddVectorObs(invertMult * transform.localPosition.z / 20f);
        AddVectorObs(invertMult * agentRb.velocity.x/speed);
        AddVectorObs(invertMult * agentRb.velocity.z/speed);
        AddVectorObs(agentRb.angularVelocity.y/angularSpeed);

        //enemy info
        AddVectorObs(otankHealth.hp / 100f);
        AddVectorObs(invertMult * opponent.transform.localPosition.x / 20f);
        AddVectorObs(invertMult * opponent.transform.localPosition.z / 20f);
        AddVectorObs(invertMult * oppoagentRb.velocity.x / speed);
        AddVectorObs(invertMult * oppoagentRb.velocity.z / speed);
        AddVectorObs(oppoagentRb.angularVelocity.y / angularSpeed);

        //shell cooldown info
        shellIsReady = (Time.fixedTime >= cdStartTime + 1f) ? true : false;
        if (shellIsReady) AddVectorObs(1f);
        else AddVectorObs(-1f);


    }


    public override void AgentAction(float[] vectorAction, string textAction)
    {
        // Time penalty
        AddReward(-0.01f);

        //take action
        bool fire = false;
        var moveForward = Mathf.Clamp(vectorAction[0], -1f, 1f);
        var rotate = Mathf.Clamp(vectorAction[1], -1f, 1f);
        fire = Mathf.Clamp(vectorAction[2], -1f, 1f)>0;

        agentRb.velocity = transform.forward * moveForward * speed;
        agentRb.angularVelocity = transform.up * rotate * angularSpeed;

        //fire
        if (shellIsReady && fire)
        {
            //先去掉声音 AudioSource.PlayClipAtPoint(shotAudio, transform.position);
            GameObject go = GameObject.Instantiate(shellPrefab, firePosition.position, firePosition.rotation) as GameObject;
            go.GetComponent<Rigidbody>().velocity = go.transform.forward * shellSpeed;
            cdStartTime = Time.fixedTime;
            shellIsReady = false;
        }


        //count reward
        if (transform.localPosition.x < -15 || transform.localPosition.x > 15) AddReward(-0.05f);
        if (transform.localPosition.z < -15 || transform.localPosition.z > 15) AddReward(-0.05f);


        if (tankHealth.hp < previoushp)
        {
            previoushp = tankHealth.hp;
            AddReward(-0.5f);
        }

        if(otankHealth.hp < previousohp)
        {
            previousohp = otankHealth.hp;
            AddReward(1f);
        }

        if (tankHealth.hp <= 0)
        {
            Done();
        }
    }

}
