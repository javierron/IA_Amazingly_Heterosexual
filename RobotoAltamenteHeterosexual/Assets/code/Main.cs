using UnityEngine;
using System.Collections;

public class Main : MonoBehaviour {

    public TriggerSensor trigger_sensor_prefab;
    public int trigger_sensors_n_per_sensor;

    public Transform left_light_sensor_trans;
    public TriggerSensor[] left_trigger_sensors;
    
    public Transform right_light_sensor_trans;
    public TriggerSensor[] right_trigger_sensors;

    public Transform left_motor_point;

    public Transform right_motor_point;

    public Transform car_trans;
    public Rigidbody2D car_body;

    public float max_k;
    public float min_k;
    public float med_k;

    public float low_threshold;
    public float high_threshold;

    public static
    void instantiate_trigger_sensors(ref Transform sensor, int trigger_sensor_n, ref TriggerSensor[] trigger_sensors, TriggerSensor prefab) {
        trigger_sensors = new TriggerSensor[trigger_sensor_n * trigger_sensor_n];

        var sensor_width = sensor.lossyScale.x;
        var trigger_sensor_width = prefab.trans.lossyScale.x;
        var disp = sensor_width / trigger_sensor_n;
        var half_trigger_sensor_n = (float)trigger_sensor_n / 2f;
        var initial_pos = sensor.position + half_trigger_sensor_n * disp * ( Vector3.left + Vector3.up);
        initial_pos += (trigger_sensor_width / 2f) * (Vector3.right + Vector3.down);
        var pos = initial_pos;
        for (int i = 0; i < trigger_sensor_n; ++i) {
            for (int j = 0; j < trigger_sensor_n; ++j) {
                var trigger_sensor = Instantiate<TriggerSensor>(prefab);
                trigger_sensors[i*trigger_sensor_n + j] = trigger_sensor;
                trigger_sensor.trans.position = pos;
                trigger_sensor.trans.parent = sensor;
                pos += disp * Vector3.right;
            }
            pos = initial_pos;
            pos += (i + 1) * disp * Vector3.down;
        }
    }

    public static
    float normalized_light(TriggerSensor[] trigger_sensors) {
        var n = 0f;
        for(int i = 0; i < trigger_sensors.Length; ++i) {
            if (trigger_sensors[i].on_path) {
                ++n;
            }
        }
        return (1f - n/trigger_sensors.Length);
    }

    public void Awake() {

        Time.timeScale = 10f;

        instantiate_trigger_sensors(ref left_light_sensor_trans, 
                                    trigger_sensors_n_per_sensor, 
                                    ref left_trigger_sensors, 
                                    trigger_sensor_prefab);
        instantiate_trigger_sensors(ref right_light_sensor_trans, 
                                    trigger_sensors_n_per_sensor, 
                                    ref right_trigger_sensors, 
                                    trigger_sensor_prefab);
    }

    public void left_motor(float k) {
        car_body.AddForceAtPosition(k * car_trans.up, 
                                    (Vector2)left_motor_point.position, 
                                    ForceMode2D.Impulse);
    }

    public void right_motor(float k) {
        car_body.AddForceAtPosition(k  * car_trans.up, 
                                    (Vector2)right_motor_point.position, 
                                    ForceMode2D.Impulse);
    }

    public void FixedUpdate() {
        var left_normalized_light = normalized_light(left_trigger_sensors);
        var right_normalized_light = normalized_light(right_trigger_sensors);


        //non_diffuse_follower(left_normalized_light, right_normalized_light);
        diffuse_follower(left_normalized_light, right_normalized_light);


    }


    void non_diffuse_follower(float left_normalized_light, float right_normalized_light) {
        if (left_normalized_light > 0.2f) {
            left_motor(max_k);
            right_motor(min_k);
        } else {
            left_motor(min_k);
            right_motor(max_k);
        }
    }

    void diffuse_follower(float left_normalized_light, float right_normalized_light) {
        var Left_Sensor_High = left_normalized_light <= low_threshold;
        var Left_Sensor_Med = left_normalized_light > low_threshold && left_normalized_light < high_threshold;
        var Left_Sensor_Low = left_normalized_light >= high_threshold;

        var Right_Sensor_High = right_normalized_light <= low_threshold;
        var Right_Sensor_Med = right_normalized_light < high_threshold && right_normalized_light > low_threshold;
        var Right_Sensor_Low = right_normalized_light >= high_threshold;


        if (Left_Sensor_High && Right_Sensor_High) { left_motor(max_k); right_motor(max_k); } // Go Ahead Fast
        if (Left_Sensor_High && Right_Sensor_Low) { left_motor(min_k); right_motor(max_k); } // Turn Left Sharp
        if (Left_Sensor_High && Right_Sensor_Med) { left_motor(med_k); right_motor(max_k); } // Turn Left Wide

        if (Left_Sensor_Med && Right_Sensor_High) { left_motor(max_k); right_motor(med_k); } // Turn Right Wide
        if (Left_Sensor_Med && Right_Sensor_Med) { left_motor(med_k); right_motor(med_k); } // Go Ahead Slow
        if (Left_Sensor_Med && Right_Sensor_Low) { left_motor(min_k); right_motor(max_k); } // Turn Left Sharp

        if (Left_Sensor_Low && Right_Sensor_High) { left_motor(max_k); right_motor(min_k); } // Turn Right Sharp
        if (Left_Sensor_Low && Right_Sensor_Med) { left_motor(max_k); right_motor(min_k); } // Turn Right Sharp
        if (Left_Sensor_Low && Right_Sensor_Low) { Return_To_Path(); }
    }

    void Return_To_Path() {
        right_motor(min_k);
        left_motor(max_k);
    }
}
