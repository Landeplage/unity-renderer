namespace DCL.ECSComponents
{
    public static class PBMeshCollider_Defaults
    {
        public static float GetTopRadius(this PBMeshCollider.Types.CylinderMesh self)
        {
            return self.HasRadiusTop ? self.RadiusTop : 0.5f;
        }

        public static float GetBottomRadius(this PBMeshCollider.Types.CylinderMesh self)
        {
            return self.HasRadiusBottom ? self.RadiusBottom : 0.5f;
        }

        public static int GetColliderLayer(this PBMeshCollider self)
        {
            // TODO: the protocol is up to date and CollisionMask is `uint` instead of `int`, check this
            return self.HasCollisionMask ? (int)self.CollisionMask : ((int)ColliderLayer.ClPhysics | (int)ColliderLayer.ClPointer);
        }
    }
}
