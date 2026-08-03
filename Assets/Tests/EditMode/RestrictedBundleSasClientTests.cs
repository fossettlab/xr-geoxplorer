using NUnit.Framework;

public class RestrictedBundleSasClientTests
{
    [Test]
    public void BuildRequestUrl_appends_api_sas_restricted()
    {
        Assert.AreEqual(
            "https://geoxplorer-sas.azurewebsites.net/api/sas/restricted",
            RestrictedBundleSasClient.BuildRequestUrl("https://geoxplorer-sas.azurewebsites.net"));
    }

    [Test]
    public void BuildRequestUrl_handles_base_already_ending_with_api()
    {
        Assert.AreEqual(
            "https://geoxplorer-sas.azurewebsites.net/api/sas/restricted",
            RestrictedBundleSasClient.BuildRequestUrl("https://geoxplorer-sas.azurewebsites.net/api"));
    }

    [Test]
    public void BuildRequestUrl_trims_trailing_slash()
    {
        Assert.AreEqual(
            "https://example.com/api/sas/restricted",
            RestrictedBundleSasClient.BuildRequestUrl("https://example.com/"));
    }

    [Test]
    public void TryParseResponse_parses_valid_json()
    {
        string json = "{\"url\":\"https://acct.blob.core.windows.net/restricted/a/b?s=sig\",\"ttlMinutes\":15}";
        string url;
        int ttl;
        Assert.IsTrue(RestrictedBundleSasClient.TryParseResponse(json, out url, out ttl));
        Assert.AreEqual("https://acct.blob.core.windows.net/restricted/a/b?s=sig", url);
        Assert.AreEqual(15, ttl);
    }

    [Test]
    public void TryParseResponse_rejects_missing_url()
    {
        string url;
        int ttl;
        Assert.IsFalse(RestrictedBundleSasClient.TryParseResponse("{\"ttlMinutes\":15}", out url, out ttl));
    }
}
