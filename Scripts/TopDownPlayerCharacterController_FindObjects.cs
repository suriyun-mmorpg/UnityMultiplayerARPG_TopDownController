using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerARPG
{
    public partial class TopDownPlayerCharacterController
    {
        public const int RAYCAST_COLLIDER_SIZE = 32;
        public const int OVERLAP_COLLIDER_SIZE = 32;
        protected RaycastHit[] raycasts = new RaycastHit[RAYCAST_COLLIDER_SIZE];
        protected Collider[] overlapColliders = new Collider[OVERLAP_COLLIDER_SIZE];
        protected RaycastHit2D[] raycasts2D = new RaycastHit2D[RAYCAST_COLLIDER_SIZE];
        protected Collider2D[] overlapColliders2D = new Collider2D[OVERLAP_COLLIDER_SIZE];
        protected GameObject tempGameObject;
        protected Transform tempTransform;
        protected Vector3 tempVector3;
        protected int tempCount;
        protected int tempCounter;

        public int FindClickObjects(out Vector3 worldPointFor2D)
        {
            worldPointFor2D = Vector3.zero;
            if (dimensionType == DimensionType.Dimension3D)
                return Physics.RaycastNonAlloc(Camera.main.ScreenPointToRay(Input.mousePosition), raycasts, 100f, gameInstance.GetTargetLayerMask());
            worldPointFor2D = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            return Physics2D.LinecastNonAlloc(worldPointFor2D, worldPointFor2D, raycasts2D, gameInstance.GetTargetLayerMask());
        }

        public void FindAndSetBuildingAreaFromMousePosition()
        {
            tempCount = 0;
            switch (dimensionType)
            {
                case DimensionType.Dimension3D:
                    tempCount = Physics.RaycastNonAlloc(Camera.main.ScreenPointToRay(Input.mousePosition), raycasts, 100f, gameInstance.GetBuildLayerMask());
                    break;
                case DimensionType.Dimension2D:
                    tempVector3 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    tempCount = Physics2D.LinecastNonAlloc(tempVector3, tempVector3, raycasts2D, gameInstance.GetBuildLayerMask());
                    break;
            }
            LoopSetBuildingArea(tempCount);
        }

        public void FindAndSetBuildingAreaFromCharacterDirection()
        {
            if (currentBuildingEntity == null)
                return;
            tempCount = 0;
            switch (dimensionType)
            {
                case DimensionType.Dimension3D:
                    tempVector3 = CharacterTransform.position + (CharacterTransform.forward * currentBuildingEntity.characterForwardDistance);
                    currentBuildingEntity.CacheTransform.eulerAngles = GetBuildingPlaceEulerAngles(CharacterTransform.eulerAngles);
                    currentBuildingEntity.buildingArea = null;
                    tempCount = Physics.RaycastNonAlloc(new Ray(tempVector3 + (Vector3.up * 2.5f), Vector3.down), raycasts, 5f, gameInstance.GetBuildLayerMask());
                    break;
                case DimensionType.Dimension2D:
                    // TODO: implement this
                    break;
            }

            if (!LoopSetBuildingArea(tempCount))
                currentBuildingEntity.CacheTransform.position = GetBuildingPlacePosition(tempVector3);
        }

        private bool LoopSetBuildingArea(int count)
        {
            BuildingArea nonSnapBuildingArea = null;
            for (tempCounter = 0; tempCounter < count; ++tempCounter)
            {
                tempTransform = GetRaycastTransform(tempCounter);
                tempVector3 = GetRaycastPoint(tempCounter);
                if (Vector3.Distance(tempVector3, CharacterTransform.position) > gameInstance.buildDistance)
                    return false;

                BuildingArea buildingArea = tempTransform.GetComponent<BuildingArea>();
                if (buildingArea == null || (buildingArea.buildingEntity != null && buildingArea.buildingEntity == currentBuildingEntity))
                    continue;

                if (currentBuildingEntity.buildingType.Equals(buildingArea.buildingType))
                {
                    currentBuildingEntity.CacheTransform.position = GetBuildingPlacePosition(tempVector3);
                    currentBuildingEntity.buildingArea = buildingArea;
                    if (buildingArea.snapBuildingObject)
                        return true;
                    nonSnapBuildingArea = buildingArea;
                }
            }
            if (nonSnapBuildingArea != null)
                return true;
            return false;
        }

        public Transform GetRaycastTransform(int index)
        {
            if (dimensionType == DimensionType.Dimension3D)
                return raycasts[index].transform;
            return raycasts2D[index].transform;
        }

        public Vector3 GetRaycastPoint(int index)
        {
            if (dimensionType == DimensionType.Dimension3D)
                return raycasts[index].point;
            return raycasts2D[index].point;
        }

        public int OverlapObjects(Vector3 position, float distance, int layerMask)
        {
            if (dimensionType == DimensionType.Dimension3D)
                return Physics.OverlapSphereNonAlloc(position, distance, overlapColliders, layerMask);
            return Physics2D.OverlapCircleNonAlloc(position, distance, overlapColliders2D, layerMask);
        }

        public GameObject GetOverlapObject(int index)
        {
            if (dimensionType == DimensionType.Dimension3D)
                return overlapColliders[index].gameObject;
            return overlapColliders2D[index].gameObject;
        }

        public bool FindTarget(GameObject target, float actDistance, int layerMask)
        {
            tempCount = OverlapObjects(CharacterTransform.position, actDistance, layerMask);
            for (tempCounter = 0; tempCounter < tempCount; ++tempCounter)
            {
                tempGameObject = GetOverlapObject(tempCounter);
                if (tempGameObject == target)
                    return true;
            }
            return false;
        }
    }
}
