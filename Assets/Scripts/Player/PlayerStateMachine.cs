using UnityEngine;

namespace DeadCellsTestFramework.Player
{
    public class PlayerStateMachine
    {
        private PlayerController player;
        private PlayerState currentState;
        
        // States
        public PlayerIdleState IdleState { get; private set; }
        public PlayerMoveState MoveState { get; private set; }
        public PlayerJumpState JumpState { get; private set; }
        public PlayerFallState FallState { get; private set; }
        public PlayerAttackState AttackState { get; private set; }
        public PlayerRollState RollState { get; private set; }
        
        public PlayerStateMachine(PlayerController player)
        {
            this.player = player;
            
            IdleState = new PlayerIdleState(this, player);
            MoveState = new PlayerMoveState(this, player);
            JumpState = new PlayerJumpState(this, player);
            FallState = new PlayerFallState(this, player);
            AttackState = new PlayerAttackState(this, player);
            RollState = new PlayerRollState(this, player);
        }
        
        public void Initialize()
        {
            currentState = IdleState;
            currentState.Enter();
        }
        
        public void Update()
        {
            currentState?.Update();
        }
        
        public void FixedUpdate()
        {
            currentState?.FixedUpdate();
        }
        
        public void ChangeState(PlayerState newState)
        {
            currentState?.Exit();
            currentState = newState;
            currentState?.Enter();
        }
        
        public PlayerState GetCurrentState() => currentState;
    }
}