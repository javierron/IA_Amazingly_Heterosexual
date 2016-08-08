using UnityEngine;
using System.Collections;

public class TriggerSensor : MonoBehaviour {

    public Transform trans;
    public bool on_path;

    public void OnTriggerEnter2D(Collider2D collider) {
        if (collider.CompareTag("Path")) {
            on_path = true;
        }
    }

    public void OnTriggerExit2D(Collider2D collider) {
        if (collider.CompareTag("Path")) {
            on_path = false;
        }
    }
}
