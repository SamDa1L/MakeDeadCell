using UnityEngine;

namespace DeadCells.Player
{
    public class PlayerInput
    {
        private float horizontalInput;
        private bool jumpPressed;
        private bool jumpHeld;
        private bool attackPressed;
        private bool rollPressed;
        
        public float Horizontal => horizontalInput;
        public bool JumpPressed => jumpPressed;
        public bool JumpHeld => jumpHeld;
        public bool AttackPressed => attackPressed;
        public bool RollPressed => rollPressed;
        
        public void Update()
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            jumpPressed = Input.GetKeyDown(KeyCode.Space);
            jumpHeld = Input.GetKey(KeyCode.Space);
            attackPressed = Input.GetMouseButtonDown(0);
            rollPressed = Input.GetKeyDown(KeyCode.LeftShift);
        }
    }
}