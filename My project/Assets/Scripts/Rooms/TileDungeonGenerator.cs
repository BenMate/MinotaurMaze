using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class WeightedRoom
{
    public GameObject prefab;

    [Tooltip("Minimum weight for this room to appear. Higher = more likely")]
    public int minWeight = 1;

    [Tooltip("Maximum weight for this room to appear. Random value between min and max used")]
    public int maxWeight = 3;

    public int GetRandomWeight()
    {
        return Random.Range(minWeight, maxWeight + 1);
    }
}

public class TileDungeonGenerator : MonoBehaviour
{
    [Header("Room Prefabs & Settings")]
    public List<WeightedRoom> roomPrefabs = new List<WeightedRoom>();
    public int roomCount = 15;

    [Tooltip("Tolerance for width matching between attachment points (allows slight mismatch)")]
    public float widthTolerance = 1f;

    [Header("Dead-End Prefabs by Direction")]
    public List<GameObject> northDeadEnds = new List<GameObject>();
    public List<GameObject> southDeadEnds = new List<GameObject>();
    public List<GameObject> eastDeadEnds = new List<GameObject>();
    public List<GameObject> westDeadEnds = new List<GameObject>();

    private List<RoomModule> placedRooms = new List<RoomModule>();
    private List<OpenAttachment> openAttachments = new List<OpenAttachment>();

    private class OpenAttachment
    {
        public RoomModule baseRoom;
        public RoomModule.AttachmentPoint attachPoint;
    }

    void Start()
    {
        GenerateDungeon();
    }

    public void GenerateDungeon()
    {
        placedRooms.Clear();
        openAttachments.Clear();

        if (roomPrefabs.Count == 0)
        {
            Debug.LogWarning("No room prefabs assigned!");
            return;
        }

        // Spawn first room at origin
        WeightedRoom firstWeighted = GetRandomWeightedPrefab();
        if (firstWeighted.prefab == null) return;

        GameObject firstRoomGO = Instantiate(firstWeighted.prefab, Vector3.zero, Quaternion.identity);
        RoomModule firstRoom = firstRoomGO.GetComponent<RoomModule>();
        if (firstRoom == null) return;

        ResetRoomAttachments(firstRoom);
        placedRooms.Add(firstRoom);

        foreach (var attach in firstRoom.attachmentPoints)
            if (attach != null && !attach.isUsed)
                openAttachments.Add(new OpenAttachment { baseRoom = firstRoom, attachPoint = attach });

        int roomsPlaced = 1;
        int attempts = 0;
        int maxAttempts = roomCount * 20;

        while (roomsPlaced < roomCount && attempts < maxAttempts)
        {
            if (openAttachments.Count == 0) break;

            int index = Random.Range(0, openAttachments.Count);
            OpenAttachment openAttach = openAttachments[index];

            bool placed = TryPlaceRoomAtAttachment(openAttach);
            attempts++;

            if (placed)
            {
                roomsPlaced++;
                openAttachments.Remove(openAttach);
            }
            else
            {
                // leave in list for dead-end placement later
                openAttachments.Remove(openAttach);
            }
        }

        if (roomsPlaced < roomCount)
            Debug.LogWarning($"Could only place {roomsPlaced} out of {roomCount} rooms.");

        AddDeadEnds();
    }

    private bool TryPlaceRoomAtAttachment(OpenAttachment openAttach)
    {
        var candidates = new List<(WeightedRoom weighted, RoomModule.AttachmentPoint attachPointData)>();

        foreach (var weightedRoom in roomPrefabs)
        {
            if (weightedRoom?.prefab == null) continue;

            RoomModule prefabModule = weightedRoom.prefab.GetComponent<RoomModule>();
            if (prefabModule == null || prefabModule.attachmentPoints == null) continue;

            // Use attachment data only; no instance modification
            foreach (var attach in prefabModule.attachmentPoints)
            {
                if (attach == null) continue;

                bool widthMatch = Mathf.Abs(openAttach.attachPoint.size.x - attach.size.x) <= widthTolerance;
                bool dirMatch = IsOpposite(openAttach.attachPoint.forwardDir, attach.forwardDir);

                if (widthMatch && dirMatch)
                    candidates.Add((weightedRoom, attach));
            }
        }

        if (candidates.Count == 0) return false;

        // Weighted random selection
        int totalWeight = candidates.Sum(c => c.weighted.GetRandomWeight());
        int rand = Random.Range(0, totalWeight);
        int sum = 0;
        WeightedRoom chosenWeighted = null;
        RoomModule.AttachmentPoint chosenAttach = null;

        foreach (var c in candidates)
        {
            sum += c.weighted.GetRandomWeight();
            if (rand < sum)
            {
                chosenWeighted = c.weighted;
                chosenAttach = c.attachPointData;
                break;
            }
        }

        if (chosenWeighted?.prefab == null || chosenAttach == null) return false;

        Vector3 spawnPos = openAttach.baseRoom.transform.position
                           + openAttach.attachPoint.localPosition
                           - chosenAttach.localPosition;

        RoomModule newRoomModule = chosenWeighted.prefab.GetComponent<RoomModule>();
        Bounds newBounds = new Bounds(spawnPos, newRoomModule.GetRoomBounds().size);
        if (placedRooms.Any(r => r.GetRoomBounds().Intersects(newBounds))) return false;

        GameObject roomGO = Instantiate(chosenWeighted.prefab, spawnPos, Quaternion.identity);
        RoomModule newRoom = roomGO.GetComponent<RoomModule>();
        if (newRoom == null) return false;

        ResetRoomAttachments(newRoom);

        // Find matching attachment on spawned room
        RoomModule.AttachmentPoint spawnedAttach = newRoom.attachmentPoints.FirstOrDefault(a =>
            Mathf.Abs(a.size.x - chosenAttach.size.x) <= widthTolerance &&
            a.forwardDir == chosenAttach.forwardDir
        );
        if (spawnedAttach == null) return false;

        openAttach.attachPoint.isUsed = true;
        spawnedAttach.isUsed = true;

        placedRooms.Add(newRoom);

        foreach (var attach in newRoom.attachmentPoints)
            if (attach != null && !attach.isUsed)
                openAttachments.Add(new OpenAttachment { baseRoom = newRoom, attachPoint = attach });

        return true;
    }

