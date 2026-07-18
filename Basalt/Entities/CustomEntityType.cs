namespace Basalt.Core.Entities;

using System.Text.Json;
using Basalt.Core.Entities.Traits;

/// <summary>
/// Seat definition for a rideable custom entity.
/// </summary>
public sealed class CustomEntitySeatOptions
{
  /// <summary>
  /// Position offset of the seat relative to the entity (x, y, z).
  /// </summary>
  public required float[] Position { get; init; }

  /// <summary>
  /// Lock the rider's rotation to this value. Null means no lock.
  /// </summary>
  public float? LockRiderRotation { get; init; }
}

/// <summary>
/// Rideable component options for a custom entity.
/// </summary>
public sealed class CustomEntityRideableOptions
{
  /// <summary>
  /// Number of seats. Defaults to the length of Seats if provided.
  /// </summary>
  public int? SeatCount { get; init; }

  /// <summary>
  /// Seat definitions. If null, a single default seat is created.
  /// </summary>
  public IReadOnlyList<CustomEntitySeatOptions>? Seats { get; init; }

  /// <summary>
  /// Interaction text shown to players (e.g. "action.interact.ride.horse").
  /// </summary>
  public string? InteractText { get; init; }

  /// <summary>
  /// Family types allowed to ride (e.g. "player").
  /// </summary>
  public IReadOnlyList<string>? FamilyTypes { get; init; }
}

/// <summary>
/// Health component options for a custom entity.
/// </summary>
public sealed class CustomEntityHealthOptions
{
  /// <summary>
  /// Maximum health value. Defaults to 20.
  /// </summary>
  public float Max { get; init; } = 20f;

  /// <summary>
  /// Starting health value. Defaults to Max.
  /// </summary>
  public float? Value { get; init; }
}

/// <summary>
/// Collision box component options for a custom entity.
/// </summary>
public sealed class CustomEntityCollisionBoxOptions
{
  /// <summary>
  /// Width of the collision box. Defaults to 0.6.
  /// </summary>
  public float Width { get; init; } = 0.6f;

  /// <summary>
  /// Height of the collision box. Defaults to 1.8.
  /// </summary>
  public float Height { get; init; } = 1.8f;
}

/// <summary>
/// Options for defining a custom entity type.
/// </summary>
public sealed class CustomEntityTypeOptions
{
  /// <summary>
  /// The namespaced identifier (e.g. "mynamespace:custom_mob").
  /// </summary>
  public required string Identifier { get; init; }

  /// <summary>
  /// Health component. Null means no health trait.
  /// </summary>
  public CustomEntityHealthOptions? Health { get; init; }

  /// <summary>
  /// Collision box component. Null means no collision trait.
  /// </summary>
  public CustomEntityCollisionBoxOptions? CollisionBox { get; init; }

  /// <summary>
  /// Rideable component. Null means not rideable.
  /// </summary>
  public CustomEntityRideableOptions? Rideable { get; init; }

  /// <summary>
  /// Whether this entity has gravity. Defaults to true.
  /// </summary>
  public bool HasGravity { get; init; } = true;

  /// <summary>
  /// Whether this entity has movement physics. Defaults to true.
  /// </summary>
  public bool HasMovement { get; init; } = true;

  /// <summary>
  /// Path to the loot table for this entity.
  /// </summary>
  public string? LootTable { get; init; }

  /// <summary>
  /// Additional trait types to register for this entity type.
  /// </summary>
  public IReadOnlyList<Type>? Traits { get; init; }
}

/// <summary>
/// Factory for creating and registering custom entity types.
/// Register custom entities during plugin OnLoad before players connect.
/// </summary>
public static class CustomEntityType
{
  /// <summary>
  /// Creates and registers a new custom entity type.
  /// </summary>
  public static EntityType Create(CustomEntityTypeOptions options)
  {
    if (string.IsNullOrWhiteSpace(options.Identifier))
    {
      throw new ArgumentException("Entity identifier cannot be empty.", nameof(options));
    }

    if (EntityType.Get(options.Identifier) is not null)
    {
      throw new InvalidOperationException($"Entity type '{options.Identifier}' is already registered.");
    }

    List<string> components = BuildComponentList(options);
    EntityPropertiesPayloadData propertiesPayload = BuildPropertiesPayload(options);

    EntityType type = new(
      options.Identifier,
      components,
      propertiesPayload,
      options.LootTable);

    if (options.Traits is { Count: > 0 })
    {
      for (int i = 0; i < options.Traits.Count; i++)
      {
        Type traitType = options.Traits[i];
        if (!typeof(EntityTrait).IsAssignableFrom(traitType) || traitType.IsAbstract)
        {
          continue;
        }

        string identifier = GetTraitIdentifier(traitType);
        type.RegisterTrait(traitType, identifier);
      }
    }

    return type;
  }

  private static List<string> BuildComponentList(CustomEntityTypeOptions options)
  {
    List<string> components = [];

    if (options.Health is not null)
    {
      components.Add("minecraft:health");
    }

    if (options.CollisionBox is not null)
    {
      components.Add("minecraft:collision_box");
    }

    if (options.Rideable is not null)
    {
      components.Add("minecraft:rideable");
    }

    if (options.HasMovement)
    {
      components.Add("minecraft:movement.basic");
    }

    return components;
  }

  private static EntityPropertiesPayloadData BuildPropertiesPayload(CustomEntityTypeOptions options)
  {
    Dictionary<string, JsonElement> componentProperties = [];

    if (options.Health is not null)
    {
      float max = options.Health.Max;
      float value = options.Health.Value ?? max;
      componentProperties["minecraft:health"] = JsonSerializer.SerializeToElement(
        new { value, max });
    }

    if (options.CollisionBox is not null)
    {
      componentProperties["minecraft:collision_box"] = JsonSerializer.SerializeToElement(
        new { width = options.CollisionBox.Width, height = options.CollisionBox.Height });
    }

    if (options.Rideable is not null)
    {
      componentProperties["minecraft:rideable"] = BuildRideableElement(options.Rideable);
    }

    return new EntityPropertiesPayloadData
    {
      Components = componentProperties,
      ComponentGroups = []
    };
  }

  private static JsonElement BuildRideableElement(CustomEntityRideableOptions rideable)
  {
    var seats = rideable.Seats ?? [new CustomEntitySeatOptions { Position = [0f, 1f, 0f] }];
    int seatCount = rideable.SeatCount ?? seats.Count;

    var seatData = new List<object>(seats.Count);
    for (int i = 0; i < seats.Count; i++)
    {
      CustomEntitySeatOptions seat = seats[i];
      if (seat.LockRiderRotation.HasValue)
      {
        seatData.Add(new { position = seat.Position, lock_rider_rotation = seat.LockRiderRotation.Value });
      }
      else
      {
        seatData.Add(new { position = seat.Position });
      }
    }

    return JsonSerializer.SerializeToElement(new
    {
      seat_count = seatCount,
      seats = seatData,
      interact_text = rideable.InteractText ?? string.Empty,
      family_types = rideable.FamilyTypes ?? (IReadOnlyList<string>)[]
    });
  }

  private static string GetTraitIdentifier(Type traitType)
  {
    var property = traitType.GetProperty(
      "Identifier",
      System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

    if (property is not null &&
      property.PropertyType == typeof(string) &&
      property.GetValue(null) is string id &&
      !string.IsNullOrWhiteSpace(id))
    {
      return id;
    }

    return traitType.FullName ?? traitType.Name;
  }
}
