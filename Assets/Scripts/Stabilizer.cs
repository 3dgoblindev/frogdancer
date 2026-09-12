using UnityEngine;

public class Stabilizer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    //mantine la rotacion del objeto estable, aunque su padre gire o se mueva, para que el objeto hijo no rote con el padre
    void Update()
    {
        transform.rotation = Quaternion.identity;
    }


}
