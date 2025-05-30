﻿using Content.Shared._RD;
using Content.Shared.Throwing;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map.Components;

namespace Content.Client._RD.WorldSprite;

public sealed class RDWorldSpriteSystem : RDEntitySystem
{
    [Dependency] private readonly IResourceCache _resCache = default!;

    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        base.Initialize();

        _spriteQuery = GetEntityQuery<SpriteComponent>();

        SubscribeLocalEvent<RDWorldSpriteComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RDWorldSpriteComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<RDWorldSpriteComponent, ThrownEvent>(OnThrown);
    }

    private void OnComponentInit(Entity<RDWorldSpriteComponent> entity, ref ComponentInit args)
    {
        if (_spriteQuery.TryComp(entity, out var spriteComponent))
        {
            foreach (var key in entity.Comp.Layers)
            {
                if (key == "$main")
                {
                    entity.Comp.CachedTexture[key] = spriteComponent.BaseRSI;
                    continue;
                }

                if (!spriteComponent.LayerMapTryGet(key, out var index))
                    continue;

                if (!spriteComponent.TryGetLayer(index, out var layer))
                    continue;

                entity.Comp.CachedTexture[key] = layer.RSI;
            }
        }

        Update(entity);
    }

    private void OnParentChanged(Entity<RDWorldSpriteComponent> entity, ref EntParentChangedMessage args)
    {
        Update(entity);
    }

    private void OnThrown(Entity<RDWorldSpriteComponent> entity, ref ThrownEvent args)
    {
        // Idk, but throw don't call reparent
        Update(entity, args.User);
    }

    private void Update(Entity<RDWorldSpriteComponent> entity, EntityUid? parent = null)
    {
        var inWorld = IsMap(parent ?? Transform(entity).ParentUid);
        if (!_spriteQuery.TryComp(entity, out var spriteComponent))
            return;

        if (inWorld)
        {
            foreach (var key in entity.Comp.Layers)
            {
                if (!_resCache.TryGetResource<RSIResource>($"/Textures/{entity.Comp.Sprite}", out var rsiResource))
                {
                    Log.Warning($"Failed to load RSI at path '{entity.Comp.Sprite}' for layer '{key}'");
                    continue;
                }

                if (key == "$main")
                {
                    spriteComponent.BaseRSI = rsiResource.RSI;
                    continue;
                }

                if (!spriteComponent.LayerMapTryGet(key, out var index))
                    continue;

                if (!spriteComponent.TryGetLayer(index, out var layer))
                    continue;

                layer.SetRsi(rsiResource.RSI);
            }
            return;
        }

        foreach (var key in entity.Comp.Layers)
        {
            if (!entity.Comp.CachedTexture.TryGetValue(key, out var rsi))
            {
                Log.Warning($"No RSI path defined for layer '{key}' in CachedTexture for {ToPrettyString(entity)}");
                continue;
            }

            if (key == "$main")
            {
                spriteComponent.BaseRSI = rsi;
                continue;
            }

            if (!spriteComponent.LayerMapTryGet(key, out var index))
                continue;

            if (!spriteComponent.TryGetLayer(index, out var layer))
                continue;

            layer.SetRsi(rsi);
        }
    }
}