    private void AddDeadEnds()
    {
        List<OpenAttachment> attachmentsToProcess = new List<OpenAttachment>(openAttachments);

        foreach (var openAttach in attachmentsToProcess)
        {
            RoomModule baseRoom = openAttach.baseRoom;
            RoomModule.AttachmentPoint baseAttach = openAttach.attachPoint;

            if (baseAttach == null || baseAttach.isUsed) continue;

            // Select possible dead-ends that fit and don't overlap
            List<GameObject> possibleDeadEnds = GetAllDeadEndsForDirection(openAttach.attachPoint.forwardDir)
                .Where(prefab =>
                {
                    RoomModule module = prefab.GetComponent<RoomModule>();
                    if (module == null || module.attachmentPoints.Count == 0) return false;
                    Bounds bounds = new Bounds(baseRoom.transform.position + baseAttach.localPosition - module.attachmentPoints[0].localPosition,
                                               module.GetRoomBounds().size);
                    return !placedRooms.Any(r => r.GetRoomBounds().Intersects(bounds));
                }).ToList();

            if (possibleDeadEnds.Count == 0) continue;

            GameObject deadEndPrefab = possibleDeadEnds[Random.Range(0, possibleDeadEnds.Count)];
            RoomModule deadEndModule = deadEndPrefab.GetComponent<RoomModule>();
            if (deadEndModule == null || deadEndModule.attachmentPoints.Count == 0) continue;

            Vector3 spawnPos = baseRoom.transform.position + baseAttach.localPosition - deadEndModule.attachmentPoints[0].localPosition;

            GameObject roomGO = Instantiate(deadEndPrefab, spawnPos, Quaternion.identity);
            RoomModule spawnedRoom = roomGO.GetComponent<RoomModule>();
            if (spawnedRoom == null) continue;

            ResetRoomAttachments(spawnedRoom);

            baseAttach.isUsed = true;
            spawnedRoom.attachmentPoints[0].isUsed = true;

            placedRooms.Add(spawnedRoom);
            openAttachments.Remove(openAttach);
        }

        openAttachments.Clear();
    }

    private List<GameObject> GetAllDeadEndsForDirection(RoomModule.Direction openDir)
    {
        switch (openDir)
        {
            case RoomModule.Direction.North: return southDeadEnds;
            case RoomModule.Direction.South: return northDeadEnds;
            case RoomModule.Direction.East: return westDeadEnds;
            case RoomModule.Direction.West: return eastDeadEnds;
        }
        return new List<GameObject>();
    }

    private WeightedRoom GetRandomWeightedPrefab()
    {
        int totalWeight = roomPrefabs.Sum(r => r.GetRandomWeight());
        int random = Random.Range(0, totalWeight);
        int sum = 0;

        foreach (var r in roomPrefabs)
        {
            sum += r.GetRandomWeight();
            if (random < sum) return r;
        }

        return roomPrefabs[0];
    }

    private bool IsOpposite(RoomModule.Direction a, RoomModule.Direction b)
    {
        return (a == RoomModule.Direction.North && b == RoomModule.Direction.South) ||
               (a == RoomModule.Direction.South && b == RoomModule.Direction.North) ||
               (a == RoomModule.Direction.East && b == RoomModule.Direction.West) ||
               (a == RoomModule.Direction.West && b == RoomModule.Direction.East);
    }

    private void ResetRoomAttachments(RoomModule room)
    {
        if (room == null || room.attachmentPoints == null) return;
        foreach (var attach in room.attachmentPoints)
            if (attach != null) attach.isUsed = false;
    }
}
