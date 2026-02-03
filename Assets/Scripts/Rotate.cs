using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    [SerializeField] Vector3 _rotateDir;

    void Update()
    {
        transform.Rotate(_rotateDir * Time.deltaTime);
    }
}
