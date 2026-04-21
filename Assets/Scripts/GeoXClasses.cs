using System.Collections.Generic;
using System.Xml.Serialization;

[System.Serializable]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class OutcropObject
{
    [XmlElement("author")] public string author { get; set; }
    [XmlElement("country")] public string country { get; set; }
    [XmlElement("dateAcquired")] public string dateAcquired { get; set; }
    [XmlElement("description")] public string description { get; set; }
    [XmlElement("geoDescription")] public string geoDescription { get; set; }
    [XmlElement("isAssetBundle")] public string isAssetBundle { get; set; }
    [XmlElement("latitude")] public string latitude { get; set; }
    [XmlElement("lithologiesPresent")] public string lithologiesPresent { get; set; }
    [XmlElement("locAccuracy")] public string locAccuracy { get; set; }
    [XmlElement("locDescription")] public string locDescription { get; set; }
    [XmlElement("longitude")] public string longitude { get; set; }
    [XmlElement("modelName")] public string modelName { get; set; }
    [XmlElement("prefabName")] public string prefabName { get; set; }
    [XmlElement("structuresPresent")] public string structuresPresent { get; set; }
    [XmlElement("timePeriod")] public string timePeriod { get; set; }
    [XmlElement("bundleName")] public string bundleName { get; set; }
}

[System.Serializable]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class DEMObject
{
    [XmlElement("author")] public string author { get; set; }
    [XmlElement("description")] public string description { get; set; }
    [XmlElement("geoDescription")] public string geoDescription { get; set; }
    [XmlElement("isAssetBundle")] public string isAssetBundle { get; set; }
    [XmlElement("latitude")] public string latitude { get; set; }
    [XmlElement("longitude")] public string longitude { get; set; }
    [XmlElement("modelName")] public string modelName { get; set; }
    [XmlElement("prefabName")] public string prefabName { get; set; }
    [XmlElement("planetaryBody")] public string planetaryBody { get; set; }
    [XmlElement("elevMin")] public string elevMin { get; set; }
    [XmlElement("elevMax")] public string elevMax { get; set; }
    [XmlElement("bundleName")] public string bundleName { get; set; }
}


[System.Serializable]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class HandSampleObject
{
    [XmlElement("modelName")] public string modelName { get; set; }
    [XmlElement("author")] public string author { get; set; }
    [XmlElement("description")] public string description { get; set; }
    [XmlElement("isAssetBundle")] public string isAssetBundle { get; set; }
    [XmlElement("mineralGroup")] public string mineralGroup { get; set; }
    [XmlElement("locationOfCollection")] public string locationOfCollection { get; set; }
    [XmlElement("prefabName")] public string prefabName { get; set; }
    [XmlElement("bundleName")] public string bundleName { get; set; }
}

[System.Serializable]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class CrystalLatticeObject
{
    [XmlElement("author")] public string author { get; set; }
    [XmlElement("description")] public string description { get; set; }
    [XmlElement("isAssetBundle")] public string isAssetBundle { get; set; }
    [XmlElement("mineralGroup")] public string mineralGroup { get; set; }
    [XmlElement("prefabName")] public string prefabName { get; set; }
    [XmlElement("modelName")] public string modelName { get; set; }
    [XmlElement("symmetry")] public string symmetry { get; set; }
    [XmlElement("bundleName")] public string bundleName { get; set; }
}

[System.Serializable]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class BioObject
{
    [XmlElement("author")] public string author { get; set; }
    [XmlElement("description")] public string description { get; set; }
    [XmlElement("isAssetBundle")] public string isAssetBundle { get; set; }
    [XmlElement("classification")] public string classification { get; set; }
    [XmlElement("prefabName")] public string prefabName { get; set; }
    [XmlElement("modelName")] public string modelName { get; set; }
    [XmlElement("organism")] public string organism { get; set; }
    [XmlElement("bundleName")] public string bundleName { get; set; }
    [XmlElement("fullUrl")] public string fullUrl { get; set; }
}

[System.Serializable]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class ArcheologyObject
{
    [XmlElement("author")] public string author { get; set; }
    [XmlElement("description")] public string description { get; set; }
    [XmlElement("isAssetBundle")] public string isAssetBundle { get; set; }
    [XmlElement("latitude")] public string latitude { get; set; }
    [XmlElement("longitude")] public string longitude { get; set; }
    [XmlElement("prefabName")] public string prefabName { get; set; }
    [XmlElement("modelName")] public string modelName { get; set; }
    [XmlElement("bundleName")] public string bundleName { get; set; }
    [XmlElement("fullUrl")] public string fullUrl { get; set; }

}

[System.Serializable]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class ArchitectureObject
{
    [XmlElement("author")] public string author { get; set; }
    [XmlElement("description")] public string description { get; set; }
    [XmlElement("isAssetBundle")] public string isAssetBundle { get; set; }
    [XmlElement("latitude")] public string latitude { get; set; }
    [XmlElement("longitude")] public string longitude { get; set; }
    [XmlElement("prefabName")] public string prefabName { get; set; }
    [XmlElement("modelName")] public string modelName { get; set; }
    [XmlElement("bundleName")] public string bundleName { get; set; }
    [XmlElement("fullUrl")] public string fullUrl { get; set; }
}

[System.Serializable]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public class ArtHistoryObject
{
    [XmlElement("author")] public string author { get; set; }
    [XmlElement("description")] public string description { get; set; }
    [XmlElement("isAssetBundle")] public string isAssetBundle { get; set; }
    [XmlElement("latitude")] public string latitude { get; set; }
    [XmlElement("longitude")] public string longitude { get; set; }
    [XmlElement("prefabName")] public string prefabName { get; set; }
    [XmlElement("modelName")] public string modelName { get; set; }
    [XmlElement("bundleName")] public string bundleName { get; set; }
    [XmlElement("fullUrl")] public string fullUrl { get; set; }
}

public class GeoXCollection
{
    public string className { get; set; }
    public List<Lab> lab { get; set; }
}

public class Lab
{
    public string labName { get; set; }
    public List<OutcropObject> outcropObjects { get; set; }
    public List<DEMObject> demObjects { get; set; }
    public List<HandSampleObject> handsampleObjects { get; set; }
    public List<CrystalLatticeObject> crystallatticeObjects { get; set; }
    public List<BioObject> bioObjects { get; set; }
    public List<ArchitectureObject> architectureObjects { get; set; }
    public List<ArcheologyObject> archeologyObjects { get; set; }
    public List<ArtHistoryObject> artHistoryObjects { get; set; }
}