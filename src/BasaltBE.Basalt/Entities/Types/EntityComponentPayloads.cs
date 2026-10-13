using System.Text.Json.Serialization;
using BasaltBE.Basalt.Entities.Types.Components;

namespace BasaltBE.Basalt.Entities.Types;

public sealed class EntityComponentPayloads {
    [JsonInclude, JsonPropertyName("minecraft:attack")]
    public EntityAttackComponent? Attack;

    [JsonInclude, JsonPropertyName("minecraft:behavior.avoid_mob_type")]
    public EntityAvoidMobTypeBehaviorComponent? AvoidMobType;

    [JsonInclude, JsonPropertyName("minecraft:behavior.look_at_player")]
    public EntityLookAtPlayerBehaviorComponent? LookAtPlayer;

    [JsonInclude, JsonPropertyName("minecraft:behavior.mount_pathing")]
    public EntityMountPathingBehaviorComponent? MountPathing;

    [JsonInclude, JsonPropertyName("minecraft:behavior.nearest_attackable_target")]
    public EntityNearestAttackableTargetBehaviorComponent? NearestAttackableTarget;

    [JsonInclude, JsonPropertyName("minecraft:behavior.panic")]
    public EntityPanicBehaviorComponent? Panic;

    [JsonInclude, JsonPropertyName("minecraft:behavior.random_look_around")]
    public EntityRandomLookAroundBehaviorComponent? RandomLookAround;

    [JsonInclude, JsonPropertyName("minecraft:behavior.random_stroll")]
    public EntityRandomStrollBehaviorComponent? RandomStroll;

    [JsonInclude, JsonPropertyName("minecraft:behavior.tempt")]
    public EntityTemptBehaviorComponent? Tempt;

    [JsonInclude, JsonPropertyName("minecraft:breathable")]
    public EntityBreathableComponent? Breathable;

    [JsonInclude, JsonPropertyName("minecraft:burns_in_daylight")]
    public EntityBurnsInDaylightComponent? BurnsInDaylight;

    [JsonInclude, JsonPropertyName("minecraft:collision_box")]
    public EntityCollisionBoxComponent? CollisionBox;

    [JsonInclude, JsonPropertyName("minecraft:health")]
    public EntityHealthComponent? Health;

    [JsonInclude, JsonPropertyName("minecraft:inventory")]
    public EntityInventoryComponent? Inventory;

    [JsonInclude, JsonPropertyName("minecraft:movement")]
    public EntityMovementComponent? Movement;

    [JsonInclude, JsonPropertyName("minecraft:movement.amphibious")]
    public EntityMovementAmphibiousComponent? MovementAmphibious;

    [JsonInclude, JsonPropertyName("minecraft:movement.basic")]
    public EntityMovementBasicComponent? MovementBasic;

    [JsonInclude, JsonPropertyName("minecraft:movement.fly")]
    public EntityMovementFlyComponent? MovementFly;

    [JsonInclude, JsonPropertyName("minecraft:movement.generic")]
    public EntityMovementGenericComponent? MovementGeneric;

    [JsonInclude, JsonPropertyName("minecraft:movement.glide")]
    public EntityMovementGlideComponent? MovementGlide;

    [JsonInclude, JsonPropertyName("minecraft:movement.jump")]
    public EntityMovementJumpComponent? MovementJump;

    [JsonInclude, JsonPropertyName("minecraft:movement.skip")]
    public EntityMovementSkipComponent? MovementSkip;

    [JsonInclude, JsonPropertyName("minecraft:movement.sway")]
    public EntityMovementSwayComponent? MovementSway;

    [JsonInclude, JsonPropertyName("minecraft:player.exhaustion")]
    public EntityPlayerExhaustionComponent? PlayerExhaustion;

    [JsonInclude, JsonPropertyName("minecraft:player.experience")]
    public EntityPlayerExperienceComponent? PlayerExperience;

    [JsonInclude, JsonPropertyName("minecraft:player.level")]
    public EntityPlayerLevelComponent? PlayerLevel;

    [JsonInclude, JsonPropertyName("minecraft:player.saturation")]
    public EntityPlayerSaturationComponent? PlayerSaturation;

    [JsonInclude, JsonPropertyName("minecraft:rideable")]
    public EntityRideableComponent? Rideable;

    [JsonInclude, JsonPropertyName("minecraft:scale")]
    public EntityScaleComponent? Scale;

    [JsonInclude, JsonPropertyName("minecraft:type_family")]
    public EntityTypeFamilyComponent? TypeFamily;

    [JsonInclude, JsonPropertyName("minecraft:underwater_movement")]
    public EntityUnderwaterMovementComponent? UnderwaterMovement;
}
