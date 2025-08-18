using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniMap : MonoBehaviour
{

    public Transform player; // Asigna el objeto del jugador en el Inspector
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void LateUpdate()
    {
        Vector3 newPosition = player.position;
        newPosition.y = transform.position.y; // Mantiene la altura del minimapa
        transform.position = newPosition;
        transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f); // Mantiene la rotación del minimapa

        // Asegúrate de que el jugador no sea nulo
        //if (player != null)
        //{
        //    // Actualiza la posición del minimapa para que siga al jugador
        //    transform.position = new Vector3(player.position.x, transform.position.y, player.position.z);
        //}
    }
}
