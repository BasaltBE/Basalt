namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Entities.Traits.Enums;
using Basalt.Core.Entities.Traits.Types;
using BedrockProtocol.Enums;
using BedrockProtocol.Packets;
using BedrockProtocol.Types;
using System.Text.Json;
using Player = Basalt.Core.Player.Player;

public sealed class EntityRideableTrait : EntityTrait {
    public new static string Identifier => "rideable";
    public new static readonly string[] Components = ["minecraft:rideable"];

    private readonly Dictionary<int, long> _riders = [];
    private readonly List<RideableSeat> _seats = [];

    public EntityRideableTrait(Entity entity) : base(entity) {
    }

    public bool HasAvailableSeats() {
        return _riders.Count < _seats.Count;
    }

    public RideableSeat? GetNextAvailableSeat() {
        for (int i = 0; i < _seats.Count; i++) {
            if (!_riders.ContainsKey(_seats[i].Index)) {
                return _seats[i];
            }
        }

        return null;
    }

    public List<(Entity Rider, RideableSeat Seat)> GetRiders() {
        List<(Entity, RideableSeat)> result = [];

        if (Entity.Dimension is null) {
            return result;
        }

        foreach ((int seatIndex, long uniqueId) in _riders) {
            Entity? rider = FindEntityByUniqueId(uniqueId);
            if (rider is null) {
                continue;
            }

            RideableSeat? seat = FindSeatByIndex(seatIndex);
            if (seat is null) {
                continue;
            }

            result.Add((rider, seat));
        }

        return result;
    }

    public bool AddRider(Entity rider) {
        if (!HasAvailableSeats()) {
            return false;
        }

        RideableSeat? seat = GetNextAvailableSeat();
        if (seat is null) {
            return false;
        }

        SetActorLinkPacket packet = new() {
            Link = new ActorLink {
                TargetA = new ActorUniqueID {
                    Value = Entity.UniqueId
                },
                TargetB = new ActorUniqueID {
                    Value = rider.UniqueId
                },
                Type = ActorLinkType.Riding,
                Immediate = true,
                PassengerInitiated = true,
                VehicleAngularVelocity = 0f
            }
        };

        Entity.Dimension?.Broadcast(packet);
        _riders[seat.Index] = rider.UniqueId;

        EntityRidingTrait riding = new(rider, Entity, seat);
        rider.AddTrait(riding);
        rider.Flags.SetActorFlag(ActorFlag.Riding, true);

        return true;
    }

    public void RemoveRider(Entity rider) {
        SetActorLinkPacket packet = new() {
            Link = new ActorLink {
                TargetA = new ActorUniqueID {
                    Value = Entity.UniqueId
                },
                TargetB = new ActorUniqueID {
                    Value = rider.UniqueId
                },
                Type = ActorLinkType.None,
                Immediate = true,
                PassengerInitiated = true,
                VehicleAngularVelocity = 0f
            }
        };

        Entity.Dimension?.Broadcast(packet);

        foreach ((int index, long riderId) in _riders) {
            if (riderId == rider.UniqueId) {
                _riders.Remove(index);
                break;
            }
        }

        rider.Flags.SetActorFlag(ActorFlag.Riding, false);

        EntityRidingTrait? riding = rider.GetTrait<EntityRidingTrait>();
        if (riding is not null) {
            rider.RemoveTrait(riding);
        }
    }

    public void ClearRiders() {
        List<long> riderIds = [.. _riders.Values];

        foreach (long uniqueId in riderIds) {
            Entity? rider = FindEntityByUniqueId(uniqueId);
            if (rider is null) {
                continue;
            }

            RemoveRider(rider);
        }
    }

    public RideableSeat CreateSeat(
        Vec3 position,
        bool driver = false,
        float seatRotation = 0f,
        bool lockRotation = false
    ) {
        int index = _seats.Count;

        RideableSeat seat = new(
            index,
            position,
            seatRotation,
            lockRotation,
            driver
        );

        _seats.Add(seat);
        return seat;
    }

