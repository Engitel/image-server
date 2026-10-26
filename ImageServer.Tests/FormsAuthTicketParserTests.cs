using ImageServer.Authorization;

namespace ImageServer.Tests;

public class FormsAuthTicketParserTests
{
    // Spin&Go-style UserData: admin token at index 1, user token at index 9 (1-based)
    private const string SampleUserData = "42|a|b|c|d|e|f|g|1001|extra";

    [Fact]
    public void ParseUserData_ReadsConfiguredTokens()
    {
        var ids = FormsAuthTicketParser.ParseUserData(SampleUserData, '|', userToken: 9, adminToken: 1);

        Assert.Equal(1001, ids.UserId);
        Assert.Equal(42, ids.UserAdminId);
        Assert.True(ids.IsAdmin);
    }

    [Fact]
    public void ParseUserData_ReturnsDefault_WhenUserDataMissingSeparator()
    {
        var ids = FormsAuthTicketParser.ParseUserData("nosep", '|', 9, 1);

        Assert.Equal(0, ids.UserId);
        Assert.Equal(0, ids.UserAdminId);
    }

    [Fact]
    public void ParseUserData_ReturnsDefault_WhenNullOrEmpty()
    {
        Assert.Equal(default, FormsAuthTicketParser.ParseUserData(null, '|', 9, 1));
        Assert.Equal(default, FormsAuthTicketParser.ParseUserData("", '|', 9, 1));
    }

    [Fact]
    public void ParseUserData_IgnoresOutOfRangeTokens()
    {
        var ids = FormsAuthTicketParser.ParseUserData("1|2", '|', userToken: 9, adminToken: 1);

        Assert.Equal(0, ids.UserId);
        Assert.Equal(1, ids.UserAdminId);
    }
}
