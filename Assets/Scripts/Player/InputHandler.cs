using UnityEngine;
using UnityEngine.InputSystem;
namespace Alpha.Player
{
    public class InputHandler : MonoBehaviour
    {
        public Vector2 move { get; private set; }
        public bool jump { get; private set; } = false;
        public bool crouch { get; private set; } = false;
        void Start()
        {

        }

        void Update()
        {
            jump = Keyboard.current.spaceKey.isPressed;
            crouch = Keyboard.current.leftShiftKey.isPressed;
            if (Keyboard.current == null)
            {
                move = Vector2.zero;
                return;
            }
            float xVelocity=0, zVelocity=0;
            if(Keyboard.current.wKey.isPressed)
                zVelocity += 1;
            if (Keyboard.current.sKey.isPressed)
                zVelocity -= 1;
            if(Keyboard.current.aKey.isPressed)
                xVelocity -= 1;
            if(Keyboard.current.dKey.isPressed)
                xVelocity += 1;
            move = new Vector2(xVelocity, zVelocity).normalized;

        }
    }
}