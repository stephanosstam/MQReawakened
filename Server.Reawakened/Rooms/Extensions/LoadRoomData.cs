﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Server.Base.Core.Extensions;
using Server.Base.Logging;
using Server.Reawakened.Configs;
using Server.Reawakened.Network.Helpers;
using Server.Reawakened.Rooms.Models.Entities;
using Server.Reawakened.Rooms.Models.Entities.ColliderType;
using Server.Reawakened.Rooms.Models.Planes;
using Server.Reawakened.Rooms.Services;
using Server.Reawakened.XMLs.BundlesInternal;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Xml;
using UnityEngine;
using WorldGraphDefines;

namespace Server.Reawakened.Rooms.Extensions;

public static class LoadRoomData
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    public static Dictionary<string, BaseCollider> LoadTerrainColliders(this Room room)
    {
        var outColliderList = new Dictionary<string, BaseCollider>();
        room.ColliderCatalog.TerrainColliderCatalog.TryGetValue(room.LevelInfo.LevelId, out var colliderList);
        Console.WriteLine(room.LevelInfo.LevelId);
        foreach (var collider in colliderList)
        {
            outColliderList.Add("0", new TCCollider(collider, room));
            Console.WriteLine(collider.Position + ", " + collider.Width + collider.Height);
        }
        return outColliderList;
    }
    public static Dictionary<string, PlaneModel> LoadPlanes(this LevelInfo levelInfo, ServerRConfig config)
    {
        var levelInfoPath = Path.Join(config.LevelSaveDirectory, $"{levelInfo.Name}.xml");
        var levelDataPath = Path.Join(config.LevelDataSaveDirectory, $"{levelInfo.Name}.json");

        var xmlDocument = new XmlDocument();
        xmlDocument.Load(levelInfoPath);

        var planeNames = new string[7];

        for (var i = 0; i < planeNames.Length; i++)
            planeNames[i] = (i % 2 != 0 ? "Plane" : "Decor") + i / 2;

        planeNames[5] = "Unity";
        planeNames[6] = "TemplatePlane";

        var planes = planeNames.ToDictionary(name => name, name => new PlaneModel(name));

        foreach (XmlNode data in xmlDocument.FirstChild!.NextSibling!)
            foreach (XmlNode planeNode in data.ChildNodes)
            {
                if (planeNode.Name != "Plane")
                    continue;

                var planeName = planeNode.Attributes!.GetValue("name");
                var plane = planes[planeName];

                foreach (XmlNode gameObject in planeNode.ChildNodes)
                {
                    if (gameObject.Name != "GameObject")
                        continue;

                    foreach (XmlNode gameObjectAttributes in gameObject.ChildNodes)
                    {
                        switch (data.Name)
                        {
                            case "LoadUnit":
                                plane.LoadGameObjectXml(gameObjectAttributes);
                                break;
                            case "Collider":
                                plane.LoadColliderXml(gameObjectAttributes);
                                break;
                        }
                    }
                }
            }

        File.WriteAllText(levelDataPath, JsonSerializer.Serialize(planes, _jsonSerializerOptions));

        return planes;
    }
    public static Dictionary<string, List<BaseComponent>> LoadEntities(this Room room, IServiceProvider services,
    out Dictionary<string, List<string>> unknownEntities)
    {
        var reflectionUtils = services.GetRequiredService<ReflectionUtils>();
        var fileLogger = services.GetRequiredService<FileLogger>();
        var classCopier = services.GetRequiredService<ClassCopier>();

        var entities = new Dictionary<string, List<BaseComponent>>();
        unknownEntities = [];

        if (room.Planes == null)
            return entities;

        var invalidProcessable = new List<string>();

        var entityComponents = typeof(BaseComponent).Assembly.GetServices<BaseComponent>()
            .Where(t => t.BaseType != null)
            .Where(t => t.BaseType.GenericTypeArguments.Length > 0)
            .ToDictionary(t => t.BaseType.GenericTypeArguments.First().FullName, t => t);

        var processable = typeof(DataComponentAccessor).Assembly.GetServices<DataComponentAccessor>()
            .ToDictionary(x => x.Name, x => x);

        string translateComponent;
        string[] translatedArray;

        foreach (var plane in room.Planes)
            foreach (var entity in plane.Value.GameObjects)
                foreach (var component in entity.Value.ObjectInfo.Components)
                {
                    if (!processable.TryGetValue(component.Key, out var mqType))
                        continue;

                    if (entityComponents.TryGetValue(mqType.FullName!, out var internalType))
                    {
                        var newEntity = classCopier.GetClassAndInfo(mqType);

                        var dataObj = newEntity.Key;
                        var fields = newEntity.Value;

                        foreach (var componentValue in component.Value.ComponentAttributes.Where(componentValue =>
                                     !string.IsNullOrEmpty(componentValue.Value)))
                        {
                            var field = fields.FirstOrDefault(f => f.Name == componentValue.Key);

                            if (field == null)
                                continue;

                            if (field.FieldType == typeof(string))
                                field.SetValue(dataObj, componentValue.Value);
                            else if (field.FieldType == typeof(int))
                                field.SetValue(dataObj, int.Parse(componentValue.Value));
                            else if (field.FieldType == typeof(bool))
                                field.SetValue(dataObj, componentValue.Value.Equals("true", StringComparison.CurrentCultureIgnoreCase));
                            else if (field.FieldType == typeof(float))
                                field.SetValue(dataObj, float.Parse(componentValue.Value));
                            else if (field.FieldType.IsEnum)
                                field.SetValue(dataObj, Enum.Parse(field.FieldType, componentValue.Value));
                            else if (field.FieldType == typeof(Vector3))
                            {
                                translateComponent = componentValue.Value.Replace("(", "").Replace(")", "");
                                translatedArray = translateComponent.Split(",");
                                field.SetValue(dataObj, new Vector3(float.Parse(translatedArray[0]), float.Parse(translatedArray[1]), float.Parse(translatedArray[2])));
                            }
                            else if (field.FieldType == typeof(Color))
                            {
                                translateComponent = componentValue.Value.Replace("RGBA(", "").Replace(")", "");
                                translatedArray = translateComponent.Split(",");
                                field.SetValue(dataObj, new Color(float.Parse(translatedArray[0]), float.Parse(translatedArray[1]), float.Parse(translatedArray[2]), float.Parse(translatedArray[3])));
                            }
                            else if (field.FieldType == typeof(string[]))
                            {
                                translatedArray = componentValue.Value.Split(",");
                                field.SetValue(dataObj, translatedArray);
                            }
                            else
                            {
                                room.Logger.LogError("It is unknown how to convert a string to a {FieldType} (data: {Data}).",
                                    field.FieldType, componentValue.Value);
                            }
                        }

                        var entityData = new Entity(entity.Value, room, fileLogger);

                        var instancedComponent = reflectionUtils.CreateBuilder<BaseComponent>(internalType.GetTypeInfo())
                            .Invoke(services);

                        var methods = internalType.GetMethods().Where(m =>
                        {
                            var parameters = m.GetParameters();

                            return
                                m.Name == "SetComponentData" &&
                                parameters.Length == 2 &&
                                parameters[0].ParameterType == dataObj.GetType() &&
                                parameters[1].ParameterType == entityData.GetType();
                        }).ToArray();

                        if (methods.Length != 1)
                            room.Logger.LogError(
                                "Found invalid {Count} amount of initialization methods for {EntityId} ({EntityType})",
                                methods.Length, entity.Key, internalType.Name);
                        else
                            methods.First().Invoke(instancedComponent, [dataObj, entityData]);

                        if (!entities.ContainsKey(entity.Key))
                            entities.Add(entity.Key, []);

                        entities[entity.Key].Add(instancedComponent);
                    }
                    else
                    {
                        if (!unknownEntities.ContainsKey(entity.Key))
                            unknownEntities.Add(entity.Key, []);

                        unknownEntities[entity.Key].Add(mqType.Name);

                        if (!invalidProcessable.Contains(mqType.Name))
                            invalidProcessable.Add(mqType.Name);
                    }
                }

        foreach (var type in invalidProcessable.Order())
            room.Logger.LogWarning("Could not find synced entity for {EntityType}", type);

        return entities;
    }

    public static Dictionary<string, T> GetComponentsOfType<T>(this Room room) where T : class
    {
        var type = typeof(T);

        var components = room.Entities.Values.SelectMany(t => t).Where(t => t is T).ToArray();

        if (components.Length > 0)
            return components.ToDictionary(x => x.Id, x => x as T);

        room.Logger.LogError("Could not find components with type {TypeName}. Returning empty. " +
                             "Possible types: {Types}", type.Name, string.Join(", ", room.Entities.Keys));

        return [];
    }

    public static string GetUnknownComponentTypes(this Room room, string id)
    {
        var entityInfo = new Dictionary<string, IEnumerable<string>>();

        if (room.UnknownEntities.TryGetValue(id, out var value))
            entityInfo.Add("entities", value);

        var components = room.Planes.Values
            .Where(p => p.GameObjects.ContainsKey(id))
            .Select(p => p.GameObjects[id])
            .SelectMany(g => g.ObjectInfo.Components.Keys)
            .Where(c => !entityInfo.Values.SelectMany(s => s).Contains(c))
            .ToArray();

        if (components.Length != 0)
            entityInfo.Add("components", components);

        entityInfo.Add("game object", new[] { id.ToString() });

        return $"Unknown {string.Join(", ",
            entityInfo.Select(a => $"{a.Key}: {string.Join(", ", a.Value)}")
        )}";
    }
}