    public void RemoveSeat(RideableSeat seat) {
        _seats.Remove(seat);
    }

    public void ClearSeats() {
        _seats.Clear();
    }

    public override void OnInteract(Player player, EntityInteractMethod method) {
        if (method != EntityInteractMethod.Interact || player.IsSneaking) {
            return;
        }

        if (!HasAvailableSeats()) {
            return;
        }

        EntityRidingTrait? currentRiding = player.GetTrait<EntityRidingTrait>();
        if (currentRiding is not null) {
            currentRiding.Vehicle
                .GetTrait<EntityRideableTrait>()
                ?.RemoveRider(player);
        }

        AddRider(player);
    }

    public override void OnAdd() {
        if (_seats.Count > 0) {
            return;
        }

        if (
            Entity.Type.TryGetComponentProperties(
                "minecraft:rideable",
                out JsonElement rideable
            )
        ) {
            ParseSeatsFromJson(rideable);
        }

        if (_seats.Count == 0) {
            CreateSeat(
                new Vec3() { X = 0f, Y = 1f, Z = 0f },
                driver: true
            );
        }
    }

    public override void OnRemove() {
        ClearRiders();
        _seats.Clear();
        _riders.Clear();
    }

    public override EntityTrait Clone(Entity entity) {
        EntityRideableTrait clone = new(entity);

        for (int i = 0; i < _seats.Count; i++) {
            RideableSeat src = _seats[i];

            clone.CreateSeat(
                src.Position,
                src.Driver,
                src.SeatRotation,
                src.LockRotation
            );
        }

        return clone;
    }

    private void ParseSeatsFromJson(JsonElement rideable) {
        if (!rideable.TryGetProperty("seats", out JsonElement seatsElement)) {
            return;
        }

        if (seatsElement.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement seatElement in seatsElement.EnumerateArray()) {
                ParseSingleSeat(seatElement);
            }
        }
        else if (seatsElement.ValueKind == JsonValueKind.Object) {
            ParseSingleSeat(seatsElement);
        }
    }

    private void ParseSingleSeat(JsonElement seatElement) {
        Vec3 position = new() { X = 0f, Y = 1f, Z = 0f };
        float seatRotation = 0f;
        bool lockRotation = false;

        if (
            seatElement.TryGetProperty(
                "position",
                out JsonElement posElement
            ) &&
            posElement.ValueKind == JsonValueKind.Array
        ) {
            float[] coords = new float[3];
            int idx = 0;

            foreach (JsonElement coord in posElement.EnumerateArray()) {
                if (idx < 3 && coord.TryGetSingle(out float val)) {
                    coords[idx] = val;
                }

                idx++;
            }

            position = new Vec3() {
                X = coords[0],
                Y = coords[1],
                Z = coords[2]
            };
        }

        if (
            seatElement.TryGetProperty(
                "lock_rider_rotation",
                out JsonElement lockElement
            )
        ) {
            if (lockElement.ValueKind == JsonValueKind.Number) {
                seatRotation = lockElement.TryGetSingle(out float rot)
                    ? rot
                    : 0f;

                lockRotation = true;
            }
            else if (lockElement.ValueKind == JsonValueKind.True) {
                lockRotation = true;
            }
        }

        CreateSeat(
            position,
            driver: _seats.Count == 0,
            seatRotation,
            lockRotation
        );
    }

    private Entity? FindEntityByUniqueId(long uniqueId) {
        if (Entity.Dimension is null) {
            return null;
        }

        foreach (Entity entity in Entity.Dimension.Entities) {
            if (entity.UniqueId == uniqueId) {
                return entity;
            }
        }

        return null;
    }

    private RideableSeat? FindSeatByIndex(int index) {
        for (int i = 0; i < _seats.Count; i++) {
            if (_seats[i].Index == index) {
                return _seats[i];
            }
        }

        return null;
    }
}