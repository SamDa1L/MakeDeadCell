using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// Captures and provides player input state
    /// Interfaces with keyboard/controller input and exposes it to the state machine
    /// </summary>
    public class PlayerInput
    {
        #region Movement Input
        private float horizontalInput;
        private bool jumpPressed;
        private bool jumpHeld;
        #endregion

        #region Action Input
        private bool attackPressed;
        private bool rollPressed;
        private bool crouchHeld;
        private bool crouchPressed;
        private bool crouchReleased;
        #endregion

        #region Climbing Input
        private float climbAxis;
        private bool climbHeld;
        #endregion

        #region Properties
        public float Horizontal => horizontalInput;

        public bool JumpPressed => jumpPressed;
        public bool JumpHeld => jumpHeld;

        public bool AttackPressed => attackPressed;
        public bool RollPressed => rollPressed;

        public bool CrouchHeld => crouchHeld;
        public bool CrouchPressed => crouchPressed;
        public bool CrouchReleased => crouchReleased;

        public float ClimbAxis => climbAxis;
        public bool ClimbHeld => climbHeld;
        #endregion

        /// <summary>
        /// Update input state - should be called once per frame in PlayerController.Update()
        /// </summary>
        public void Update()
        {
            // Movement
            horizontalInput = Input.GetAxisRaw("Horizontal");
            jumpPressed = Input.GetKeyDown(KeyCode.Space);
            jumpHeld = Input.GetKey(KeyCode.Space);

            // Actions
            attackPressed = Input.GetMouseButtonDown(0);
            rollPressed = Input.GetKeyDown(KeyCode.LeftShift);
            crouchHeld = Input.GetKey(KeyCode.C);
            crouchPressed = Input.GetKeyDown(KeyCode.C);
            crouchReleased = Input.GetKeyUp(KeyCode.C);

            // Climbing (using vertical axis)
            climbAxis = Input.GetAxisRaw("Vertical");
            climbHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        }
    }
}