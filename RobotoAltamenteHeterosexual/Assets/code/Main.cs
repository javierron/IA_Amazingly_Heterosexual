using UnityEngine;
using System.Collections;

public class Main : MonoBehaviour {
    public enum follower_type {
        Simple_One_Sensor,
        Simple_Two_Sensors,
        Fuzzy_Two_Sensors,
    }

    [System.Serializable]
    public struct debug_fuzzy_t {
        public float left_normalized_light;
        public float right_normalized_light;

        public float Left_Sensor_High;
        public float Left_Sensor_Med;
        public float Left_Sensor_Low;

        public float Right_Sensor_High;
        public float Right_Sensor_Med;
        public float Right_Sensor_Low;

        public float Go_Ahead_Fast;
        public float Turn_Left_Wide;
        public float Turn_Left_Sharp;
        public float Turn_Right_Wide;
        public float Go_Ahead_Slow;
        public float Turn_Right_Sharp;

        public float Normalized_Left_Motor_Speed;
        public float Normalized_Right_Motor_Speed;

        public float Left_Motor_Speed;
        public float Right_Motor_Speed;
    }

    public follower_type type;
    public float time_scale;

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

    public float fuzzy_max_k;
    public float fuzzy_min_k;

    public debug_fuzzy_t debug_fuzzy;

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
        Time.timeScale = time_scale;

        var left_normalized_light = normalized_light(left_trigger_sensors);
        var right_normalized_light = normalized_light(right_trigger_sensors);

        debug_fuzzy.left_normalized_light = left_normalized_light;
        debug_fuzzy.right_normalized_light = right_normalized_light;

