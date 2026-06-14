using UnityEngine;

public class NoiseTrap : MonoBehaviour
{
    public MinotaurAI minotaur; //enemy

    public int triggersLeft = 1; //till trap breaks
    bool triggered; //trap broken


    private void OnTriggerEnter2D(Collider2D collision)
    {
      
        if (triggered || !collision.GetComponent<PlayerController>()) return; //trap is disabled or there isnt a player

        triggersLeft--;

        if (triggersLeft == 0) triggered = true;

        minotaur.TriggerNoise(transform.position);
    }

}
