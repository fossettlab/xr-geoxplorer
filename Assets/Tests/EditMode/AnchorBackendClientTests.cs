using System.Collections.Generic;
using NUnit.Framework;

public class AnchorBackendClientTests
{
    [Test]
    public void BuildListUrl_appends_api_anchors()
    {
        Assert.AreEqual(
            "https://geoxplorer-sas.azurewebsites.net/api/anchors",
            AnchorBackendClient.BuildListUrl("https://geoxplorer-sas.azurewebsites.net"));
    }

    [Test]
    public void TryParseAnchorListResponse_parses_firebase_shape_array()
    {
        string json = "[{\"name\":\"Room\",\"identifier\":\"asa-1\",\"dateCreated\":\"2026-01-01T00:00:00Z\",\"dateExpired\":\"2026-02-01T00:00:00Z\"}]";
        List<AnchorBackendClient.AnchorListEntry> entries;
        Assert.IsTrue(AnchorBackendClient.TryParseAnchorListResponse(json, out entries));
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual("Room", entries[0].name);
    }

    [Test]
    public void TryParseAnchorListResponse_accepts_null_array()
    {
        List<AnchorBackendClient.AnchorListEntry> entries;
        Assert.IsTrue(AnchorBackendClient.TryParseAnchorListResponse("null", out entries));
        Assert.AreEqual(0, entries.Count);
    }

    [Test]
    public void BuildCreateUrl_appends_api_anchors()
    {
        Assert.AreEqual(
            "https://geoxplorer-sas.azurewebsites.net/api/anchors",
            AnchorBackendClient.BuildCreateUrl("https://geoxplorer-sas.azurewebsites.net"));
    }

    [Test]
    public void BuildGetUrl_includes_anchor_id()
    {
        string id = "a1b2c3d4e5f6789012345678abcdef01";
        Assert.AreEqual(
            "https://example.com/api/anchors/" + id,
            AnchorBackendClient.BuildGetUrl("https://example.com", id));
    }

    [Test]
    public void TryParseAnchorResponse_parses_valid_json()
    {
        string json = "{\"id\":\"abc\",\"name\":\"Room\",\"identifier\":\"asa-1\",\"date_created\":\"2026-01-01T00:00:00Z\",\"date_expired\":\"2026-02-01T00:00:00Z\"}";
        AnchorBackendClient.AnchorBackendRecord record;
        Assert.IsTrue(AnchorBackendClient.TryParseAnchorResponse(json, out record));
        Assert.AreEqual("abc", record.id);
        Assert.AreEqual("asa-1", record.identifier);
    }
}
