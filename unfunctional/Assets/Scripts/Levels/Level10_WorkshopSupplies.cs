using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Missing digging task and replacement supplies, kept separate from the authored
/// workshop's tool animations. Existing meshes are cloned before they can break.
/// </summary>
public partial class Level10_ItemDegradation
{
    private readonly Dictionary<string, GameObject> replacementTemplates = new Dictionary<string, GameObject>();
    private readonly List<Material> supplyMaterials = new List<Material>();
    private Transform supplyTemplates;
    private Transform digSoil;

    private Material SupplyMaterial(Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        supplyMaterials.Add(material);
        return material;
    }

    private GameObject SupplyBox(string name, Vector3 position, Vector3 size, Material material, Transform parent = null)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent != null ? parent : transform, false);
        box.transform.localPosition = position;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private void CreateDigStation(Task task)
    {
        var station = new GameObject("Station_Shovel");
        station.transform.SetParent(transform, false);
        station.transform.position = new Vector3(-7, 0.1f, -6);
        // A fixed interaction volume stays reachable as the visual soil lowers.
        var target = station.AddComponent<BoxCollider>();
        target.size = new Vector3(2.5f, 0.3f, 2.5f);
        digSoil = SupplyBox("Soil", Vector3.zero, new Vector3(2.5f, 0.3f, 2.5f),
            SupplyMaterial(new Color(0.3f, 0.2f, 0.1f)), station.transform).transform;
        digSoil.GetComponent<Collider>().enabled = false;
        var library = GameArtLibrary.Load();
        if (library != null)
        {
            Material rim = library.wood;
            SupplyBox("DigTrayFront", new Vector3(0, 0, -1.3f), new Vector3(2.8f, 0.32f, 0.12f), rim, station.transform);
            SupplyBox("DigTrayBack", new Vector3(0, 0, 1.3f), new Vector3(2.8f, 0.32f, 0.12f), rim, station.transform);
            SupplyBox("DigTrayLeft", new Vector3(-1.34f, 0, 0), new Vector3(0.12f, 0.32f, 2.6f), rim, station.transform);
            SupplyBox("DigTrayRight", new Vector3(1.34f, 0, 0), new Vector3(0.12f, 0.32f, 2.6f), rim, station.transform);
        }
        task.stationObject = station;
        task.stationPosition = station.transform.position;
        SupplyLabel(station.transform, "DIG HERE / SHOVEL", new Vector3(0, 1.4f, 0));
    }

    private void UpdateDigProgress(Task task)
    {
        if (digSoil != null)
        {
            float progress = Mathf.Clamp01((float)task.currentUses / task.requiredUses);
            digSoil.localPosition = Vector3.down * (0.22f * progress);
            digSoil.localScale = new Vector3(2.5f - progress, 0.3f, 2.5f - progress);
        }
    }

    private void InitWorkshopSupplies()
    {
        var storage = new GameObject("ReplacementTemplates");
        storage.transform.SetParent(transform, false);
        storage.SetActive(false);
        supplyTemplates = storage.transform;
        if (availableHammers.Count > 0) CacheReplacement("Hammer", availableHammers[0].gameObject);
        if (availableSaws.Count > 0) CacheReplacement("Saw", availableSaws[0].gameObject);
        if (availableWrenches.Count > 0) CacheReplacement("Wrench", availableWrenches[0].gameObject);
        if (availableBrooms.Count > 0) CacheReplacement("Broom", availableBrooms[0].gameObject);
        string[] names = { "Shovel", "Hammer", "Saw", "Wrench", "Broom" };
        for (int i = 0; i < names.Length; i++)
        {
            var rack = SupplyBox("ToolRack_" + names[i], new Vector3(-5 + i * 2.2f, 0.5f, -10),
                new Vector3(1.6f, 1, 1.2f), SupplyMaterial(toolColors[names[i]]));
            // Keep labels unscaled so they remain readable above each crate.
            var sign = new GameObject("SupplySign");
            sign.transform.SetParent(transform, false);
            sign.transform.position = rack.transform.position + Vector3.up * 1.3f;
            SupplyLabel(sign.transform, names[i].ToUpperInvariant() + "\n[E] REPLACEMENT", Vector3.zero);
        }
        DressWorkshop();
    }

    private void CacheReplacement(string name, GameObject original)
    {
        var template = Instantiate(original, supplyTemplates);
        template.name = name + "Template";
        replacementTemplates.Add(name, template);
    }

    private void DispenseReplacementTool(string toolName)
    {
        if (isSwinging) return;
        if (GetFirstEmptySlot() < 0)
        {
            StartBreakMessage("Inventory full. Press [Q] to drop a tool first.");
            return;
        }
        if (toolName == "Shovel") { PickUpTool(toolName); return; }
        if (!replacementTemplates.TryGetValue(toolName, out GameObject template))
        {
            Debug.LogError($"[Level10] No authored {toolName} available for replacement supplies.");
            return;
        }
        var tool = Instantiate(template, transform);
        tool.name = "Replacement_" + toolName;
        tool.SetActive(true);
        switch (toolName)
        {
            case "Hammer": PickUpHammer(new HammerInfo { gameObject = tool, headTransform = tool.transform.Find("hammerhead"), handleTransform = tool.transform.Find("hammerhandle") }); break;
            case "Saw": PickUpSaw(new SawInfo { gameObject = tool, bladeTransform = tool.transform.Find("sawblade"), handleTransform = tool.transform.Find("sawhandle") }); break;
            case "Wrench": PickUpWrench(new WrenchInfo { gameObject = tool, tipTransform = tool.transform.Find("wrenchtip"), handleTransform = tool.transform.Find("wrenchhandle") }); break;
            case "Broom": PickUpBroom(new BroomInfo { gameObject = tool, headTransform = tool.transform.Find("broomhead"), handleTransform = tool.transform.Find("broomhandle") }); break;
        }
    }

    private void CreateGenericToolVisual(string toolName)
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (currentToolVisual != null) Destroy(currentToolVisual);
        var library = GameArtLibrary.Load();
        if (library == null || library.shovel == null)
        {
            Debug.LogError("[Level10] Workshop art missing. Run Prepare Finale and Workshop Art.");
            return;
        }
        currentToolVisual = Instantiate(library.shovel, cam.transform, false);
        currentToolVisual.name = "HeldTool_" + toolName;
        currentToolVisual.transform.localPosition = new Vector3(0.46f, -0.35f, 1.05f);
        currentToolVisual.transform.localRotation = Quaternion.Euler(-12, 18, 156);
        foreach (var collider in currentToolVisual.GetComponentsInChildren<Collider>()) collider.enabled = false;
    }

    private void SupplyLabel(Transform parent, string content, Vector3 localPosition)
    {
        var go = new GameObject("Label", typeof(TextMesh));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        var text = go.GetComponent<TextMesh>();
        text.text = content;
        text.font = UIHelper.GetDefaultFont();
        text.fontSize = 48;
        go.transform.localRotation = parent.name == "Station_Shovel" ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
        text.characterSize = 0.055f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        go.GetComponent<Renderer>().sharedMaterial = text.font.material;
    }

    protected override void OnDestroy()
    {
        foreach (var material in supplyMaterials) if (material != null) Destroy(material);
        base.OnDestroy();
    }
}
