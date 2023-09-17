using System;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    interface IPhysicsMaterialProperties
    {
        CollisionResponsePolicy CollisionResponse { get; set; }

        PhysicsMaterialCoefficient Friction { get; set; }

        PhysicsMaterialCoefficient Restitution { get; set; }

        PhysicsCategoryTags BelongsTo { get; set; }

        PhysicsCategoryTags CollidesWith { get; set; }

        // TODO: Enable Mass Factors?
        // TODO: Surface Velocity?

        CustomPhysicsMaterialTags CustomTags { get; set; }
    }

    interface IInheritPhysicsMaterialProperties : IPhysicsMaterialProperties
    {
        PhysicsMaterialTemplate Template { get; set; }
        bool OverrideCollisionResponse { get; set; }
        bool OverrideFriction { get; set; }
        bool OverrideRestitution { get; set; }
        bool OverrideBelongsTo { get; set; }
        bool OverrideCollidesWith { get; set; }
        bool OverrideCustomTags { get; set; }
    }

    [Serializable]
    public struct PhysicsMaterialCoefficient
    {
        [SoftRange(0f, 1f, TextFieldMax = float.MaxValue)]
        public float Value;

        public Material.CombinePolicy CombineMode;
    }

    abstract class OverridableValue<T> where T : struct
    {
        [SerializeField] bool m_Override;

        [SerializeField] T m_Value;

        public bool Override
        {
            get => m_Override;
            set => m_Override = value;
        }

        public T Value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                Override = true;
            }
        }

        public void OnValidate() => OnValidate(ref m_Value);

        protected virtual void OnValidate(ref T value)
        {
        }
    }

    [Serializable]
    class OverridableCollisionResponse : OverridableValue<CollisionResponsePolicy>
    {
    }

    [Serializable]
    class OverridableMaterialCoefficient : OverridableValue<PhysicsMaterialCoefficient>
    {
        protected override void OnValidate(ref PhysicsMaterialCoefficient value) =>
            value.Value = math.max(0f, value.Value);
    }

    [Serializable]
    class OverridableCategoryTags : OverridableValue<PhysicsCategoryTags>
    {
    }

    [Serializable]
    class OverridableCustomMaterialTags : OverridableValue<CustomPhysicsMaterialTags>
    {
    }

    [Serializable]
    class PhysicsMaterialProperties : IInheritPhysicsMaterialProperties, ISerializationCallbackReceiver
    {
        const int k_LatestVersion = 1;

        internal static bool s_SuppressUpgradeWarnings;

        [SerializeField, HideInInspector] bool m_SupportsTemplate;

        [SerializeField] [Tooltip("Assign a template to use its values.")]
        PhysicsMaterialTemplate m_Template;

        [SerializeField] OverridableCollisionResponse m_CollisionResponse = new OverridableCollisionResponse
        {
            Value = CollisionResponsePolicy.Collide,
            Override = false
        };

        [SerializeField] OverridableMaterialCoefficient m_Friction = new OverridableMaterialCoefficient
        {
            Value = new PhysicsMaterialCoefficient {Value = 0.5f, CombineMode = Material.CombinePolicy.GeometricMean},
            Override = false
        };

        [SerializeField] OverridableMaterialCoefficient m_Restitution = new OverridableMaterialCoefficient
        {
            Value = new PhysicsMaterialCoefficient {Value = 0f, CombineMode = Material.CombinePolicy.Maximum},
            Override = false
        };

        [SerializeField] OverridableCategoryTags m_BelongsToCategories =
            new OverridableCategoryTags {Value = PhysicsCategoryTags.Everything, Override = false};

        [SerializeField] OverridableCategoryTags m_CollidesWithCategories =
            new OverridableCategoryTags {Value = PhysicsCategoryTags.Everything, Override = false};

        [SerializeField] OverridableCustomMaterialTags m_CustomMaterialTags =
            new OverridableCustomMaterialTags {Value = default, Override = false};

        [SerializeField] int m_SerializedVersion = 0;

        public PhysicsMaterialProperties(bool supportsTemplate) => m_SupportsTemplate = supportsTemplate;

        public PhysicsMaterialTemplate Template
        {
            get => m_Template;
            set => m_Template = m_SupportsTemplate ? value : null;
        }

        public bool OverrideCollisionResponse
        {
            get => m_CollisionResponse.Override;
            set => m_CollisionResponse.Override = value;
        }

        public CollisionResponsePolicy CollisionResponse
        {
            get => Get(m_CollisionResponse, m_Template == null ? null : m_Template?.CollisionResponse);
            set => m_CollisionResponse.Value = value;
        }

        public bool OverrideFriction
        {
            get => m_Friction.Override;
            set => m_Friction.Override = value;
        }

        public PhysicsMaterialCoefficient Friction
        {
            get => Get(m_Friction, m_Template == null ? null : m_Template?.Friction);
            set => m_Friction.Value = value;
        }

        public bool OverrideRestitution
        {
            get => m_Restitution.Override;
            set => m_Restitution.Override = value;
        }

        public PhysicsMaterialCoefficient Restitution
        {
            get => Get(m_Restitution, m_Template == null ? null : m_Template?.Restitution);
            set => m_Restitution.Value = value;
        }

        public bool OverrideBelongsTo
        {
            get => m_BelongsToCategories.Override;
            set => m_BelongsToCategories.Override = value;
        }

        public PhysicsCategoryTags BelongsTo
        {
            get => Get(m_BelongsToCategories, m_Template == null ? null : m_Template?.BelongsTo);
            set => m_BelongsToCategories.Value = value;
        }

        public bool OverrideCollidesWith
        {
            get => m_CollidesWithCategories.Override;
            set => m_CollidesWithCategories.Override = value;
        }

        public PhysicsCategoryTags CollidesWith
        {
            get => Get(m_CollidesWithCategories, m_Template == null ? null : m_Template?.CollidesWith);
            set => m_CollidesWithCategories.Value = value;
        }

        public bool OverrideCustomTags
        {
            get => m_CustomMaterialTags.Override;
            set => m_CustomMaterialTags.Override = value;
        }

        public CustomPhysicsMaterialTags CustomTags
        {
            get => Get(m_CustomMaterialTags, m_Template == null ? null : m_Template?.CustomTags);
            set => m_CustomMaterialTags.Value = value;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() => UpgradeVersionIfNecessary();

        static T Get<T>(OverridableValue<T> value, T? templateValue) where T : struct =>
            value.Override || templateValue == null ? value.Value : templateValue.Value;

        internal static void OnValidate(ref PhysicsMaterialProperties material, bool supportsTemplate)
        {
            material.UpgradeVersionIfNecessary();

            material.m_SupportsTemplate = supportsTemplate;
            if (!supportsTemplate)
            {
                material.m_Template = null;
                material.m_CollisionResponse.Override = true;
                material.m_Friction.Override = true;
                material.m_Restitution.Override = true;
            }

            material.m_Friction.OnValidate();
            material.m_Restitution.OnValidate();
        }

#pragma warning disable 618
        void UpgradeVersionIfNecessary()
        {
            if (m_SerializedVersion < k_LatestVersion)
            {
                // old data from version < 1 have been removed
                if (m_SerializedVersion < 1)
                    m_SerializedVersion = 1;
            }
        }

#pragma warning restore 618
    }
}