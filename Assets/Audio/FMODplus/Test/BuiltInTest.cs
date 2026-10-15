using UnityEngine;

public class BuiltInTest : MonoBehaviour
{
    [SerializeField] FmodEventPlayer testPlayer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        testPlayer.Play(0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