        switch(type) {
            case follower_type.Simple_One_Sensor: {
                non_diffuse_follower_one_sensor(left_normalized_light, 
                                                right_normalized_light);
            } break;
            case follower_type.Simple_Two_Sensors: {
                non_diffuse_follower_two_sensors(left_normalized_light, 
                                                 right_normalized_light);
            }
            break;
            case follower_type.Fuzzy_Two_Sensors: {
                diffuse_follower_two_sensors(left_normalized_light, 
                                             right_normalized_light);
            }
            break;
        }
    }


    void non_diffuse_follower_one_sensor(float left_normalized_light, float right_normalized_light) {
        if (left_normalized_light > 0.2f) {
            left_motor(max_k);
            right_motor(min_k);
        } else {
            left_motor(min_k);
            right_motor(max_k);
        }
    }

    void non_diffuse_follower_two_sensors(float left_normalized_light, float right_normalized_light) {
        var Left_Sensor_High = left_normalized_light <= low_threshold;
        var Left_Sensor_Med = left_normalized_light > low_threshold && left_normalized_light < high_threshold;
        var Left_Sensor_Low = left_normalized_light >= high_threshold;

        var Right_Sensor_High = right_normalized_light <= low_threshold;
        var Right_Sensor_Med = right_normalized_light < high_threshold && right_normalized_light > low_threshold;
        var Right_Sensor_Low = right_normalized_light >= high_threshold;


        if(Left_Sensor_High && Right_Sensor_High) { left_motor(max_k); right_motor(max_k); } // Go Ahead Fast
        if(Left_Sensor_High && Right_Sensor_Low) { left_motor(min_k); right_motor(max_k); } // Turn Left Sharp
        if(Left_Sensor_High && Right_Sensor_Med) { left_motor(med_k); right_motor(max_k); } // Turn Left Wide

        if(Left_Sensor_Med && Right_Sensor_High) { left_motor(max_k); right_motor(med_k); } // Turn Right Wide
        if(Left_Sensor_Med && Right_Sensor_Med) { left_motor(med_k); right_motor(med_k); } // Go Ahead Slow
        if(Left_Sensor_Med && Right_Sensor_Low) { left_motor(min_k); right_motor(max_k); } // Turn Left Sharp

        if(Left_Sensor_Low && Right_Sensor_High) { left_motor(max_k); right_motor(min_k); } // Turn Right Sharp
        if(Left_Sensor_Low && Right_Sensor_Med) { left_motor(max_k); right_motor(min_k); } // Turn Right Sharp
        if(Left_Sensor_Low && Right_Sensor_Low) { Return_To_Path(); }
    }

    float trapezoidal_fm(float x, float a, float b, float c, float d) {
        return Mathf.Max(Mathf.Min((x-a)/(b-a), 1f, (d-x)/(d-c) ), 0f);
    }

    float sensor_low_fm(float normalized_light) {
        return trapezoidal_fm(normalized_light, 0.55f, 0.95f, 1f, 1.1f);
    }

    float sensor_med_fm(float normalized_light) {
        return trapezoidal_fm(normalized_light, 0.15f, 0.45f, 0.5f, 0.65f);
    }

    float sensor_high_fm(float normalized_light) {
        return trapezoidal_fm(normalized_light, -0.1f, 0f, 0.05f, 0.45f);
    }

    float trapezoidal_rv(float a, float b, float c, float d) {
        return 0.5f * ( b + c );
    }

    float speed_low_rv() {
        return trapezoidal_rv(-0.1f, 0f, 0.35f, 0.45f);
    }

    float speed_med_rv() {
        return trapezoidal_rv(0.35f, 0.45f, 0.55f, 0.65f);
    }

    float speed_high_rv() {
        return trapezoidal_rv(0.55f, 0.65f, 1f, 1.1f);
    }

    void speed_high_rvs(ref float accum, ref float count) {
        accum = (float) (0.55 + 0.6 + 0.65 + 0.7 + 0.75 + 0.8 + 0.85 + 0.9 + 0.95 + 1);
        count = 10;
    }

    void speed_med_rvs(ref float accum, ref float count) {
        accum = (float) ( 0.35 + 0.4 + 0.45 + 0.5 + 0.55 + 0.6 + 0.65 );
        count = 7;
    }

    void speed_low_rvs(ref float accum, ref float count) {
        accum = (float) ( 0 + 0.05 + 0.1 + 0.15 + 0.2 + 0.25 + 0.3 + 0.35 + 0.4 + 0.45 );
        count = 10;
    }

    void diffuse_follower_two_sensors(float left_normalized_light, float right_normalized_light) {
        var Left_Sensor_High = sensor_high_fm(left_normalized_light);
        var Left_Sensor_Med = sensor_med_fm(left_normalized_light);
        var Left_Sensor_Low = sensor_low_fm(left_normalized_light);

        var Right_Sensor_High = sensor_high_fm(right_normalized_light);
        var Right_Sensor_Med = sensor_med_fm(right_normalized_light);
        var Right_Sensor_Low = sensor_low_fm(right_normalized_light);


        debug_fuzzy.Left_Sensor_High = Left_Sensor_High;
        debug_fuzzy.Left_Sensor_Med = Left_Sensor_Med;
        debug_fuzzy.Left_Sensor_Low = Left_Sensor_Low;
        debug_fuzzy.Right_Sensor_High = Right_Sensor_High;
        debug_fuzzy.Right_Sensor_Med = Right_Sensor_Med;
        debug_fuzzy.Right_Sensor_Low = Right_Sensor_Low;


        var go_ahead_fast = Mathf.Min(Left_Sensor_High, Right_Sensor_High);
        var turn_left_wide = Mathf.Min(Left_Sensor_High, Right_Sensor_Med);
        var turn_left_sharp = Mathf.Min(Left_Sensor_High, Right_Sensor_Low);

        var turn_right_wide = Mathf.Min(Left_Sensor_Med, Right_Sensor_High);
        var go_ahead_slow = Mathf.Min(Left_Sensor_Med, Right_Sensor_Med);
        var turn_left_sharp_2 = Mathf.Min(Left_Sensor_Med, Right_Sensor_Low);

        var turn_right_sharp = Mathf.Min(Left_Sensor_Low, Right_Sensor_High);
        var turn_right_sharp_2 = Mathf.Min(Left_Sensor_Low, Right_Sensor_Med);

        /*
        if (Left_Sensor_High && Right_Sensor_High) { left_motor(max_k); right_motor(max_k); } // Go Ahead Fast
        if (Left_Sensor_High && Right_Sensor_Med) { left_motor(med_k); right_motor(max_k); } // Turn Left Wide
        if (Left_Sensor_High && Right_Sensor_Low) { left_motor(min_k); right_motor(max_k); } // Turn Left Sharp

        if (Left_Sensor_Med && Right_Sensor_High) { left_motor(max_k); right_motor(med_k); } // Turn Right Wide
        if (Left_Sensor_Med && Right_Sensor_Med) { left_motor(med_k); right_motor(med_k); } // Go Ahead Slow
        if (Left_Sensor_Med && Right_Sensor_Low) { left_motor(min_k); right_motor(max_k); } // Turn Left Sharp

        if (Left_Sensor_Low && Right_Sensor_High) { left_motor(max_k); right_motor(min_k); } // Turn Right Sharp
        if (Left_Sensor_Low && Right_Sensor_Med) { left_motor(max_k); right_motor(min_k); } // Turn Right Sharp
        if (Left_Sensor_Low && Right_Sensor_Low) { Return_To_Path(); }
        */

        var f_go_ahead_fast     = go_ahead_fast;                 // { left_motor(high), right_motor(high) }
        var f_turn_left_wide    = turn_left_wide;                // { left_motor(med),  right_motor(high) }
        var f_turn_left_sharp   = Mathf.Max(turn_left_sharp, 
                                            turn_left_sharp_2);  // { left_motor(low),  right_motor(high) }
        var f_turn_right_wide   = turn_right_wide;               // { left_motor(high), right_motor(med) }
        var f_go_ahead_slow     = go_ahead_slow;                 // { left_motor(med),  right_motor(med) }
        var f_turn_right_sharp  = Mathf.Max(turn_right_sharp, 
                                            turn_right_sharp_2); // { left_motor(high),  right_motor(low) }
        
        debug_fuzzy.Go_Ahead_Fast = f_go_ahead_fast;
        debug_fuzzy.Turn_Left_Wide = f_turn_left_wide;
        debug_fuzzy.Turn_Left_Sharp = f_turn_left_sharp;
        debug_fuzzy.Turn_Right_Wide = f_turn_right_wide;
        debug_fuzzy.Go_Ahead_Slow = f_go_ahead_slow;
        debug_fuzzy.Turn_Right_Sharp = f_turn_right_sharp;
        
        var left_motor_speed = 0f;
        var right_motor_speed = 0f;

        //{ // Defuzzification
        //    var total_weight = f_go_ahead_fast +
        //                       f_turn_left_wide +
        //                       f_turn_left_sharp +
        //                       f_turn_right_wide +
        //                       f_go_ahead_slow +
        //                       f_turn_right_sharp;


        //    left_motor_speed = f_go_ahead_fast * speed_high_rv() +
        //                       f_turn_left_wide * speed_med_rv() +
        //                       f_turn_left_sharp * speed_low_rv() +
        //                       f_turn_right_wide * speed_high_rv() +
        //                       f_go_ahead_slow * speed_med_rv() +
        //                       f_turn_right_sharp * speed_high_rv();
        //    left_motor_speed /= total_weight;


        //    right_motor_speed = f_go_ahead_fast * speed_high_rv() +
        //                        f_turn_left_wide * speed_high_rv() +
        //                        f_turn_left_sharp * speed_high_rv() +
        //                        f_turn_right_wide * speed_med_rv() +
        //                        f_go_ahead_slow * speed_med_rv() +
        //                        f_turn_right_sharp * speed_low_rv();
        //    right_motor_speed /= total_weight;
        //}

        { // Defuzzification Center of Gravity
            var speed_high_rvs_accum = 0f;
            var speed_high_rvs_count = 0f;
            speed_high_rvs(ref speed_high_rvs_accum, ref speed_high_rvs_count);
            var speed_med_rvs_accum = 0f;
            var speed_med_rvs_count = 0f;
            speed_med_rvs(ref speed_med_rvs_accum, ref speed_med_rvs_count);
            var speed_low_rvs_accum = 0f;
            var speed_low_rvs_count = 0f;
            speed_low_rvs(ref speed_low_rvs_accum, ref speed_low_rvs_count);


            var left_total_weight = f_go_ahead_fast * speed_high_rvs_count +
                                    f_turn_left_wide * speed_med_rvs_count +
                                    f_turn_left_sharp * speed_low_rvs_count +
                                    f_turn_right_wide * speed_high_rvs_count +
                                    f_go_ahead_slow * speed_med_rvs_count +
                                    f_turn_right_sharp * speed_high_rvs_count;
            left_motor_speed = f_go_ahead_fast * speed_high_rvs_accum +
                               f_turn_left_wide * speed_med_rvs_accum +
                               f_turn_left_sharp * speed_low_rvs_accum +
                               f_turn_right_wide * speed_high_rvs_accum +
                               f_go_ahead_slow * speed_med_rvs_accum +
                               f_turn_right_sharp * speed_high_rvs_accum;
            left_motor_speed /= left_total_weight;


            var right_total_weight = f_go_ahead_fast * speed_high_rvs_count +
                                     f_turn_left_wide * speed_high_rvs_count +
                                     f_turn_left_sharp * speed_high_rvs_count +
                                     f_turn_right_wide * speed_med_rvs_count +
                                     f_go_ahead_slow * speed_med_rvs_count +
                                     f_turn_right_sharp * speed_low_rvs_count;
            right_motor_speed = f_go_ahead_fast * speed_high_rvs_accum +
                                f_turn_left_wide * speed_high_rvs_accum +
                                f_turn_left_sharp * speed_high_rvs_accum +
                                f_turn_right_wide * speed_med_rvs_accum +
                                f_go_ahead_slow * speed_med_rvs_accum +
                                f_turn_right_sharp * speed_low_rvs_accum;
            right_motor_speed /= right_total_weight;
        }

        if (Left_Sensor_High == 0f &&
            Left_Sensor_Med == 0f &&
            Right_Sensor_High == 0f &&
            Right_Sensor_Med == 0f) { // Return To Path
            left_motor_speed = last_left_motor_speed;
            right_motor_speed = last_right_motor_speed;
        }

        debug_fuzzy.Normalized_Left_Motor_Speed = last_left_motor_speed = left_motor_speed;
        debug_fuzzy.Normalized_Right_Motor_Speed = last_right_motor_speed = right_motor_speed;

        left_motor_speed = ( fuzzy_max_k - fuzzy_min_k ) * left_motor_speed + fuzzy_min_k;
        right_motor_speed = ( fuzzy_max_k - fuzzy_min_k ) * right_motor_speed + fuzzy_min_k;
        
        debug_fuzzy.Left_Motor_Speed = left_motor_speed;
        debug_fuzzy.Right_Motor_Speed = right_motor_speed;
        
        left_motor(left_motor_speed);
        right_motor(right_motor_speed);
    }

    float last_left_motor_speed;
    float last_right_motor_speed;

    void Return_To_Path() {
        right_motor(min_k);
        left_motor(max_k);
    }
}
