using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class InfoScript : MonoBehaviour {

    public GameObject Head;
    public Text Distance;
    public Text Parts;
    public Text Scale;

	// Use this for initialization
	void Start () {
        float _distance = Vector3.Distance(Camera.main.transform.position, Head.transform.position);
        Distance.text = "Distance: " + _distance;

        int layers = 0;
        foreach(Transform trans in Head.GetComponentsInChildren<Transform>())
        {
            layers++;
        }
        Parts.text = "Parts: " + layers;

        Scale.text = "Scale: " + Head.transform.localScale.z;

	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
