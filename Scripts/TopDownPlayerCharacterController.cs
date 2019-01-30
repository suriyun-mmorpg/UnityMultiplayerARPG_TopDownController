using LiteNetLibManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class TopDownPlayerCharacterController : BasePlayerCharacterController
    {
        public enum PlayerCharacterControllerMode
        {
            PointClick,
            WASD,
            Both,
        }

        public struct UsingSkillData
        {
            public Vector3? aimPosition;
            public int dataId;
            public UsingSkillData(Vector3? position, int dataId)
            {
                this.aimPosition = position;
                this.dataId = dataId;
            }
        }

        public const float DETECT_MOUSE_DRAG_DISTANCE = 10f;
        public const float DETECT_MOUSE_HOLD_DURATION = 1f;
        public PlayerCharacterControllerMode controllerMode;
        [Tooltip("Set this to TRUE to find nearby enemy and look to it while attacking when `Controller Mode` is `WASD`")]
        public bool wasdLockAttackTarget;
        [Tooltip("This will be used to find nearby enemy when `Controller Mode` is `Point Click` or when `Wasd Lock Attack Target` is `TRUE`")]
        public float lockAttackTargetDistance = 10f;
        public FollowCameraControls gameplayCameraPrefab;
        public GameObject targetObjectPrefab;
        [Header("Building Settings")]
        public bool buildGridSnap;
        public float buildGridSize = 4f;
        public bool buildRotationSnap;

        protected Vector3? destination;
        protected UsingSkillData? queueUsingSkill;
        protected Vector3 mouseDownPosition;
        protected float mouseDownTime;
        protected bool isMouseDragOrHoldOrOverUI;
        protected uint lastNpcObjectId;

        public FollowCameraControls CacheGameplayCameraControls { get; protected set; }
        public GameObject CacheTargetObject { get; protected set; }

        protected BaseGameEntity targetEntity;
        protected Vector3 targetPosition;
        // Optimizing garbage collection
        protected bool getMouseUp;
        protected bool getMouseDown;
        protected bool getMouse;
        protected bool isPointerOverUI;
        protected bool isMouseDragDetected;
        protected bool isMouseHoldDetected;
        protected bool isMouseHoldAndNotDrag;
        protected BaseCharacterEntity targetCharacter;
        protected BaseCharacterEntity targetEnemy;
        protected BasePlayerCharacterEntity targetPlayer;
        protected BaseMonsterCharacterEntity targetMonster;
        protected NpcEntity targetNpc;
        protected ItemDropEntity targetItemDrop;
        protected BuildingEntity targetBuilding;
        protected HarvestableEntity targetHarvestable;

        protected override void Awake()
        {
            base.Awake();
            buildingItemIndex = -1;
            currentBuildingEntity = null;

            if (gameplayCameraPrefab != null)
            {
                // Set parent transform to root for the best performance
                CacheGameplayCameraControls = Instantiate(gameplayCameraPrefab);
            }
            if (targetObjectPrefab != null)
            {
                // Set parent transform to root for the best performance
                CacheTargetObject = Instantiate(targetObjectPrefab);
                CacheTargetObject.SetActive(false);
            }
        }

        protected override void Setup(BasePlayerCharacterEntity characterEntity)
        {
            base.Setup(characterEntity);

            if (characterEntity == null)
                return;

            if (CacheGameplayCameraControls != null)
                CacheGameplayCameraControls.target = characterEntity.CacheTransform;
        }

        protected override void Desetup(BasePlayerCharacterEntity characterEntity)
        {
            base.Desetup(characterEntity);

            if (CacheGameplayCameraControls != null)
                CacheGameplayCameraControls.target = null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (CacheGameplayCameraControls != null)
                Destroy(CacheGameplayCameraControls.gameObject);
            if (CacheTargetObject != null)
                Destroy(CacheTargetObject.gameObject);
        }

        protected override void Update()
        {
            if (PlayerCharacterEntity == null || !PlayerCharacterEntity.IsOwnerClient)
                return;

            base.Update();

            if (CacheTargetObject != null)
                CacheTargetObject.gameObject.SetActive(destination.HasValue);

            if (PlayerCharacterEntity.IsDead())
            {
                queueUsingSkill = null;
                destination = null;
                if (CacheUISceneGameplay != null)
                    CacheUISceneGameplay.SetTargetEntity(null);
                CancelBuild();
            }
            else
            {
                if (CacheUISceneGameplay != null)
                    CacheUISceneGameplay.SetTargetEntity(PlayerCharacterEntity.GetTargetEntity());
            }

            if (destination.HasValue)
            {
                if (CacheTargetObject != null)
                    CacheTargetObject.transform.position = destination.Value;
                if (Vector3.Distance(destination.Value, CharacterTransform.position) < StoppingDistance + 0.5f)
                    destination = null;
            }

            UpdateInput();
            UpdateFollowTarget();
        }

        private Vector3 GetBuildingPlacePosition(Vector3 position)
        {
            if (buildGridSnap)
                position = new Vector3(Mathf.Round(position.x / buildGridSize) * buildGridSize, position.y, Mathf.Round(position.z / buildGridSize) * buildGridSize);
            return position;
        }

        private Vector3 GetBuildingPlaceEulerAngles(Vector3 eulerAngles)
        {
            eulerAngles.x = 0;
            eulerAngles.z = 0;
            // Make Y rotation set to 0, 90, 180
            if (buildRotationSnap)
                eulerAngles.y = Mathf.Round(eulerAngles.y / 90) * 90;
            return eulerAngles;
        }

        public bool TryGetAttackingCharacter(out BaseCharacterEntity character)
        {
            character = null;
            if (PlayerCharacterEntity.TryGetTargetEntity(out character))
            {
                if (character.CanReceiveDamageFrom(PlayerCharacterEntity))
                    return true;
                else
                    character = null;
            }
            return false;
        }

        public bool GetAttackDistanceAndFov(out float attackDistance, out float attackFov)
        {
            attackDistance = PlayerCharacterEntity.GetAttackDistance();
            attackFov = PlayerCharacterEntity.GetAttackFov();
            if (queueUsingSkill.HasValue)
            {
                UsingSkillData queueUsingSkillValue = queueUsingSkill.Value;
                Skill skill = null;
                if (GameInstance.Skills.TryGetValue(queueUsingSkillValue.dataId, out skill) && skill != null)
                {
                    if (skill.IsAttack())
                    {
                        attackDistance = PlayerCharacterEntity.GetSkillAttackDistance(skill);
                        attackFov = PlayerCharacterEntity.GetSkillAttackFov(skill);
                    }
                    else
                    {
                        // Stop movement to use non attack skill
                        PlayerCharacterEntity.StopMove();
                        RequestUsePendingSkill();
                        return false;
                    }
                }
                else
                    queueUsingSkill = null;
            }
            return true;
        }

        public bool IsLockTarget()
        {
            return controllerMode == PlayerCharacterControllerMode.Both ||
                controllerMode == PlayerCharacterControllerMode.PointClick ||
                (controllerMode == PlayerCharacterControllerMode.WASD && wasdLockAttackTarget);
        }

        public Vector3 GetMoveDirection(float horizontalInput, float verticalInput)
        {
            Vector3 moveDirection = Vector3.zero;
            switch (gameInstance.DimensionType)
            {
                case DimensionType.Dimension3D:
                    moveDirection += Camera.main.transform.forward * verticalInput;
                    moveDirection += Camera.main.transform.right * horizontalInput;
                    moveDirection.y = 0;
                    break;
                case DimensionType.Dimension2D:
                    moveDirection = new Vector2(horizontalInput, verticalInput);
                    break;
            }
            return moveDirection;
        }

        public void RequestAttack()
        {
            PlayerCharacterEntity.RequestAttack();
        }

        public void RequestUseSkill(int dataId, Vector3 aimPosition)
        {
            PlayerCharacterEntity.RequestUseSkill(dataId, aimPosition);
        }

        public void RequestUsePendingSkill()
        {
            if (queueUsingSkill.HasValue)
            {
                UsingSkillData queueUsingSkillValue = queueUsingSkill.Value;
                Vector3 aimPosition = queueUsingSkillValue.aimPosition.HasValue ? queueUsingSkillValue.aimPosition.Value : CharacterTransform.position;
                RequestUseSkill(queueUsingSkillValue.dataId, aimPosition);
                queueUsingSkill = null;
            }
        }

        public void RequestEquipItem(short itemIndex)
        {
            PlayerCharacterEntity.RequestEquipItem(itemIndex);
        }

        public void RequestUseItem(short itemIndex)
        {
            PlayerCharacterEntity.RequestUseItem(itemIndex);
        }
    }
}
