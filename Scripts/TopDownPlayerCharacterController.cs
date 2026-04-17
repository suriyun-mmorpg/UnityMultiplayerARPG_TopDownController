using Insthync.CameraAndInput;
using UnityEngine;

namespace MultiplayerARPG
{
    public sealed partial class TopDownPlayerCharacterController : PlayerCharacterController
    {
        private bool _cannotSetDestination;
        private bool _getRMouseDown;
        private bool _getRMouseUp;
        private bool _getRMouse;
        private bool _lastFrameIsAiming;
        private bool _previouslyDead;

        public override void ManagedUpdate()
        {
            if (!_previouslyDead && PlayingCharacterEntity.IsDead())
                SetTarget(null, TargetActionType.None);
            pointClickSetTargetImmediately = true;
            controllerMode = PlayerCharacterControllerMode.PointClick;
            _isFollowingTarget = true;
            base.ManagedUpdate();
            _previouslyDead = PlayingCharacterEntity.IsDead();
        }

        public override void UpdatePointClickInput()
        {
            // If it's building something, not allow point click movement
            if (ConstructingBuildingEntity != null)
                return;

            bool isPointerOverUI = UISceneGameplay != null && UISceneGameplay.IsPointerOverUIObject();
            if (isPointerOverUI)
                return;

            // Temp mouse input value
            _getMouseDown = InputManager.GetMouseButtonDown(0);
            _getMouseUp = InputManager.GetMouseButtonUp(0);
            _getMouse = InputManager.GetMouseButton(0);
            _getRMouseDown = InputManager.GetMouseButtonDown(1);
            _getRMouseUp = InputManager.GetMouseButtonUp(1);
            _getRMouse = InputManager.GetMouseButton(1);

            // Prepare temp variables
            bool foundTargetEntity = false;
            Transform tempTransform;
            Vector3 tempVector3;
            int tempCount;

            // Clear target
            if (_getMouseDown)
                _didActionOnTarget = false;

            tempCount = FindClickObjects(out tempVector3);
            for (int tempCounter = 0; tempCounter < tempCount; ++tempCounter)
            {
                tempTransform = _physicFunctions.GetRaycastTransform(tempCounter);
                _targetPosition = _physicFunctions.GetRaycastPoint(tempCounter);
                ITargetableEntity targetable = tempTransform.GetComponent<ITargetableEntity>();
                IActivatableEntity clickActivatable = targetable as IActivatableEntity;
                IHoldActivatableEntity rightClickActivatable = targetable as IHoldActivatableEntity;
                IDamageableEntity damageable = targetable as IDamageableEntity;
                if (!targetable.IsNull())
                {
                    if (!_getMouse)
                    {
                        if (damageable.IsNull() || !damageable.IsDeadOrHideFrom(PlayingCharacterEntity))
                        {
                            // Mouse cursor hover on entity
                            foundTargetEntity = true;
                            if (!TargetEntity.IsNull())
                                SelectedEntity = TargetEntity;
                            else
                                SelectedEntity = targetable;
                        }
                    }
                    if (_getMouseDown)
                    {
                        if (!clickActivatable.IsNull() && clickActivatable.CanActivate())
                        {
                            // Clicked on entity
                            foundTargetEntity = true;
                            if (clickActivatable.ShouldBeAttackTarget())
                                SetTarget(clickActivatable, TargetActionType.Attack);
                            else
                                SetTarget(clickActivatable, TargetActionType.ClickActivate);
                        }
                        else if (!damageable.IsNull() && !damageable.IsDeadOrHideFrom(PlayingCharacterEntity) && damageable.CanReceiveDamageFrom(PlayingCharacterEntity.GetInfo()))
                        {
                            // Clicked on entity
                            foundTargetEntity = true;
                            SetTarget(damageable, TargetActionType.Attack);
                        }
                    }
                    if (_getRMouseDown)
                    {
                        if (!rightClickActivatable.IsNull() && rightClickActivatable.CanHoldActivate())
                        {
                            // Right-clicked on entity
                            foundTargetEntity = true;
                            SetTarget(rightClickActivatable, TargetActionType.HoldClickActivate);
                        }
                    }
                }
                if (foundTargetEntity)
                    break;
            }

            if (_getMouseUp && TargetEntity == null)
            {
                // Mouse release while cursor hover on ground
                SelectedEntity = null;
            }

            if (!_getMouse && !foundTargetEntity)
            {
                // Mouse cursor not hover on entity
                SelectedEntity = null;
            }


            if (_getMouse)
            {
                if (TargetGameEntity != null)
                {
                    // Has target so move to target not the destination
                    _cannotSetDestination = true;
                }
                else
                {
                    // Close NPC dialog, when target changes
                    HideNpcDialog();
                }

                if (_lastFrameIsAiming)
                    _cannotSetDestination = true;

                // Move to target
                if (!_cannotSetDestination && tempCount > 0)
                {
                    // When moving, find target position which mouse click on
                    _targetPosition = _physicFunctions.GetRaycastPoint(0);
                    // When clicked on map (any non-collider position)
                    // tempVector3 is come from FindClickObjects()
                    // - Clear character target to make character stop doing actions
                    // - Clear selected target to hide selected entity UIs
                    // - Set target position to position where mouse clicked
                    if (CurrentGameInstance.DimensionType == DimensionType.Dimension2D)
                    {
                        PlayingCharacterEntity.SetTargetEntity(null);
                        tempVector3.z = 0;
                        _targetPosition = tempVector3;
                    }
                    _destination = _targetPosition;
                    PlayingCharacterEntity.PointClickMovement(_targetPosition.Value);
                }
            }
            else
            {
                // Mouse released, reset states
                if (TargetGameEntity == null)
                    _cannotSetDestination = false;
            }

            _lastFrameIsAiming = AreaSkillAimController.IsAiming;
        }

        protected override void OnDoActionOnEntity()
        {
            if (!_getMouse && !_getRMouse)
            {
                // Clear target when player release mouse button
                ClearTarget(true);
            }
        }

        protected override void OnAttackOnEntity()
        {
            if (!_getMouse && !_getRMouse)
            {
                // Clear target when player release mouse button
                ClearTarget(true);
            }
        }

        protected override void OnUseSkillOnEntity()
        {
            if (!_getMouse && !_getRMouse)
            {
                // Clear target when player release mouse button
                ClearTarget(true);
            }
        }

        protected override void SetTarget(ITargetableEntity entity, TargetActionType targetActionType, bool checkControllerMode = true)
        {
            _targetActionType = targetActionType;
            _destination = null;
            TargetEntity = entity;
            if (entity is IGameEntity)
                PlayingCharacterEntity.SetTargetEntity((entity as IGameEntity).Entity);
        }
    }
}
