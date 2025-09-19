using UnityEngine;

namespace DeadCells.Player
{
    public abstract class PlayerState
    {
        protected PlayerStateMachine stateMachine;
        protected PlayerController player;
        
        protected PlayerState(PlayerStateMachine stateMachine, PlayerController player)
        {
            this.stateMachine = stateMachine;
            this.player = player;
        }
        
        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Exit() { }
    }
    
    public class PlayerIdleState : PlayerState
    {
        public PlayerIdleState(PlayerStateMachine stateMachine, PlayerController player) : base(stateMachine, player) { }
        
        public override void Update()
        {
            if (player.Input.AttackPressed)
            {
                stateMachine.ChangeState(stateMachine.AttackState);
                return;
            }
            
            if (player.Input.RollPressed)
            {
                stateMachine.ChangeState(stateMachine.RollState);
                return;
            }
            
            if (player.CanJump())
            {
                stateMachine.ChangeState(stateMachine.JumpState);
                return;
            }
            
            if (Mathf.Abs(player.Input.Horizontal) > 0.1f)
            {
                stateMachine.ChangeState(stateMachine.MoveState);
                return;
            }
            
            if (!player.IsGrounded && player.Rigidbody.velocity.y < 0)
            {
                stateMachine.ChangeState(stateMachine.FallState);
                return;
            }
        }
    }
    
    public class PlayerMoveState : PlayerState
    {
        public PlayerMoveState(PlayerStateMachine stateMachine, PlayerController player) : base(stateMachine, player) { }
        
        public override void Update()
        {
            if (player.Input.AttackPressed)
            {
                stateMachine.ChangeState(stateMachine.AttackState);
                return;
            }
            
            if (player.Input.RollPressed)
            {
                stateMachine.ChangeState(stateMachine.RollState);
                return;
            }
            
            if (player.CanJump())
            {
                stateMachine.ChangeState(stateMachine.JumpState);
                return;
            }
            
            if (Mathf.Abs(player.Input.Horizontal) < 0.1f)
            {
                stateMachine.ChangeState(stateMachine.IdleState);
                return;
            }
            
            if (!player.IsGrounded && player.Rigidbody.velocity.y < 0)
            {
                stateMachine.ChangeState(stateMachine.FallState);
                return;
            }
        }
        
        public override void FixedUpdate()
        {
            player.Move(player.Input.Horizontal);
        }
    }
    
    public class PlayerJumpState : PlayerState
    {
        public PlayerJumpState(PlayerStateMachine stateMachine, PlayerController player) : base(stateMachine, player) { }
        
        public override void Enter()
        {
            player.Jump();
        }
        
        public override void Update()
        {
            if (player.Input.AttackPressed)
            {
                stateMachine.ChangeState(stateMachine.AttackState);
                return;
            }
            
            if (player.Rigidbody.velocity.y <= 0)
            {
                stateMachine.ChangeState(stateMachine.FallState);
                return;
            }
        }
        
        public override void FixedUpdate()
        {
            player.Move(player.Input.Horizontal);
        }
    }
    
    public class PlayerFallState : PlayerState
    {
        public PlayerFallState(PlayerStateMachine stateMachine, PlayerController player) : base(stateMachine, player) { }
        
        public override void Update()
        {
            if (player.Input.AttackPressed)
            {
                stateMachine.ChangeState(stateMachine.AttackState);
                return;
            }
            
            if (player.IsGrounded)
            {
                if (Mathf.Abs(player.Input.Horizontal) > 0.1f)
                    stateMachine.ChangeState(stateMachine.MoveState);
                else
                    stateMachine.ChangeState(stateMachine.IdleState);
                return;
            }
        }
        
        public override void FixedUpdate()
        {
            player.Move(player.Input.Horizontal);
        }
    }
    
    public class PlayerAttackState : PlayerState
    {
        private float attackTimer;
        private const float ATTACK_DURATION = 0.3f;
        
        public PlayerAttackState(PlayerStateMachine stateMachine, PlayerController player) : base(stateMachine, player) { }
        
        public override void Enter()
        {
            attackTimer = ATTACK_DURATION;
            // Trigger attack animation and logic here
        }
        
        public override void Update()
        {
            attackTimer -= Time.deltaTime;
            
            if (attackTimer <= 0)
            {
                if (player.IsGrounded)
                {
                    if (Mathf.Abs(player.Input.Horizontal) > 0.1f)
                        stateMachine.ChangeState(stateMachine.MoveState);
                    else
                        stateMachine.ChangeState(stateMachine.IdleState);
                }
                else
                {
                    stateMachine.ChangeState(stateMachine.FallState);
                }
            }
        }
    }
    
    public class PlayerRollState : PlayerState
    {
        private float rollTimer;
        private const float ROLL_DURATION = 0.4f;
        private const float ROLL_SPEED = 12f;
        
        public PlayerRollState(PlayerStateMachine stateMachine, PlayerController player) : base(stateMachine, player) { }
        
        public override void Enter()
        {
            rollTimer = ROLL_DURATION;
        }
        
        public override void Update()
        {
            rollTimer -= Time.deltaTime;
            
            if (rollTimer <= 0)
            {
                if (player.IsGrounded)
                {
                    if (Mathf.Abs(player.Input.Horizontal) > 0.1f)
                        stateMachine.ChangeState(stateMachine.MoveState);
                    else
                        stateMachine.ChangeState(stateMachine.IdleState);
                }
                else
                {
                    stateMachine.ChangeState(stateMachine.FallState);
                }
            }
        }
        
        public override void FixedUpdate()
        {
            float rollDirection = player.FacingRight ? 1f : -1f;
            player.Rigidbody.velocity = new Vector2(rollDirection * ROLL_SPEED, player.Rigidbody.velocity.y);
        }
    }
}