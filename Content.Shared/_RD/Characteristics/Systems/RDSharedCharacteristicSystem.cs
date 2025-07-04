﻿using Content.Shared._RD.Characteristics.Components;
using Content.Shared._RD.Characteristics.Events;
using Content.Shared._RD.Mathematics.Random;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RD.Characteristics.Systems;

public abstract class RDSharedCharacteristicSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RDCharacteristicContainerComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<RDCharacteristicContainerComponent> entity, ref ComponentStartup args)
    {
        entity.Comp.Random = new Xoshiro256(GetSeed(GetNetEntity(entity).Id));
    }

    public void Set(Entity<RDCharacteristicContainerComponent?> entity,
        ProtoId<RDCharacteristicPrototype> id,
        int value)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return;

        entity.Comp.Values[id] = value;
        Refresh(entity);

        if (_net.IsServer)
            DirtyField(entity, entity.Comp, nameof(RDCharacteristicContainerComponent.Values));
    }

    public int Get(Entity<RDCharacteristicContainerComponent?> entity,
        ProtoId<RDCharacteristicPrototype> id)
    {
        return !Resolve(entity, ref entity.Comp, logMissing: false)
            ? RDCharacteristicContainerComponent.DefaultValue
            : entity.Comp.ModifiedValues.GetValueOrDefault(id, RDCharacteristicContainerComponent.DefaultValue);
    }

    public int GetRaw(Entity<RDCharacteristicContainerComponent?> entity,
        ProtoId<RDCharacteristicPrototype> id)
    {
        return !Resolve(entity, ref entity.Comp, logMissing: false)
            ? RDCharacteristicContainerComponent.DefaultValue
            : entity.Comp.Values.GetValueOrDefault(id, RDCharacteristicContainerComponent.DefaultValue);
    }

    public IReadOnlyDictionary<ProtoId<RDCharacteristicPrototype>, int> GetAll(Entity<RDCharacteristicContainerComponent?> entity)
    {
        return !Resolve(entity, ref entity.Comp, logMissing: false)
            ? new Dictionary<ProtoId<RDCharacteristicPrototype>, int>()
            : entity.Comp.ModifiedValues;
    }

    public bool Check(Entity<RDCharacteristicContainerComponent?> entity,
        ProtoId<RDCharacteristicPrototype> id,
        int difficulty)
    {
        return Check(entity, id, difficulty, out _, out _);
    }

    public bool Check(Entity<RDCharacteristicContainerComponent?> entity,
        ProtoId<RDCharacteristicPrototype> id,
        int difficulty,
        out int modifier,
        out int checkValue)
    {
        modifier = 0;
        checkValue = 0;

        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return true;

        if (!_prototype.TryIndex(id, out var prototype))
            return true;

        var value = Get(entity, id);
        modifier = GetModifier(prototype, value);

        checkValue = entity.Comp.Random.NextInt(RDCharacteristicPrototype.Min, prototype.Max);

        return checkValue + modifier >= difficulty;
    }

    public void Refresh(Entity<RDCharacteristicContainerComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return;

        foreach (var (type, value) in entity.Comp.Values)
        {
            var ev = new RDGetCharacteristicModifiersEvent((entity, entity.Comp), type);
            RaiseLocalEvent(entity, ev);

            entity.Comp.ModifiedValues[type] = (int) Math.Floor((value + ev.ValueAdditional) * ev.ValueMultiplier);
        }

        if (_net.IsServer)
            DirtyField(entity, entity.Comp, nameof(RDCharacteristicContainerComponent.ModifiedValues));
    }

    private static long GetSeed(int start)
    {
        var seed = (long) start;
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return seed;
    }

    private static int GetModifier(RDCharacteristicPrototype prototype, int value)
    {
        return (int) Math.Floor((value - prototype.Medium) / 2f);
    }
}
