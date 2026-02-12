using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class GroupLOD : IComparable<GroupLOD>
{
    public float distance = 0;
    [Range(1, 256)] public int resolution = 10;

    public List<MeshFilter> Meshes;

    public int CompareTo(GroupLOD other)
    {
        return distance.CompareTo(other.distance);
    }
}
[Serializable]
public struct LODGroupDebugData
{
    public bool show;
    public Color color;
}

[RequireComponent(typeof(Planet))]
public class PlanetLODManager : MonoBehaviour
{
    [SerializeField] Transform _distanceDeterminer;
    [SerializeField] LODGroupDebugData[] groupDebugData;
    [SerializeField] List<GroupLOD> groups;
    Planet _planet;
    GroupLOD _selectedGroup;
    bool _firstGeneration = true;

    Planet Planet 
    {
        get
        {
            if (_planet == null) _planet = GetComponent<Planet>();
            return _planet;
        }
    }

    void OnValidate()
    {
        ValidateGroups(groups);

    }

    void FixedUpdate()
    {
            Tick();
    }

    private void OnDrawGizmos()
    {
        if(Planet.shapeSettings == null || groupDebugData == null || groups == null) return;
        int count = Math.Min(groupDebugData.Length, groups.Count);

        for (int i = 0; i < count; i++) 
        {
            LODGroupDebugData data = groupDebugData[i];
            if (!data.show) continue;

            Gizmos.color = data.color;

            Gizmos.DrawSphere(Planet.transform.position, Planet.shapeSettings.planetRadius + groups[i].distance);
        }
    }

    void Tick()
    {
        if (groups.Count == 0) return;

        float distance = Vector3.Distance(_distanceDeterminer.position, Planet.transform.position);
        GroupLOD selectedGroup = GetGroup(distance);

        if (IsGroupValid(selectedGroup))
        {
            SwitchToNewGroup(selectedGroup);
        }
        else
        {
            if(Planet.resolution != selectedGroup.resolution)
            {
                Planet.resolution = selectedGroup.resolution;
                if (_firstGeneration)
                {
                    _firstGeneration = false;
                    Planet.GetAllMeshFilters().ForEach(x => DestroyImmediate(x.gameObject));
                }
                Planet.EmptyMeshFilterCache();
                Planet.GeneratePlanet();
            }

            PopulateGroup(Planet, selectedGroup);

            SwitchToNewGroup(selectedGroup);
            _firstGeneration = false;

        }

    }

    void SwitchToNewGroup(GroupLOD newGroup)
    {
        if (_selectedGroup != null)
        {
            if (_selectedGroup.resolution == newGroup.resolution) return;
            SetActiveGroup(_selectedGroup, false);
        }
        _selectedGroup = newGroup;
        SetActiveGroup(_selectedGroup, true);
        Debug.Log("Switched to " + _selectedGroup.resolution);
    }

    void ValidateGroups(List<GroupLOD> groups)
    {
        if (groups == null) return;
        if (groups.Count == 0)
        {
            groups.Add(new GroupLOD());
            return;
        }
        for (int i = 1; i < groups.Count; i++)
        {
            if (groups[i].distance < 0)
            {
                groups[i].distance = 0;
            }
            if (groups[i - 1].distance >= groups[i].distance)
            {
                groups[i].distance = groups[i - 1].distance + .1f;
            }
        }
        groups[0].distance = 0;

    }

    GroupLOD GetGroup(float distance)
    {
        if(groups.Count == 1) return groups[0];

        for (int i = groups.Count - 1; i >= 0; i--)
        {
            if (distance > groups[i].distance + Planet.shapeSettings.planetRadius)
            {
                return groups[i];
                
            }
        }
        Debug.LogWarning("No LOD specified for this range");
        return groups[0];
    }

    bool IsGroupValid(GroupLOD group)
    {
        if(group == null) return false;
        List<MeshFilter> meshes = group.Meshes;
        return meshes!= null && meshes.Count > 0 && meshes[0] != null;
    }

    void SetActiveGroup(GroupLOD group, bool isActive)
    {
        group.Meshes.ForEach(mesh => mesh.gameObject.SetActive(isActive));
    }

    void PopulateGroup(Planet planet, GroupLOD group)
    {
        if(planet == null) return;

        List<MeshFilter> filters = planet.GetAllMeshFilters();
        group.Meshes = filters;
    }
}
