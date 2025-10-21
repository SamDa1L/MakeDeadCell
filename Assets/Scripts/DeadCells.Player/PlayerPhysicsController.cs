using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家物理控制器 - 集中管理所有物理操作
    /// 负责：速度控制、重力、碰撞体调整、角色翻转等
    /// 独立于状态机，既可被脚本驱动系统使用，也可被Animator驱动系统使用
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerPhysicsController : MonoBehaviour
    {
        // Rigidbody2D 组件引用
        private Rigidbody2D rb;

        // Collider2D 组件引用
        private Collider2D coll2D;

        // 角色是否面向右侧
        private bool facingRight = true;

        // 缓存原始碰撞体尺寸和偏移（用于从下蹲状态恢复）
        private Vector2 normalColliderSize;
        private Vector2 normalColliderOffset;

        private void Awake()
        {
            // 获取 Rigidbody2D 组件
            rb = GetComponent<Rigidbody2D>();

            // 获取 Collider2D 组件
            coll2D = GetComponent<Collider2D>();

            // 缓存原始碰撞体尺寸和偏移
            if (coll2D is CapsuleCollider2D capsule)
            {
                normalColliderSize = capsule.size;
                normalColliderOffset = capsule.offset;
            }
            else if (coll2D is BoxCollider2D box)
            {
                normalColliderSize = box.size;
                normalColliderOffset = box.offset;
            }
        }

        #region 速度控制

        /// <summary>
        /// 设置水平速度，保持竖直速度不变
        /// </summary>
        /// <param name="velocity">目标水平速度</param>
        public void SetHorizontalVelocity(float velocity)
        {
            rb.velocity = new Vector2(velocity, rb.velocity.y);
        }

        /// <summary>
        /// 设置竖直速度，保持水平速度不变
        /// </summary>
        /// <param name="velocity">目标竖直速度</param>
        public void SetVerticalVelocity(float velocity)
        {
            rb.velocity = new Vector2(rb.velocity.x, velocity);
        }

        /// <summary>
        /// 增加水平速度增量，保持竖直速度不变
        /// </summary>
        /// <param name="delta">速度增量</param>
        public void AddHorizontalVelocity(float delta)
        {
            rb.velocity = new Vector2(rb.velocity.x + delta, rb.velocity.y);
        }

        /// <summary>
        /// 增加竖直速度增量，保持水平速度不变
        /// </summary>
        /// <param name="delta">速度增量</param>
        public void AddVerticalVelocity(float delta)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y + delta);
        }

        /// <summary>
        /// 应用跳跃力（设置竖直速度）
        /// </summary>
        /// <param name="jumpForce">跳跃力度</param>
        public void ApplyJump(float jumpForce)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        #endregion

        #region 重力控制

        /// <summary>
        /// 设置重力缩放系数
        /// </summary>
        /// <param name="scale">重力缩放值（1=正常，0=无重力，用于攀爬）</param>
        public void SetGravityScale(float scale)
        {
            rb.gravityScale = scale;
        }

        #endregion

        #region 碰撞体调整

        /// <summary>
        /// 调整碰撞体尺寸和偏移
        /// 支持 CapsuleCollider2D 和 BoxCollider2D
        /// </summary>
        /// <param name="newSize">新的碰撞体尺寸</param>
        /// <param name="newOffset">新的碰撞体偏移</param>
        public void ResizeCollider(Vector2 newSize, Vector2 newOffset)
        {
            if (coll2D is CapsuleCollider2D capsule)
            {
                capsule.size = newSize;
                capsule.offset = newOffset;
            }
            else if (coll2D is BoxCollider2D box)
            {
                box.size = newSize;
                box.offset = newOffset;
            }
        }

        /// <summary>
        /// 恢复碰撞体到原始尺寸和偏移
        /// 用于从下蹲状态恢复到站立状态
        /// </summary>
        public void RestoreColliderSize()
        {
            ResizeCollider(normalColliderSize, normalColliderOffset);
        }

        /// <summary>
        /// 检查是否有足够的空间从下蹲站起
        /// </summary>
        /// <param name="checkSize">用于检测的尺寸</param>
        /// <returns>如果有足够空间则返回true</returns>
        public bool HasHeadroom(Vector2 checkSize)
        {
            if (coll2D is CapsuleCollider2D capsule)
            {
                // 从当前位置向上投射，检查是否有障碍物
                Vector2 topPosition = (Vector2)transform.position + Vector2.up * normalColliderSize.y * 0.5f;
                RaycastHit2D hit = Physics2D.Raycast(topPosition, Vector2.up, checkSize.y, LayerMask.GetMask("Default"));
                return !hit.collider;
            }
            return true;
        }

        #endregion

        #region 方向控制

        /// <summary>
        /// 翻转角色方向
        /// 同时更新 facingRight 状态和旋转角度
        /// </summary>
        public void Flip()
        {
            facingRight = !facingRight;
            transform.Rotate(0f, 180f, 0f);
        }

        /// <summary>
        /// 角色是否面向右侧的属性
        /// </summary>
        public bool FacingRight => facingRight;

        /// <summary>
        /// 获取角色面向的方向
        /// </summary>
        /// <returns>1表示面向右侧，-1表示面向左侧</returns>
        public float GetFacingDirection()
        {
            return facingRight ? 1f : -1f;
        }

        #endregion

        #region 状态查询

        /// <summary>
        /// 获取当前水平速度的绝对值
        /// </summary>
        /// <returns>水平速度大小</returns>
        public float GetHorizontalSpeed()
        {
            return Mathf.Abs(rb.velocity.x);
        }

        /// <summary>
        /// 获取当前竖直速度
        /// </summary>
        /// <returns>竖直速度值</returns>
        public float GetVerticalSpeed()
        {
            return rb.velocity.y;
        }

        /// <summary>
        /// 获取当前速度向量
        /// </summary>
        /// <returns>速度向量</returns>
        public Vector2 GetVelocity()
        {
            return rb.velocity;
        }

        /// <summary>
        /// 获取归一化的水平速度（0-1范围）
        /// 用于Animator的Speed参数
        /// </summary>
        /// <param name="maxSpeed">最大速度，用于归一化计算</param>
        /// <returns>0-1之间的归一化速度</returns>
        public float GetNormalizedHorizontalSpeed(float maxSpeed)
        {
            // 防止除以零
            if (maxSpeed <= 0) return 0;
            return Mathf.Abs(rb.velocity.x) / maxSpeed;
        }

        #endregion
    }
}
