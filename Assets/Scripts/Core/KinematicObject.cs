using UnityEngine;

namespace Core
{
    /// <summary>
    /// Implements game physics for some in game entity.
    /// </summary>
    public abstract class KinematicObject : MonoBehaviour
    {
        private const float MinGroundNormalY = .65f;
        private const float GravityModifier = 1f;
        private const float MinMoveDistance = 0.001f;
        private const float ShellRadius = 0.01f;

        [SerializeField] private Rigidbody2D _rigidbody2D;

        protected Vector2 _velocity;
        protected Vector2 _targetVelocity;
        protected bool _isGrounded;

        private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[16];
        private Vector2 _groundNormal;
        private ContactFilter2D _contactFilter;

        protected virtual void OnEnable()
        {
            _rigidbody2D.isKinematic = true;
        }

        protected virtual void OnDisable()
        {
            _rigidbody2D.isKinematic = false;
        }

        protected virtual void Start()
        {
            _contactFilter.useTriggers = false;
            _contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
            _contactFilter.useLayerMask = true;
        }

        protected virtual void Update()
        {
            _targetVelocity = Vector2.zero;
            ComputeVelocity();
        }

        public void Teleport(Vector3 position)
        {
            _rigidbody2D.position = position;
            _velocity *= 0;
            _rigidbody2D.velocity *= 0;
        }

        protected abstract void ComputeVelocity();

        protected virtual void FixedUpdate()
        {
            IncreaseFallingVelocity();
            _velocity.x = _targetVelocity.x;
            _isGrounded = false;
            var deltaPosition = _velocity * Time.deltaTime;
            var moveAlongGround = new Vector2(_groundNormal.y, -_groundNormal.x);
            var move = moveAlongGround * deltaPosition.x;
            PerformMovement(move, false);
            move = Vector2.up * deltaPosition.y;
            PerformMovement(move, true);
        }

        private void IncreaseFallingVelocity()
        {
            var gravityVelocity = Physics2D.gravity * Time.deltaTime;
            //if already falling, fall faster than the jump speed, otherwise use normal gravity.
            if (_velocity.y < 0)
            {
                gravityVelocity *= GravityModifier;
            }

            _velocity += gravityVelocity;
        }

        private void PerformMovement(Vector2 move, bool yMovement)
        {
            var distance = move.magnitude;
            distance = RecalculateDistance(move, yMovement, distance);
            _rigidbody2D.position += move.normalized * distance;
        }

        private float RecalculateDistance(Vector2 move, bool yMovement, float distance)
        {
            if (distance <= MinMoveDistance)
            {
                return distance;
            }

            //check if we hit anything in current direction of travel
            var hitCount = _rigidbody2D.Cast(move, _contactFilter, _hitBuffer, distance + ShellRadius);
            for (var i = 0; i < hitCount; i++)
            {
                distance = IterateHits(yMovement, distance, i);
            }

            return distance;
        }

        private float IterateHits(bool yMovement, float distance, int i)
        {
            var currentNormal = _hitBuffer[i].normal;
            currentNormal = Ground(yMovement, currentNormal);
            RecountVelocityOnHills(currentNormal);
            //remove shellDistance from actual move distance.
            var modifiedDistance = _hitBuffer[i].distance - ShellRadius;
            distance = modifiedDistance < distance ? modifiedDistance : distance;
            return distance;
        }

        private void RecountVelocityOnHills(Vector2 currentNormal)
        {
            if (!_isGrounded)
            {
                //We are airborne, but hit something, so cancel vertical up and horizontal velocity.
                _velocity.x *= 0;
                _velocity.y = Mathf.Min(_velocity.y, 0);
                return;
            }

            //how much of our velocity aligns with surface normal?
            var projection = Vector2.Dot(_velocity, currentNormal);
            if (projection < 0)
            {
                //slower velocity if moving against the normal (up a hill).
                _velocity -= projection * currentNormal;
            }
        }

        private Vector2 Ground(bool yMovement, Vector2 currentNormal)
        {
            //is this surface flat enough to land on?
            if (currentNormal.y <= MinGroundNormalY)
            {
                return currentNormal;
            }

            _isGrounded = true;
            // if moving up, change the groundNormal to new surface normal.
            if (!yMovement)
            {
                return currentNormal;
            }

            _groundNormal = currentNormal;
            currentNormal.x = 0;

            return currentNormal;
        }
    }
}